using System;
using Amazon.CDK;
using Amazon.CDK.AWS.ECR;
using Amazon.CDK.AWS.ECS;
using Amazon.CDK.AWS.Logs;

namespace PrivateCloud.CDK.Constructs.ECS.Containers
{
    public class PublicRoutingProps
    {
        public IRepository Repository { get; set; }
        public string Tag { get; set; }
    }

    public class PublicRouting : Construct
    {
        public TaskDefinition Task { get; }

        public PublicRouting(Construct scope, string id, PublicRoutingProps props) : base(scope, id)
        {
            Task = new FargateTaskDefinition(this, "Public Nginx Router", new FargateTaskDefinitionProps
            {
                Cpu = 256,
                MemoryLimitMiB = 512,
            });

            var logGroup = new LogGroup(this, "Public NGINX Router Log Group", new LogGroupProps
            {
                LogGroupName = "/publiccloud/nginx",
                RemovalPolicy = RemovalPolicy.DESTROY,
                Retention = RetentionDays.ONE_WEEK,
            });

            var container = Task.AddContainer("publicrouter", new ContainerDefinitionOptions
            {
                Image = ContainerImage.FromEcrRepository(props.Repository, props.Tag),
                Logging = LogDriver.AwsLogs(new AwsLogDriverProps
                {
                    LogGroup = logGroup,
                    StreamPrefix = "nginxcontainer"
                }),
                Essential = true
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
