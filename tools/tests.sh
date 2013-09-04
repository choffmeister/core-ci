#!/bin/bash
set -e

xbuild /p:Configuration=Debug /verbosity:quiet CoreCI.sln
mono --runtime=v4.0 libs/nunit-runners/tools/nunit-console.exe CoreCI.Tests/bin/Debug/CoreCI.Tests.dll

exit 0
