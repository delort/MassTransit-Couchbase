# MassTransit-Couchbase
[![Travis Build Status](https://img.shields.io/travis/sergef/MassTransit-Couchbase.svg)](https://travis-ci.org/sergef/MassTransit-Couchbase)
[![NuGet Package](https://img.shields.io/nuget/v/MassTransit.Persistence.Couchbase.svg)](https://www.nuget.org/packages/MassTransit.Persistence.Couchbase)

Couchbase persistent storage implementation for MassTransit.

##Build & Package

Run build script with target parameter:

	build.bat BuildAll
or

	./build.sh BuildAll

Generated NuGet package located in ./out/ folder.

Use other targets if needed:
 - **Clean**: remove all build artefacts and extracted nuget packages;
 - **AssemblyInfo**: sync AssemblyInfo.cs file with project settings (project.json);
 - **LogConfigUpdate**: sync log configuration file (log4net.config) with project settings (project.json);
 - **RestorePackages**: restore nuget packages used in solution/projects;
 - **BuildAll**: run build for all configured projects;
 - **Package**: create NuGet packages using project settings and \*.nuspec file(s) if found in the project root;
 - **TestAll**: run xUnit 2.0 tests with xunit.console runner;

Build scripts implemented with [FAKE - F# Make](http://fsharp.github.io/FAKE/ "Go to FAKE website").

##Install

Install NuGet package:

    Install-Package MassTransit.Persistence.Couchbase

##Configure
Implement [ICouchbaseSagaRepositorySettings](https://github.com/sergef/MassTransit-Couchbase/blob/master/src/MassTransit.Persistence.Couchbase/Configuration/ICouchbaseSagaRepositorySettings.cs) interface or use [DefaultCouchbaseSagaRepositorySettings](https://github.com/sergef/MassTransit-Couchbase/blob/master/src/MassTransit.Persistence.Couchbase/DefaultCouchbaseSagaRepositorySettings.cs).
Repository will auto-configure Couchbase Bucket and Design Document accordingly.

Example configuration with Autofac module:
```c#
using Autofac
...
using Couchbase;
using Couchbase.Configuration.Client;
using Couchbase.Core;
...
using MassTransit.Persistence.Couchbase;
...
public class SomeSagaModule : Module
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
                                PoolConfiguration = new PoolConfiguration { MaxSize = 10, MinSize = 5 }
                            }
                        }
                    }
                };
                return new Cluster(config);
            })
            .As<ICluster>()
            .SingleInstance();

        builder.RegisterType<DefaultCouchbaseSagaRepositorySettings<SomeSaga>>()
            .As<ICouchbaseSagaRepositorySettings<SomeSaga>>()
            .SingleInstance();

        builder.RegisterType<CouchbaseSagaRepository<SomeSagaInstance, SomeSaga>>()
            .As<ISagaRepository<SomeSagaStateMachineInstance>>()
            .SingleInstance();
        ...
    }
}
```
Make sure you are **not** using [InMemorySagaRepository](https://github.com/MassTransit/MassTransit/blob/ee1eab9b1964b79c99deb5dd445c6075f47157df/src/MassTransit/Saga/InMemorySagaRepository.cs "See the Code") at the same time:
```c#
builder.RegisterType<InMemorySagaRepository<ContentPublishingSagaStateMachineInstance>>()
    .As<ISagaRepository<SomeSagaStateMachineInstance>>()
    .SingleInstance();
```
