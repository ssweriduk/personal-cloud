using System;
using Amazon.CDK;
using Amazon.CDK.AWS.ECR;
using Amazon.CDK.AWS.ECS;
using Amazon.CDK.AWS.ServiceDiscovery;
using Amazon.CDK.AWS.SSM;
using PrivateCloud.CDK.Constructs.ECS.Services;

namespace PrivateCloud.CDK.Stacks.PrivateCloud
{
    public class FargateServicesStackProps
    {
        public PrivateDnsNamespace PrivateDnsNamespace { get; set; }
        public Cluster Cluster { get; set; }
    }


    public class FargateServicesStack : NestedStack
    {
        public FargateServicesStack(Construct scope, string id, FargateServicesStackProps props) : base(scope, id)
        {
            var nginxRouterRepository = Repository.FromRepositoryName(this, "NginxRouterRepository", StackInfo.NginxRouterRepositoryName);
            var publicNginxRouterRepository = Repository.FromRepositoryName(this, "PublicNginxRouterRepository", StackInfo.PublicNginxRouterRepositoryName);

            _ = new PrivateRoutingService(this, "PrivateRouting", new PrivateRoutingServiceStackProps
            {
                Tag = StringParameter.FromStringParameterName(this, "PrivateRouterTag", "/Docker/private-nginx-router/Latest").StringValue,
                RouterRepository = nginxRouterRepository,
                Cluster = props.Cluster,
                PrivateDnsNamespace = props.PrivateDnsNamespace
            });

            _ = new PublicRoutingService(this, "PublicRouting", new PublicRoutingServiceStackProps
            {
                Tag = StringParameter.FromStringParameterName(this, "PublicRouterTag", "/Docker/public-nginx-router/Latest").StringValue,
                RouterRepository = publicNginxRouterRepository,
                Cluster = props.Cluster
            });
        }
    }
}
