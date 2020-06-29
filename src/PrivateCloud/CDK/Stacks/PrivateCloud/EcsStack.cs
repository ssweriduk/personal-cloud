using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.ECR;
using Amazon.CDK.AWS.ServiceDiscovery;
using Amazon.CDK.AWS.SSM;
using PrivateCloud.CDK.Constructs.ECS;
using PrivateCloud.CDK.Stacks.PrivateCloud;

namespace PrivateCloud.CDK.Stacks
{
    public class EcsStackProps : NestedStackProps
    {
        public Vpc MainVpc { get; set; }
    }

    public class EcsStack : NestedStack
    {
        public EcsStack(Construct scope, string id, EcsStackProps props) : base(scope, id, props)
        {
            var eCSCluster = new ECSCluster(this, "Cluster", new ECSClusterProps
            {
                MainVpc = props.MainVpc,
            });

            var privateDnsNamespace = new PrivateDnsNamespace(this, "Private ECS DNS", new PrivateDnsNamespaceProps
            {
                Vpc = props.MainVpc,
                Name = "sweriduk.com",
            });

            new Ec2ServicesStack(this, "PrivateServiceStack", new Ec2ServicesStackProps
            {
                MainVpc = props.MainVpc,
                Cluster = eCSCluster.Cluster,
                PrivateDnsNamespace = privateDnsNamespace
            });

            new FargateServicesStack(this, "FargateServicesStack", new FargateServicesStackProps
            {
                Cluster = eCSCluster.Cluster,
                PrivateDnsNamespace = privateDnsNamespace
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
