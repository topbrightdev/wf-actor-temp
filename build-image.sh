#!/bin/sh

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" >/dev/null 2>&1 && pwd )"

echo $DIR

# Compile mothership actor under ./ms_standalone
cd $DIR/ms_standalone
dotnet publish --configuration Release --runtime linux-x64 --framework netcoreapp3.1 -p:PublishSingleFile=true --self-contained true

# Compile satellite actor under ./sat_standalone
cd $DIR/sat_standalone
dotnet publish --configuration Release --runtime linux-x64 --framework netcoreapp3.1 -p:PublishSingleFile=true --self-contained true

# create new docker image that contains both rmb and csharp actor
cd $DIR
docker build -t room-message-bus:local --platform linux/amd64 .
