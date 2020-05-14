using System;
using Amazon.CDK;
using Amazon.CDK.AWS.ECR;

namespace PrivateCloud.CDK.Constructs.Docker
{
    public class RepositoriesStack : Construct
    {
        public Repository NginxRouterRepository { get; }
        public RepositoriesStack(Construct scope, string id) : base(scope, id)
        {
            NginxRouterRepository = new Repository(this, "Private NGINX Router Repo", new RepositoryProps
            {
                RepositoryName = "private-nginx-router"
            });
        }
    }
}
