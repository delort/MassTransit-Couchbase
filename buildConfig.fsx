#I @"build/FSharp.Data/lib/net40"

#r @"FSharp.Data.dll"

open FSharp.Data

module BuildConfig =
    type ProjectConfigurationType = JsonProvider<"./src/MassTransit.Persistence.Couchbase/project.json">
    let Projects =
        dict [
            ("./src/MassTransit.Persistence.Couchbase/",
                ProjectConfigurationType.Load("./src/MassTransit.Persistence.Couchbase/project.json"));
            ]
    let Tests =
        [
            "./src/MassTransit.Persistence.Couchbase.Tests/"
        ]
////    let PrerequisitePackages =
////        dict [
////            ]
    let Solutions =
        [
            "./src/MassTransit.Persistence.Couchbase.sln"
        ]
