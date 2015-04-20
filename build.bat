@echo off
cls

rem TODO: Use Paket project from https://fsprojects.github.io/Paket/index.html
"src\.nuget\NuGet.exe" Install FAKE -OutputDirectory build -ExcludeVersion -Verbosity Detailed
"src\.nuget\NuGet.exe" Install FSharp.Data -OutputDirectory build -ExcludeVersion -Verbosity Detailed

rem .paket\paket.bootstrapper.exe
rem if errorlevel 1 (
rem  exit /b %errorlevel%
rem )

rem .paket\paket.exe restore
rem if errorlevel 1 (
rem   exit /b %errorlevel%
rem )

SET TARGET="ListTargets"

IF NOT [%1]==[] (set TARGET="%1")

rem TODO: Logging does not recognize warnings from msbuild yet
"build\FAKE\tools\Fake.exe" build.fsx %TARGET% --logfile build.log

pause