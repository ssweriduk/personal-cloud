using System;
using Amazon.CDK;
using Amazon.CDK.AWS.ECS;
using PrivateCloud.CDK.Constructs.ECS.Containers;

namespace PrivateCloud.CDK.Constructs.ECS.Services
{
    public class TeamCityAgentsServiceProps
    {
        public Cluster Cluster { get; set; }
        public string ServerUrl { get; set; }
    }

    public class TeamCityAgentsService : Construct
    {
        public TeamCityAgentsService(Construct scope, string id, TeamCityAgentsServiceProps props) : base(scope, id)
        {
            var teamCityAgents = new TeamCityAgents(this, "TeamCity", new TeamCityAgentsProps
            {
               NumAgents = 1,
               ServerUrl = props.ServerUrl
            });
            
            _ = new Ec2Service(this, "TeamCity Service", new Ec2ServiceProps
            {
                Cluster = props.Cluster,
                AssignPublicIp = false,
                DesiredCount = 1,
                TaskDefinition = teamCityAgents.Task,
                MinHealthyPercent = 0,
                ServiceName = "teamcityagents"
            });
        }
    }
}
