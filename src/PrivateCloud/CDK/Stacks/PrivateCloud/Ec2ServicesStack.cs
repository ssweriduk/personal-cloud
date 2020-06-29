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
    public class Ec2ServicesStackProps : Amazon.CDK.NestedStackProps
    {
        public Cluster Cluster { get; set; }
        public Vpc MainVpc { get; set; }
        public INamespace PrivateDnsNamespace { get; set; }
    }

    public class Ec2ServicesStack : Amazon.CDK.NestedStack
    {
        public Ec2ServicesStack(Construct scope, string id, Ec2ServicesStackProps props) : base(scope, id, props)
        {
            var teamCity = new TeamCityService(this, "TeamCityService", new TeamCityServiceProps
            {
                Cluster = props.Cluster,
                PrivateDnsNamespace = props.PrivateDnsNamespace
            });

            _ = new TeamCityAgentsService(this, "TeamCityAgents", new TeamCityAgentsServiceProps
            {
                Cluster = props.Cluster,
                ServerUrl = $"http://{teamCity.Service.CloudMapService.ServiceName}.{props.PrivateDnsNamespace.NamespaceName}:8111/ci/"
            });
        }
    }
}
