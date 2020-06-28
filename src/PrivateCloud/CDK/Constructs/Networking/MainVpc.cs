using System;
using System.Linq;
using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.SSM;

namespace PrivateCloud.CDK.Constructs.Networking
{
    public class MainVpc : Construct
    {
        public Vpc Vpc { get; }

        public MainVpc(Construct scope, string id) : base(scope, id)
        {
            var natGatewayProvider = NatProvider.Instance(new NatInstanceProps
            {
                InstanceType = InstanceType.Of(InstanceClass.BURSTABLE3, InstanceSize.NANO) // "t3.nano"
            });

            Vpc = new Vpc(this, "MainVpc", new VpcProps
            {
                NatGatewayProvider = natGatewayProvider,
                NatGateways = 1,
                Cidr = "10.0.0.0/16",
                MaxAzs = 1,
                SubnetConfiguration = new SubnetConfiguration[]
                {
                    new SubnetConfiguration
                    {
                        Name = "Public Subnet",
                        CidrMask = 17,
                        SubnetType = SubnetType.PUBLIC
                    },
                    new SubnetConfiguration
                    {
                        Name = "Private Subnet",
                        CidrMask = 17,
                        SubnetType = SubnetType.PRIVATE
                    }
                }
            });
        }
    }
}
