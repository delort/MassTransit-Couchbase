# MassTransit-Couchbase

Couchbase persistent storage implementation for MassTransit.

##Build

Run build script with target parameter:

	build.bat Package

Generated NuGet package located in ./out/ folder.

Other available targets:
 - Clean
 - AssemblyInfo
 - LogConfigUpdate
 - BuildAll
 - Package

Build scripts implemented with [FAKE - F# Make](http://fsharp.github.io/FAKE/ "Go to FAKE website").

##Install

Install NuGet package:

    Install-Package MassTransit.Persistence.Couchbase

##Configure

Add similar code to Autofac module:
```c#
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
```
Make sure you are *not* using [InMemorySagaRepository](https://github.com/MassTransit/MassTransit/blob/ee1eab9b1964b79c99deb5dd445c6075f47157df/src/MassTransit/Saga/InMemorySagaRepository.cs "See the Code") at the same time:
```c#
builder.RegisterType<InMemorySagaRepository<ContentPublishingSagaStateMachineInstance>>()
    .As<ISagaRepository<SomeSagaStateMachineInstance>>()
    .SingleInstance();
```
