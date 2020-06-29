#!/bin/bash

# log in to ecr
eval "$(aws ecr get-login --registry-ids 261668588222 --region us-east-1 --no-include-email)"

TAG="version_$(date +%F-%H%M%S)"
IMAGE_TAG="261668588222.dkr.ecr.us-east-1.amazonaws.com/public-nginx-router:$TAG"

docker build -t $IMAGE_TAG ../docker/PublicNginxRouter

docker push $IMAGE_TAG

aws ssm put-parameter --name /Docker/public-nginx-router/Latest --value $TAG --type String --overwrite