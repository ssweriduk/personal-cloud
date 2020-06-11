using System;
using System.Collections.Generic;
using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.ECR;
using Amazon.CDK.AWS.ECS;
using Amazon.CDK.AWS.EFS;
using Amazon.CDK.AWS.Logs;
using PrivateCloud.CDK.Constructs.Docker.Containers;

namespace PrivateCloud.CDK.Constructs.Vpn
{
    public class PrivateRoutingStackProps
    {
        public IRepository RouterRepository { get; set; }
        public string Tag { get; set; }
        public Cluster Cluster { get; set; }
        public Vpc MainVpc { get; set; }
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

            var privateRoutingSecurityGroup = new SecurityGroup(this, "Private Cloud Security Group", new SecurityGroupProps
            {
                Vpc = props.MainVpc,
                Description = "Security group for Private Cloud",
                AllowAllOutbound = true,
                SecurityGroupName = "Private Cloud SG",
            });
            privateRoutingSecurityGroup.Connections.AllowFromAnyIpv4(Port.Tcp(80));
            privateRoutingSecurityGroup.Connections.AllowFromAnyIpv4(Port.Tcp(443));

            new Ec2Service(this, "Private Cloud Service", new Ec2ServiceProps
            {
                Cluster = props.Cluster,
                AssignPublicIp = false,
                DesiredCount = 1,
                TaskDefinition = routing.Task,
                SecurityGroups = new ISecurityGroup[] { privateRoutingSecurityGroup },
                MinHealthyPercent = 0,
            });
        }
    }
}
