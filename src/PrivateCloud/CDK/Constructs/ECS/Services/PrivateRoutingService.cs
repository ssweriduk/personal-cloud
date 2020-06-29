using System;
using System.Collections.Generic;
using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.ECR;
using Amazon.CDK.AWS.ECS;
using Amazon.CDK.AWS.EFS;
using Amazon.CDK.AWS.Logs;
using Amazon.CDK.AWS.ServiceDiscovery;
using PrivateCloud.CDK.Constructs.ECS.Containers;

namespace PrivateCloud.CDK.Constructs.ECS.Services
{
    public class PrivateRoutingServiceStackProps
    {
        public IRepository RouterRepository { get; set; }
        public string Tag { get; set; }
        public INamespace PrivateDnsNamespace { get; set; }
        public Cluster Cluster { get; set; }
    }

    public class PrivateRoutingService : Construct
    {
        public PrivateRoutingService(Construct scope, string id, PrivateRoutingServiceStackProps props) : base(scope, id)
        {
            var routing = new Routing(this, "Routing", new RoutingProps
            {
                Tag = props.Tag,
                Repository = props.RouterRepository,
            });

            var routerService = new FargateService(this, "Private Cloud Service", new FargateServiceProps
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
