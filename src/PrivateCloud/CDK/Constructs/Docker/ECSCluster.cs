using System;
using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.ECS;
using Amazon.CDK.AWS.EFS;
using FileSystem = Amazon.CDK.AWS.EFS.FileSystem;

namespace PrivateCloud.CDK.Constructs.Docker
{
    public class ECSClusterProps
    {
        public Vpc MainVpc { get; set; }
    }

    public class ECSCluster : Construct
    {
        public Cluster Cluster { get; }

        public ECSCluster(Construct scope, string id, ECSClusterProps props) : base(scope, id)
        {
            Cluster = new Cluster(this, "ECS Cluster", new ClusterProps
            {
                Vpc = props.MainVpc,
                ClusterName = "MainECSCluster",
            });


            var asg = Cluster.AddCapacity("EC2 Instances", new AddCapacityOptions
            {
                InstanceType = InstanceType.Of(InstanceClass.BURSTABLE3, InstanceSize.MICRO),
                MachineImage = EcsOptimizedImage.AmazonLinux2(AmiHardwareType.STANDARD),
                DesiredCapacity = 1,
                MaxCapacity = 2,
            });

            var efs = new FileSystem(this, "Shared ECS File System", new FileSystemProps
            {
                Vpc = props.MainVpc,
                Encrypted = true,
                LifecyclePolicy = LifecyclePolicy.AFTER_14_DAYS,
                PerformanceMode = PerformanceMode.GENERAL_PURPOSE,
                ThroughputMode = ThroughputMode.BURSTING,
            });

            efs.Connections.AllowDefaultPortFrom(asg);

            // Manually mount the efs file system
            asg.AddUserData(
                "sudo yum check-update -y",
                "sudo yum upgrade -y",
                "sudo yum install -y amazon-efs-utils",
                "sudo yum install -y nfs-utils",
                "sudo mkdir /mnt/efs",
                $"sudo mount -t efs {efs.FileSystemId}:/ /mnt/efs"
            );
        }
    }
}
