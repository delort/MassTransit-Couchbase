Install NuGet package:
	
	Install-Package MassTransit.Persistence.Couchbase

Add code to Autofac Bootstrapper:

	...
	using Autofac
	...
    using Couchbase;
    using Couchbase.Configuration.Client;
    using Couchbase.Core;
	...
    using MassTransit.Persistence.Couchbase;
	...
    public class SomeAutofacModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
			...
	        builder.Register(
	            context =>
	            {
	                var config = new ClientConfiguration
	                {
	                    Servers = new List<Uri> { new Uri("http://machine:8091/pools") },
	                    UseSsl = false,
	                    BucketConfigs = new Dictionary<string, BucketConfiguration>
	                    {
	                        {
	                            "SomeSagaBucket",
	                            new BucketConfiguration
	                            {
	                                BucketName = "SomeSagaBucket",
	                                UseSsl = false,
	                                Password = string.Empty,
	                                PoolConfiguration = new PoolConfiguration { MaxSize = 10, MinSize = 5 }
	                            }
	                        }
	                    }
	                };
	                return new Cluster(config);
	            })
	        .As<ICluster>()
	        .SingleInstance();
	
	        builder.RegisterType<CouchbaseSagaRepository<SomeSagaStateMachineInstance, SomeStateMachine>>()
	            .As<ISagaRepository<SomeSagaStateMachineInstance>>()
	            .PropertiesAutowired()
	            .SingleInstance();
			...
	    }
	}
