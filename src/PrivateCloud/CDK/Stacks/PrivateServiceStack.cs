using System;
using Amazon.CDK;
using Amazon.CDK.AWS.CloudFormation;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.ECR;
using Amazon.CDK.AWS.ECS;
using PrivateCloud.CDK.Constructs.Vpn;

namespace PrivateCloud.CDK.Stacks
{
    public class PrivateServiceStackProps : Amazon.CDK.NestedStackProps
    {
        public IRepository NginxRouterRepository { get; set; }
        public string NginxRouterRepositoryTag { get; set; }
        public Cluster Cluster { get; set; }
        public Vpc MainVpc { get; set; }
    }

    public class PrivateServiceStack : Amazon.CDK.NestedStack
    {
        public PrivateServiceStack(Construct scope, string id, PrivateServiceStackProps props) : base(scope, id, props)
        {
            // Spin up the directory here for the router to point to
            _ = new PrivateRouting(this, "PrivateRouting", new PrivateRoutingStackProps
            {
                Tag = props.NginxRouterRepositoryTag,
                RouterRepository = props.NginxRouterRepository,
                Cluster = props.Cluster,
                MainVpc = props.MainVpc
            });
        }
    }
}
