using System;
using System.Collections.Generic;
using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.ECR;
using Amazon.CDK.AWS.ECS;
using Amazon.CDK.AWS.EFS;
using Amazon.CDK.AWS.Logs;
using Amazon.CDK.AWS.ServiceDiscovery;
using PrivateCloud.CDK.Constructs.Docker.Containers;

namespace PrivateCloud.CDK.Constructs.Vpn
{
    public class PrivateRoutingStackProps
    {
        public IRepository RouterRepository { get; set; }
        public string Tag { get; set; }
        public Cluster Cluster { get; set; }
        public INamespace PrivateDnsNamespace { get; set; }
    }

    public class PrivateRouting : Construct
    {
        public PrivateRouting(Construct scope, string id, PrivateRoutingStackProps props) : base(scope, id)
        {
            var routing = new Routing(this, "Routing", new RoutingProps
            {
                Tag = props.Tag,
                Repository = props.RouterRepository,
            });

            var routerService = new Ec2Service(this, "Private Cloud Service", new Ec2ServiceProps
            {
                Cluster = props.Cluster,
                AssignPublicIp = false,
                DesiredCount = 1,
                TaskDefinition = routing.Task,
                MinHealthyPercent = 0,
                ServiceName = "router",
                CloudMapOptions = new CloudMapOptions
                {
                    CloudMapNamespace = props.PrivateDnsNamespace,
                    DnsRecordType = DnsRecordType.A,
                    Name = "vpn",
                    DnsTtl = Duration.Seconds(30)
                },
            });

            routerService.Connections.AllowFromAnyIpv4(Port.Tcp(80));
            routerService.Connections.AllowFromAnyIpv4(Port.Tcp(443));
        }
    }
}
