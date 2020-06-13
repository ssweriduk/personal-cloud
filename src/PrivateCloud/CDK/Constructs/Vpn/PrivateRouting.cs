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

            new Ec2Service(this, "Private Cloud Service", new Ec2ServiceProps
            {
                Cluster = props.Cluster,
                AssignPublicIp = false,
                DesiredCount = 1,
                TaskDefinition = routing.Task,
                MinHealthyPercent = 0,
            });
        }
    }
}
