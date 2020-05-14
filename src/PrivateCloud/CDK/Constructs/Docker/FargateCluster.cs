using System;
using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.ECS;

namespace PrivateCloud.CDK.Constructs.Docker
{
    public class FargateClusterProps
    {
        public Vpc MainVpc { get; set; }
    }

    public class FargateCluster : Construct
    {
        public Cluster Cluster { get; }

        public FargateCluster(Construct scope, string id, FargateClusterProps props) : base(scope, id)
        {
            Cluster = new Cluster(this, "Fargate Cluster", new ClusterProps
            {
                Vpc = props.MainVpc
            });
        }
    }
}
