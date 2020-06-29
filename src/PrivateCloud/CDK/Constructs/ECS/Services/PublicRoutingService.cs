using System;
using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.ECR;
using Amazon.CDK.AWS.ECS;
using Amazon.CDK.AWS.ServiceDiscovery;
using PrivateCloud.CDK.Constructs.ECS.Containers;

namespace PrivateCloud.CDK.Constructs.ECS.Services
{
    public class PublicRoutingServiceStackProps
    {
        public IRepository RouterRepository { get; set; }
        public string Tag { get; set; }
        public Cluster Cluster { get; set; }
    }

    public class PublicRoutingService : Construct
    {
        public PublicRoutingService(Construct scope, string id, PublicRoutingServiceStackProps props) : base(scope, id)
        {
            var routing = new PublicRouting(this, "Public Routing", new PublicRoutingProps
            {
                Tag = props.Tag,
                Repository = props.RouterRepository,
            });

            var routerService = new FargateService(this, "Public Cloud Service", new FargateServiceProps
            {
                Cluster = props.Cluster,
                AssignPublicIp = true,
                DesiredCount = 1,
                TaskDefinition = routing.Task,
                MinHealthyPercent = 0,
                ServiceName = "publicrouter",
            });

            routerService.Connections.AllowFromAnyIpv4(Port.Tcp(80));
            routerService.Connections.AllowFromAnyIpv4(Port.Tcp(443));
        }
    }
}
