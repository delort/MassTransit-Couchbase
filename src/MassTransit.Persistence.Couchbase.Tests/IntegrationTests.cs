namespace MassTransit.Persistence.Couchbase.Tests
{
    using System;
    using System.Collections.Generic;

    using Autofac;

    using global::Couchbase;
    using global::Couchbase.Configuration.Client;

    using MassTransit.Persistence.Couchbase.Tests.Sagas;

    using Xunit;

    public class IntegrationTests : IDisposable
    {
        private readonly IContainer container;

        public IntegrationTests()
        {
            this.container = ConfigureContainer();
        }

        [Fact]
        public void BasicConnectivityTest()
        {
            var config = new ClientConfiguration
                {
                    Servers = new List<Uri> { new Uri("http://192.168.0.9:8091/pools") },
                    UseSsl = false,
                    BucketConfigs =
                        new Dictionary<string, BucketConfiguration>
                            {
                                {
                                    "TTG.MyDoctorOnline.Publishing.Coordination.ContentPublishingSagas",
                                    new BucketConfiguration
                                        {
                                            BucketName = "TTG.MyDoctorOnline.Publishing.Coordination.ContentPublishingSagas",
                                            UseSsl = false,
                                            Password = string.Empty,
                                            PoolConfiguration = new PoolConfiguration
                                                {
                                                    MaxSize = 10,
                                                    MinSize = 5
                                                }
                                        }
                                }
                            }
                };

            using (var couchbaseCluster = new Cluster(config))
            {
                var sagaRepository = new CouchbaseSagaRepository<TestSaga>(couchbaseCluster);
            }
        }

        public void Dispose()
        {
        }

        private IContainer ConfigureContainer()
        {
            var builder = new ContainerBuilder();

            return builder.Build();
        }
    }
}
