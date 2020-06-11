#!/bin/bash

if [[ -z "$AWS_REGION" ]]; then
    echo "Must provide the AWS region in which to deploy this stack" 1>&2
    exit 1
fi

if [[ -z "$AWS_ACCOUNT" ]]; then
    echo "Must provide the AWS account in which to deploy this stack" 1>&2
    exit 1
fi

if [[ -z "$AWS_PROFILE" ]]; then
    echo "Must provide the AWS profile in which to deploy this stack" 1>&2
    exit 1
fi

APP="dotnet run -p ../src/PrivateCloud/PrivateCloud.csproj --deployment-type=PrivateCloud"
cdk synth --app "$APP"
cdk deploy --app="$APP"
