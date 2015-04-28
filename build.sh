#!/bin/bash
clear

set -ev

if [ "${TRAVIS}" != "true" ]; then
    export PKG_DIR=/usr/local
    export LD_LIBRARY_PATH=$PKG_DIR/lib64:$PKG_DIR/lib:$LD_LIBRARY_PATH
    export LD_RUN_PATH=$LD_LIBRARY_PATH
    export PKG_CONFIG_PATH=$PKG_DIR/lib64/pkgconfig:$PKG_CONFIG_PATH
    export MONO_GAC_PREFIX=${PKG_DIR}
    export MONO_PATH=${PKG_DIR}/lib/mono/4.5:${PKG_DIR}/lib/mono/4.0:${PKG_DIR}/lib/mono/3.5:${PKG_DIR}/lib/mono/2.0:${PKG_DIR}/lib/mono/compat-2.0:${PWD}/build/FAKE/tools
    export MONO_CONFIG=${PKG_DIR}/etc/mono/config
    export MONO_CFG_DIR=${PKG_DIR}/etc
    export C_INCLUDE_PATH=${PKG_DIR}/include
    export ACLOCAL_PATH=${PKG_DIR}/share/aclocal
    export MONO_REGISTRY_PATH=~/.mono/registry
fi

#TODO: Use Paket project from https://fsprojects.github.io/Paket/index.html
mono "./src/.nuget/NuGet.exe" Install FAKE -OutputDirectory build -ExcludeVersion -Verbosity Detailed
mono "./src/.nuget/NuGet.exe" Install FSharp.Data -OutputDirectory build -ExcludeVersion -Verbosity Detailed

#.paket\paket.bootstrapper.exe
#if errorlevel 1 (
# exit /b %errorlevel%
#)

#.paket\paket.exe restore
#if errorlevel 1 (
#  exit /b %errorlevel%
#)

ls -Ra1
mono "./build/FAKE/tools/Fake.exe" build.fsx $1 --logfile build.log
