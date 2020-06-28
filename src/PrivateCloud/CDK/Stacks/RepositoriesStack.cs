using System;
using Amazon.CDK;
using Amazon.CDK.AWS.ECR;
using Amazon.CDK.AWS.SSM;

namespace PrivateCloud.CDK.Stacks
{
    public class RepositoriesStackProps : StackProps
    {
        public string NginxRouterRepositoryName { get; set; }
        public string NginxRouterLatestTag { get; set; }
    }

    public class RepositoriesStack : Stack
    {
        public RepositoriesStack(Construct scope, string id, RepositoriesStackProps props) : base(scope, id, props)
        {
            new Repository(this, "Private NGINX Router Repo", new RepositoryProps
            {
                RepositoryName = props.NginxRouterRepositoryName
            });

            new StringParameter(this, "Private NGINX Router Latest Tag", new StringParameterProps
            {
                ParameterName = "/Docker/private-nginx-router/Latest",
                Type = ParameterType.STRING,
                StringValue = props.NginxRouterLatestTag ?? "Initial Deploy",
            });
        }
    }
}
