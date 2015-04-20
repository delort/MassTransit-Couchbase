namespace MassTransit.Persistence.Couchbase
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Transactions;

    using Automatonymous;

    using Common.Logging;

    using global::Couchbase;
    using global::Couchbase.Core;

    using MassTransit.Exceptions;
    using MassTransit.Persistence.Couchbase.Configuration;
    using MassTransit.Pipeline;
    using MassTransit.Saga;
    using MassTransit.Util;

    public class CouchbaseSagaRepository<TInstance, TSaga> : ISagaRepository<TInstance>, IDisposable
        where TInstance : class, ISaga
        where TSaga : class, StateMachine<TInstance>
    {
        private readonly ILog log;

        private readonly ICluster cluster;

        private readonly IBucket bucket;

        private readonly ICouchbaseSagaRepositorySettings<TSaga> settings;

        public CouchbaseSagaRepository(
            ICluster cluster,
            ICouchbaseSagaRepositorySettings<TSaga> settings,
            ILog log)
        {
            this.cluster = cluster;
            this.settings = settings;
            this.log = log;

            this.cluster.Configuration.SerializationSettings = this.settings.SerializationSettings;
            this.cluster.Configuration.DeserializationSettings = this.settings.DeserializationSettings;

            this.bucket = BucketFactory.CreateBucketAndDesignDocument(
               this.cluster,
               this.settings.BucketName,
               this.settings.ServerUsername,
               this.settings.ServerPassword,
               this.settings.DesignDocumentName,
               this.settings.DesignDocument);
        }

        public void Dispose()
        {
            if (this.bucket != null)
            {
                this.cluster.CloseBucket(this.bucket);
            }
        }

        public IEnumerable<Action<IConsumeContext<TMessage>>> GetSaga<TMessage>(
            IConsumeContext<TMessage> context,
            Guid instanceId,
            InstanceHandlerSelector<TInstance, TMessage> selector,
            ISagaPolicy<TInstance, TMessage> policy) where TMessage : class
        {
            ////using (var transaction = new TransactionScope(TransactionScopeOption.RequiresNew))
            ////TODO: Looks like Custom json deserialization is not performed when GetDocument is executed
            ////var getResult = bucket.GetDocument<TInstance>(instanceId.ToString());
            ////if (!getResult.Success)
            ////{
            ////    var sagaException = new SagaException(
            ////        getResult.Message,
            ////        typeof(TInstance),
            ////        typeof(TMessage),
            ////        instanceId,
            ////        getResult.Exception);

            ////    this.log.Error(sagaException);
            ////    throw sagaException;
            ////}
            ////else if (
            ////    getResult.Document != null
            ////    && getResult.Document.Content != null)
            ////{
            ////    instance = getResult.Document.Content;
            ////}

            var instance = this
                .Where(s => s.CorrelationId.Equals(instanceId))
                .FirstOrDefault();

            if (instance == null)
            {
                if (policy.CanCreateInstance(context))
                {
                    yield return action =>
                        {
                            this.log.DebugFormat(
                                "{0} Creating New instance {1} for {2}",
                                typeof(TInstance).ToFriendlyName(),
                                instanceId,
                                typeof(TMessage).ToFriendlyName());

                            try
                            {
                                instance = policy.CreateInstance(action, instanceId);

                                foreach (var callback in selector(instance, action))
                                {
                                    callback(action);
                                }

                                if (!policy.CanRemoveInstance(instance))
                                {
                                    this.RetrieveAndProcessResult(
                                        () => this.bucket.Insert(instanceId.ToString(), instance),
                                        result => { });
                                }
                            }
                            catch (Exception exception)
                            {
                                var sagaException = new SagaException(
                                    "Create Saga Instance Exception",
                                    typeof(TInstance),
                                    typeof(TMessage),
                                    instanceId,
                                    exception);

                                this.log.Error(sagaException);
                                throw sagaException;
                            }
                        };
                }
                else
                {
                    this.log.DebugFormat(
                        "{0} Ignoring Missing instance {1} for {2}",
                        typeof(TInstance).ToFriendlyName(),
                        instanceId,
                        typeof(TMessage).ToFriendlyName());
                }
            }
            else
            {
                if (policy.CanUseExistingInstance(context))
                {
                    yield return action =>
                        {
                            this.log.DebugFormat(
                                "{0} Using Existing instance {1} for {2}",
                                typeof(TInstance).ToFriendlyName(),
                                instanceId,
                                typeof(TMessage).ToFriendlyName());

                            try
                            {
                                foreach (var callback in selector(instance, action))
                                {
                                    callback(action);
                                }

                                if (policy.CanRemoveInstance(instance))
                                {
                                    this.RetrieveAndProcessResult(
                                        () => this.bucket.Remove(instanceId.ToString()),
                                        result => { });
                                }
                                else
                                {
                                    this.RetrieveAndProcessResult(
                                        () => this.bucket.Replace(instanceId.ToString(), instance),
                                        result => { });
                                }
                            }
                            catch (Exception exception)
                            {
                                var sagaException = new SagaException(
                                    "Existing Saga Instance Exception",
                                    typeof(TInstance),
                                    typeof(TMessage),
                                    instanceId,
                                    exception);

                                this.log.Error(sagaException);
                                throw sagaException;
                            }
                        };
                }
                else
                {
                    this.log.DebugFormat(
                        "{0} Ignoring Existing instance {1} for {2}",
                        typeof(TInstance).ToFriendlyName(),
                        instanceId,
                        typeof(TMessage).ToFriendlyName());
                }
            }
        }

        public IEnumerable<Guid> Find(ISagaFilter<TInstance> filter)
        {
            return this.Where(filter, x => x.CorrelationId);
        }

        public IEnumerable<TInstance> Where(ISagaFilter<TInstance> filter)
        {
            var whereResult = new List<TInstance>();
            this.RetrieveAndProcessResult(
                () =>
                    {
                        var query = this.bucket.CreateQuery(
                            this.settings.DesignDocumentName,
                            this.settings.BucketViewName);

                        return this.bucket.Query<TInstance>(query);
                    },
                result => whereResult.AddRange(
                    result.Rows.Select(item => item.Value)
                        .AsQueryable()
                        .Where(filter.FilterExpression)));

            return whereResult;
        }

        public IEnumerable<TResult> Where<TResult>(ISagaFilter<TInstance> filter, Func<TInstance, TResult> transformer)
        {
            return this.Where(filter).Select(transformer);
        }

        public IEnumerable<TResult> Select<TResult>(Func<TInstance, TResult> transformer)
        {
            var selectResult = new List<TResult>();
            this.RetrieveAndProcessResult(
                () =>
                    {
                        var query = this.bucket.CreateQuery(
                            this.settings.DesignDocumentName,
                            this.settings.BucketViewName);

                        return this.bucket.Query<TInstance>(query);
                    },
                result => selectResult.AddRange(
                    result.Rows.Select(item => item.Value)
                        .AsQueryable()
                        .Select(transformer)));

            return selectResult;
        }

        private void RetrieveAndProcessResult<T>(
            Func<T> retrieve,
            Action<T> process) where T : IResult
        {
            using (var scope = new TransactionScope(TransactionScopeOption.RequiresNew))
            {
                var operationResult = retrieve();
                if (!operationResult.Success)
                {
                    throw new Exception(
                        operationResult.Message,
                        operationResult.Exception);
                }

                process(operationResult);
                scope.Complete();
            }
        }
    }
}
