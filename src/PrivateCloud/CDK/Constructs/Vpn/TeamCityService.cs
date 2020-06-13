using System;
using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.ECS;
using PrivateCloud.CDK.Constructs.Docker.Containers;

namespace PrivateCloud.CDK.Constructs.Vpn
{
    public class TeamCityServiceProps
    {
        public Cluster Cluster { get; set; }
    }

    public class TeamCityService : Construct
    {
        public TeamCityService(Construct scope, string id, TeamCityServiceProps props) : base(scope, id)
        {
            var teamCity = new TeamCity(this, "TeamCity", new TeamCityProps());

            _ = new Ec2Service(this, "TeamCity Service", new Ec2ServiceProps
            {
                Cluster = props.Cluster,
                AssignPublicIp = false,
                DesiredCount = 1,
                TaskDefinition = teamCity.Task,
                MinHealthyPercent = 0
            });
        }
    }
}
