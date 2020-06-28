using System;
using Amazon.CDK;
using Amazon.CDK.AWS.CloudFormation;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.ECR;
using Amazon.CDK.AWS.ECS;
using Amazon.CDK.AWS.ServiceDiscovery;
using PrivateCloud.CDK.Constructs.ECS.Services;

namespace PrivateCloud.CDK.Stacks
{
    public class EcsServicesStackProps : Amazon.CDK.NestedStackProps
    {
        public IRepository NginxRouterRepository { get; set; }
        public string NginxRouterRepositoryTag { get; set; }
        public Cluster Cluster { get; set; }
        public Vpc MainVpc { get; set; }
    }

    public class EcsServicesStack : Amazon.CDK.NestedStack
    {
        public EcsServicesStack(Construct scope, string id, EcsServicesStackProps props) : base(scope, id, props)
        {
            var privateDnsNamespace = new PrivateDnsNamespace(this, "Private ECS DNS", new PrivateDnsNamespaceProps
            {
                Vpc = props.MainVpc,
                Name = "sweriduk.com",
            });

            var teamCity = new TeamCityService(this, "TeamCityService", new TeamCityServiceProps
            {
                Cluster = props.Cluster,
                PrivateDnsNamespace = privateDnsNamespace
            });

            _ = new TeamCityAgentsService(this, "TeamCityAgents", new TeamCityAgentsServiceProps
            {
                Cluster = props.Cluster,
                ServerUrl = $"http://{teamCity.Service.CloudMapService.ServiceName}.{privateDnsNamespace.NamespaceName}:8111/ci/"
            });

            _ = new PrivateRoutingService(this, "PrivateRouting", new PrivateRoutingServiceStackProps
            {
                Tag = props.NginxRouterRepositoryTag,
                RouterRepository = props.NginxRouterRepository,
                Cluster = props.Cluster,
                PrivateDnsNamespace = privateDnsNamespace
            });
        }
    }
}
