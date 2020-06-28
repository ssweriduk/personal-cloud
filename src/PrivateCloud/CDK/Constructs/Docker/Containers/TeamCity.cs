using System;
using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.ECS;
using Amazon.CDK.AWS.Logs;
using Amazon.CDK.AWS.EFS;
using System.Collections.Generic;

namespace PrivateCloud.CDK.Constructs.Docker.Containers
{
    public class TeamCityProps { }

    public class TeamCity : Construct
    {
        public TaskDefinition Task { get; set; }

        public TeamCity(Construct scope, string id, TeamCityProps props) : base(scope, id)
        {
            Task = new Ec2TaskDefinition(this, "Private Routing Task", new Ec2TaskDefinitionProps
            {
                NetworkMode = NetworkMode.AWS_VPC,
            });

            var logGroup = new LogGroup(this, "Private TeamCity Log Group", new LogGroupProps
            {
                LogGroupName = "/privatecloud/teamcity",
                RemovalPolicy = RemovalPolicy.DESTROY,
                Retention = RetentionDays.ONE_WEEK,
            });

            var container = Task.AddContainer("teamcity", new ContainerDefinitionOptions
            {
                Image = ContainerImage.FromRegistry("jetbrains/teamcity-server:2020.1.1-linux"),
                Logging = LogDriver.AwsLogs(new AwsLogDriverProps
                {
                    LogGroup = logGroup,
                    StreamPrefix = "teamcitycontainer"
                }),
                Essential = true,
                MemoryLimitMiB = 2048,
                Cpu = 1024,
                Environment = new Dictionary<string, string>
                {
                    { "TEAMCITY_CONTEXT", "/ci" }
                }
            });
            container.AddPortMappings(new PortMapping
            {
                ContainerPort = 8111,
                HostPort = 8111,
            });

            container.AddMountPoints(new MountPoint
            {
                ContainerPath = "/data/teamcity_server/datadir",
                SourceVolume = "teamcity-data"
            });
            container.AddMountPoints(new MountPoint
            {
                ContainerPath = "/opt/teamcity/logs",
                SourceVolume = "teamcity-logs"
            });
            container.AddMountPoints(new MountPoint
            {
                ContainerPath = "/opt/teamcity/conf/server.xml",
                SourceVolume = "teamcity-conf"
            });

            Task.AddVolume(new Volume
            {
                Name = "teamcity-data",
                Host = new Host
                {
                    SourcePath = "/mnt/efs/teamcity/data"
                }
            });
            Task.AddVolume(new Volume
            {
                Name = "teamcity-logs",
                Host = new Host
                {
                    SourcePath = "/mnt/efs/teamcity/logs"
                }
            });
            Task.AddVolume(new Volume
            {
                Name = "teamcity-conf",
                Host = new Host
                {
                    SourcePath = "/mnt/efs/teamcity/conf/server.xml"
                }
            });
        }
    }
}
