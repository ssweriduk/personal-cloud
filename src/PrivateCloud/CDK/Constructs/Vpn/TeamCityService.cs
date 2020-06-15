using System;
using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.ECS;
using Amazon.CDK.AWS.ServiceDiscovery;
using PrivateCloud.CDK.Constructs.Docker.Containers;

namespace PrivateCloud.CDK.Constructs.Vpn
{
    public class TeamCityServiceProps
    {
        public Cluster Cluster { get; set; }
        public INamespace PrivateDnsNamespace { get; set; }
    }

    public class TeamCityService : Construct
    {
        public Ec2Service Service { get; set; }

        public TeamCityService(Construct scope, string id, TeamCityServiceProps props) : base(scope, id)
        {
            var teamCity = new TeamCity(this, "TeamCity", new TeamCityProps());

            Service = new Ec2Service(this, "TeamCity Service", new Ec2ServiceProps
            {
                Cluster = props.Cluster,
                AssignPublicIp = false,
                DesiredCount = 1,
                TaskDefinition = teamCity.Task,
                MinHealthyPercent = 0,
                ServiceName = "teamcity",
                CloudMapOptions = new CloudMapOptions
                {
                    CloudMapNamespace = props.PrivateDnsNamespace,
                    DnsRecordType = DnsRecordType.A,
                    Name = "teamcity",
                    DnsTtl = Duration.Seconds(30)
                },
            });

            Service.Connections.AllowFromAnyIpv4(Port.Tcp(8111));
        }
    }
}
