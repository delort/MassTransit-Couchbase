namespace MassTransit.Persistence.Couchbase.Configuration
{
    using Automatonymous;

    using Newtonsoft.Json;

    public interface ICouchbaseSagaRepositorySettings<TSaga>
        where TSaga : StateMachine
    {
        string ServerUsername { get; }

        string ServerPassword { get; }

        string BucketName { get; }

        /// <summary>
        /// Gets the name of the Couchbase Bucket View used to get all Sagas.
        /// View should be defined in the <see cref="DesignDocument"/> property.
        /// </summary>
        /// <example>AllSagas</example>
        string BucketViewName { get; set; }

        /// <summary>
        /// Gets the name of the Couchbase Bucket Design Document defined in the <see cref="DesignDocument"/> property.
        /// Value is used to create new Design Document if Bucket does not exist.
        /// </summary>
        string DesignDocumentName { get; }

        /// <summary>
        /// Gets Couchbase Design Document in JSON format.
        /// Should contain definition of the view configured in <see cref="BucketViewName"/> property.
        /// </summary>
        /// <example>
        /// {
        ///     "views": {
        ///         "AllSagas": {
        ///             "map": "function (doc, meta) {\n  emit(meta.id, doc);\n}"
        ///         }
        ///     }
        /// }
        /// </example>
        string DesignDocument { get; }

        JsonSerializerSettings SerializationSettings { get; }

        JsonSerializerSettings DeserializationSettings { get; }
    }
}
