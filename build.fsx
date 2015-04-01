#r "packages/FSharp.Data/lib/net40/FSharp.Data.dll"
#r "packages/FAKE/tools/FakeLib.dll"
#r "System.Xml.Linq.dll"

open Fake
open System.IO
open System.Xml
open System.Xml.Linq
open FSharp.Data

let (+/) firstPath secondPath = Path.Combine(firstPath, secondPath)

let outputPath = "./out/"
let packagingPath = "./build/"

type ProjectConfigurationType = JsonProvider<"./src/MassTransit.Persistence.Couchbase/project.json">

let projectConfigurations =
    dict [
        ("./src/MassTransit.Persistence.Couchbase/",
            ProjectConfigurationType.Load("./src/MassTransit.Persistence.Couchbase/project.json"));
        ]

Target "ListTargets" (fun _ ->
    listTargets()
)

Target "Clean" (fun _ ->
    DeleteDir "./src/NuGetPackages"
    DeleteDir outputPath
    DeleteDir packagingPath
    CreateDir outputPath
    CreateDir packagingPath

    for KeyValue(projectRoot, projectConfig) in projectConfigurations do
        for path in projectConfig.BuildArtifacts do DeleteDir(projectRoot +/ path)
)

open Fake.AssemblyInfoFile

Target "AssemblyInfo" (fun _ ->
    for KeyValue(projectRoot, projectConfig) in projectConfigurations do
        let assemblyInfoFile = string(projectRoot +/ "Properties/AssemblyInfo.cs")
        CreateCSharpAssemblyInfo
            assemblyInfoFile
            [
                Attribute.Title projectConfig.Title
                Attribute.Product projectConfig.Product
                Attribute.Company projectConfig.Company
                Attribute.Description projectConfig.Description
                Attribute.Copyright projectConfig.Copyright
                Attribute.Version projectConfig.Version
                Attribute.FileVersion projectConfig.Version
                Attribute.ComVisible false
            ]
)

open Fake.XMLHelper

Target "LogConfigUpdate" (fun _ ->
    for KeyValue(projectRoot, projectConfig) in projectConfigurations do
        let logConfigFileName = projectRoot +/ "log4net.config"

        if File.Exists(logConfigFileName) then
            let logConfigFile = string(logConfigFileName)
            let logConfigOriginal = new XmlDocument() in
                logConfigOriginal.Load logConfigFile
            let logConfigUpdated =
                XPathReplace
                    "/log4net/appender[@name='EventLogAppender']/applicationName/@value"
                    projectConfig.Title
                    logConfigOriginal
                XPathReplace
                    "/log4net/appender[@name='EventLogAppender']/logName/@value"
                    projectConfig.Product
                    logConfigOriginal
            logConfigUpdated.Save logConfigFile
)

Target "BuildAll" (fun _ ->
    let mode = getBuildParamOrDefault "mode" "Debug"
    let buildParams defaults =
        { defaults with
            Verbosity = Some(Normal)
            Targets = ["Build"]
            Properties =
                [
                    "Optimize", "True"
                    "DebugSymbols", "True"
                    "Configuration", mode
                ]
         }

    build buildParams "./src/MassTransit.Persistence.Couchbase.sln"
        |> DoNothing
)

open Fake.NuGetHelper

Target "Package" (fun _ ->
    for KeyValue(projectRoot, projectConfig) in projectConfigurations do
        let productPackagingPath = packagingPath +/ projectConfig.Product
        let libNet45BuildPath = "./src/" +/ projectConfig.Product +/ "bin/net45/"
        let libNet45PackagePath = packagingPath +/ projectConfig.Product +/ "lib/net45/"

        CleanDirs [libNet45PackagePath]

        CopyFile libNet45PackagePath (libNet45BuildPath +/ projectConfig.Product + ".dll")
        CopyFile libNet45PackagePath (libNet45BuildPath +/ projectConfig.Product + ".pdb")
        CopyFile libNet45PackagePath (libNet45BuildPath +/ projectConfig.Product + ".xml")
        CopyFiles productPackagingPath ["README.md"; "LICENSE.txt"]

        NuGet (fun p -> 
            {p with
                Authors = projectConfig.Authors |> Array.toList
                Project = projectConfig.Product
                Description = projectConfig.Description
                OutputPath = outputPath
                WorkingDir = productPackagingPath
                Version = projectConfig.Version
                AccessKey = getBuildParamOrDefault "nugetkey" ""
                Publish = hasBuildParam "nugetkey"
                Dependencies = getDependencies (projectRoot +/ "packages.config")
                Files = [
                    (("**\*.*"), None, None)
                ]
            })
                "./src/MassTransit.Persistence.Couchbase/MassTransit.Persistence.Couchbase.nuspec"
)

"Clean"
    //==> "CreatePrerequisitePackages"
    ==> "AssemblyInfo"
    ==> "LogConfigUpdate"
    ==> "BuildAll"
    ==> "Package"

RunTargetOrDefault "ListTargets"
