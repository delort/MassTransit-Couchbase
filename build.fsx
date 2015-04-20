#r @"build\FSharp.Data\lib\net40\FSharp.Data.dll"
#r @"build\FAKE\tools\FakeLib.dll"
#r "System.Xml.Linq.dll"

open Fake
open System.IO
open System.Xml
open System.Xml.Linq
open FSharp.Data

let (+/) firstPath secondPath = Path.Combine(firstPath, secondPath)

type ProjectConfigurationType = JsonProvider<"./src/MassTransit.Persistence.Couchbase/project.json">

let projectConfigurations =
    dict [
        ("./src/MassTransit.Persistence.Couchbase/",
            ProjectConfigurationType.Load("./src/MassTransit.Persistence.Couchbase/project.json"));
        ]

let testProjectConfigurations =
    [
        "./src/MassTransit.Persistence.Couchbase.Tests/"
    ]
    
let prereqPackageConfigurations =
    dict [
        ]

let outputPath = "./out/"
let packagingPath = "./build/"
        
Target "ListTargets" (fun _ ->
    listTargets()
)

Target "Clean" (fun _ ->
    DeleteDir "./packages"
    DeleteDir outputPath
    CreateDir outputPath

    for KeyValue(projectRoot, projectConfig) in projectConfigurations do
        for path in projectConfig.BuildArtifacts do DeleteDir(projectRoot +/ path)
)

open Fake.NuGetHelper

Target "CreatePrerequisitePackages" (fun _ ->
    for KeyValue(packageWorkDir, packageNuSpec) in prereqPackageConfigurations do
        NuGet (fun p ->
            {p with 
                OutputPath = outputPath
                WorkingDir = packageWorkDir})
            packageNuSpec
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

open Fake.RestorePackageHelper

Target "RestorePackages" (fun _ ->
    RestorePackages()
)

open Fake.XUnit2Helper

Target "TestAll" (fun _ ->
    for testProject in testProjectConfigurations do
        !! (testProject +/ "bin" +/ "*.Tests.dll") 
            |> xUnit2 (fun p -> {p with OutputDir = testProject })
)

open Fake.NuGetHelper

Target "Package" (fun _ ->
    for KeyValue(projectRoot, projectConfig) in projectConfigurations do
        let nuspecPath = projectRoot +/ (projectConfig.Title + ".nuspec")
        if File.Exists(nuspecPath) then
            let productPackagingPath = packagingPath +/ projectConfig.Title
            let libNet45BuildPath = projectRoot +/ "bin/net45/"
            let libNet45PackagePath = packagingPath +/ projectConfig.Title +/ "lib/net45/"

            CleanDirs [libNet45PackagePath]

            CopyFile libNet45PackagePath (libNet45BuildPath +/ projectConfig.Title + ".dll")
            CopyFile libNet45PackagePath (libNet45BuildPath +/ projectConfig.Title + ".pdb")
            CopyFile libNet45PackagePath (libNet45BuildPath +/ projectConfig.Title + ".xml")
            CopyFiles productPackagingPath ["README.md"; "LICENSE.txt"]

            NuGet (fun p -> 
                {p with
                    Title = projectConfig.Title
                    Authors = projectConfig.Authors |> Array.toList
                    Project = projectConfig.Product
                    ReleaseNotes = projectConfig.ReleaseNotes
                    Copyright = projectConfig.Copyright
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
                    nuspecPath
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

"Clean"
    ==> "CreatePrerequisitePackages"
    ==> "AssemblyInfo"
    ==> "LogConfigUpdate"
    ==> "RestorePackages"
    ==> "BuildAll"

"TestAll"

"BuildAll"
    ==> "Package"

RunTargetOrDefault "ListTargets"
