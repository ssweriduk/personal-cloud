#!/bin/bash

if [[ -z "$1" ]]; then
    echo "Must provide the name of the client to download" 1>&2
    exit 1
fi

if [[ -z "$2"  ]]; then
    dotnet run -p ../src/PrivateCloud/PrivateCloud.csproj --download-vpn-config="$1"
else
    dotnet run -p ../src/PrivateCloud/PrivateCloud.csproj --download-vpn-config="$1" --output-dir="$2"
fi
