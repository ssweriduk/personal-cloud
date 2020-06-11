using System;
using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.ECS;
using Amazon.CDK.AWS.Logs;
using Amazon.CDK.AWS.EFS;

namespace PrivateCloud.CDK.Constructs.Docker.Containers
{
    public class TeamCityProps
    {
        public Ec2TaskDefinition Task { get; set; }
    }

    public class TeamCity : Construct
    {
        public ContainerDefinition Container { get; }

        public TeamCity(Construct scope, string id, TeamCityProps props) : base(scope, id)
        {
            var logGroup = new LogGroup(this, "Private TeamCity Log Group", new LogGroupProps
            {
                LogGroupName = "/privatecloud/teamcity",
                RemovalPolicy = RemovalPolicy.DESTROY,
                Retention = RetentionDays.ONE_WEEK,
            });

            Container = props.Task.AddContainer("teamcity", new ContainerDefinitionOptions
            {
                Image = ContainerImage.FromRegistry("jetbrains/teamcity-server:latest"),
                Logging = LogDriver.AwsLogs(new AwsLogDriverProps
                {
                    LogGroup = logGroup,
                    StreamPrefix = "teamcitycontainer"
                }),
                Essential = false,
                MemoryLimitMiB = 256,
                Cpu = 1024,
            });
            Container.AddPortMappings(new PortMapping
            {
                ContainerPort = 8111,
                HostPort = 8111,
            });

            Container.AddMountPoints(new MountPoint
            {
                ContainerPath = "/data/teamcity_server/datadir",
                SourceVolume = "teamcity-data"
            });
            Container.AddMountPoints(new MountPoint
            {
                ContainerPath = "/opt/teamcity/logs",
                SourceVolume = "teamcity-logs"
            });

            props.Task.AddVolume(new Volume
            {
                Name = "teamcity-data",
                Host = new Host
                {
                    SourcePath = "/mnt/efs/teamcity/data"
                }
            });
            props.Task.AddVolume(new Volume
            {
                Name = "teamcity-logs",
                Host = new Host
                {
                    SourcePath = "/mnt/efs/teamcity/logs"
                }
            });
        }
    }
}
