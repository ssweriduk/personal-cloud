using System;
using System.Linq;
using Amazon.CDK;
using Amazon.CDK.AWS.ECR;
using Amazon.CDK.AWS.ECS;
using Amazon.CDK.AWS.Logs;

namespace PrivateCloud.CDK.Constructs.Docker.Containers
{
    public class RoutingProps
    {
        public IRepository Repository { get; set; }
        public string Tag { get; set; }
    }

    public class Routing : Construct
    {
        public TaskDefinition Task { get; set; }

        public Routing(Construct scope, string id, RoutingProps props) : base(scope, id)
        {
            Task = new Ec2TaskDefinition(this, "Private Routing Task", new Ec2TaskDefinitionProps
            {
                NetworkMode = NetworkMode.BRIDGE,
            });

            var logGroup = new LogGroup(this, "Private NGINX Router Log Group", new LogGroupProps
            {
                LogGroupName = "/privatecloud/nginx",
                RemovalPolicy = RemovalPolicy.DESTROY,
                Retention = RetentionDays.ONE_WEEK,
            });

            var container = Task.AddContainer("router", new ContainerDefinitionOptions
            {
                Image = ContainerImage.FromEcrRepository(props.Repository, props.Tag),
                Logging = LogDriver.AwsLogs(new AwsLogDriverProps
                {
                    LogGroup = logGroup,
                    StreamPrefix = "nginxcontainer"
                }),
                Essential = true,
                MemoryLimitMiB = 64,
                Cpu = 128,
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
        }
    }
}
