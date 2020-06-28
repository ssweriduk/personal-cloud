using System;
using System.Collections.Generic;
using Amazon.CDK;
using Amazon.CDK.AWS.ECS;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.Logs;

namespace PrivateCloud.CDK.Constructs.Docker.Containers
{
    public class TeamCityAgentsProps
    {
        public int NumAgents { get; set; }
        public string ServerUrl { get; set; }
    }

    public class TeamCityAgents: Construct
    {
        public TaskDefinition Task { get; set; }

        public TeamCityAgents(Construct scope, string id, TeamCityAgentsProps props) : base(scope, id)
        {
            Task = new Ec2TaskDefinition(this, "TeamCity Agent Task", new Ec2TaskDefinitionProps
            {
                NetworkMode = NetworkMode.BRIDGE,
                TaskRole = Role.FromRoleArn(this, "TeamCityAgentRole", "arn:aws:iam::261668588222:role/TeamCityAgentRole")
            });

            var logGroup = new LogGroup(this, "TeamCity Agent Log Group", new LogGroupProps
            {
                LogGroupName = "/privatecloud/teamcityagents",
                RemovalPolicy = RemovalPolicy.DESTROY,
                Retention = RetentionDays.ONE_WEEK,
            });

            for (var i = 0; i < props.NumAgents; i++)
            {
                Task.AddContainer($"teamcityagent_{i + 1}", new ContainerDefinitionOptions
                {
                    Image = ContainerImage.FromRegistry("jetbrains/teamcity-agent:2020.1.1-linux-sudo"),
                    Logging = LogDriver.AwsLogs(new AwsLogDriverProps
                    {
                        LogGroup = logGroup,
                        StreamPrefix = $"agent_{i + 1}"
                    }),
                    MemoryLimitMiB = 1024,
                    Essential = i == 0,
                    Environment = new Dictionary<string, string>
                    {
                        { "SERVER_URL", props.ServerUrl },
                        { "DOCKER_IN_DOCKER", "start" }
                    },
                    Privileged = true,
                });
            }
        }
    }
}
