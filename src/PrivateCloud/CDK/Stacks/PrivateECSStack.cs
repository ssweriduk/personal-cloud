using System;
using Amazon.CDK;
using Amazon.CDK.AWS.CloudFormation;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.ECR;
using PrivateCloud.CDK.Constructs.Docker;

namespace PrivateCloud.CDK.Stacks
{
    public class PrivateECSStackProps : Amazon.CDK.NestedStackProps
    {
        public Vpc MainVpc { get; set; }
        public string NginxRouterRepositoryName { get; set; }
        public string NginxRouterTag { get; set; }
    }

    public class PrivateECSStack : Amazon.CDK.NestedStack
    {
        public PrivateECSStack(Construct scope, string id, PrivateECSStackProps props) : base(scope, id, props)
        {
            var eCSCluster = new ECSCluster(this, "Cluster", new ECSClusterProps
            {
                MainVpc = props.MainVpc,
            });

            var nginxRouterRepository = Repository.FromRepositoryName(this, "NginxRouterRepository", props.NginxRouterRepositoryName);

            new PrivateServiceStack(this, "PrivateServiceStack", new PrivateServiceStackProps
            {
                MainVpc = props.MainVpc,
                Cluster = eCSCluster.Cluster,
                NginxRouterRepository = nginxRouterRepository,
                NginxRouterRepositoryTag = props.NginxRouterTag
            });
        }
    }
}
