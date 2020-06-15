﻿using System;
using System.Collections.Generic;
using Amazon.CDK;
using Amazon.CDK.AWS.ECS;
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
                    Image = ContainerImage.FromRegistry("jetbrains/teamcity-agent:latest"),
                    Logging = LogDriver.AwsLogs(new AwsLogDriverProps
                    {
                        LogGroup = logGroup,
                        StreamPrefix = $"agent_{i + 1}"
                    }),
                    MemoryLimitMiB = 1024,
                    Essential = i == 0,
                    Environment = new Dictionary<string, string>
                    {
                        {  "SERVER_URL", props.ServerUrl }
                    },
                });
            }
        }
    }
}
