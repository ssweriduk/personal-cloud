using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.ECR;
using Amazon.CDK.AWS.SSM;
using PrivateCloud.CDK.Constructs.ECS;

namespace PrivateCloud.CDK.Stacks
{
    public class EcsStackProps : NestedStackProps
    {
        public Vpc MainVpc { get; set; }
        public string NginxRouterRepositoryName { get; set; }
        public string NginxRouterTag { get; set; }
    }

    public class EcsStack : NestedStack
    {
        public EcsStack(Construct scope, string id, EcsStackProps props) : base(scope, id, props)
        {
            var eCSCluster = new ECSCluster(this, "Cluster", new ECSClusterProps
            {
                MainVpc = props.MainVpc,
            });

            var nginxRouterRepository = Repository.FromRepositoryName(this, "NginxRouterRepository", props.NginxRouterRepositoryName);

            new EcsServicesStack(this, "PrivateServiceStack", new EcsServicesStackProps
            {
                MainVpc = props.MainVpc,
                Cluster = eCSCluster.Cluster,
                NginxRouterRepository = nginxRouterRepository,
                NginxRouterRepositoryTag = props.NginxRouterTag
            });

            new StringParameter(this, "Main ECS Cluster", new StringParameterProps
            {
                ParameterName = "/ECS/Clusters/Main",
                Type = ParameterType.STRING,
                StringValue = eCSCluster.Cluster.ClusterArn
            });
        }
    }
}
