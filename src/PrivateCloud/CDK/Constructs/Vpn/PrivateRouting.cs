using System;
using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.ECR;
using Amazon.CDK.AWS.ECS;
using Amazon.CDK.AWS.Logs;

namespace PrivateCloud.CDK.Constructs.Vpn
{
    public class PrivateRoutingStackProps
    {
        public Repository RouterRepository { get; set; }
        public string Tag { get; set; }
        public Cluster FargateCluster { get; set; }
    }

    public class PrivateRouting : Construct
    {
        public PrivateRouting(Construct scope, string id, PrivateRoutingStackProps props) : base(scope, id)
        {
            var logGroup = new LogGroup(this, "Private NGINX Router Log Group", new LogGroupProps
            {
                LogGroupName = "/nginx/private",
                RemovalPolicy = RemovalPolicy.DESTROY,
                Retention = RetentionDays.ONE_WEEK,
            });

            var nginxRouterTask = new FargateTaskDefinition(this, "Private NGINX Router Task", new FargateTaskDefinitionProps
            {
                Cpu = 256,
                MemoryLimitMiB = 512,
            });
            var container = nginxRouterTask.AddContainer("router", new ContainerDefinitionOptions
            {
                Image = ContainerImage.FromEcrRepository(props.RouterRepository, props.Tag),
                Logging = LogDriver.AwsLogs(new AwsLogDriverProps
                {
                    LogGroup = logGroup,
                    StreamPrefix = "nginxcontainer"
                }),
            });
            container.AddPortMappings(new PortMapping
            {
                ContainerPort = 443,
                HostPort = 443,
            }, new PortMapping
            {
                ContainerPort = 80,
                HostPort = 80,
            });

            new FargateService(this, "Private NGINX Router Service", new FargateServiceProps
            {
                Cluster = props.FargateCluster,
                AssignPublicIp = false,
                DesiredCount = 1,
                TaskDefinition = nginxRouterTask,
            });
        }
    }
}
