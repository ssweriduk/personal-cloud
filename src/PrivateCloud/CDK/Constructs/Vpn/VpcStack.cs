using System;
using System.Linq;
using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.SSM;

namespace PrivateCloud.CDK.Constructs.Vpn
{

    public class VpcStackProps
    {
        public string PrivateSubnetIdsSSMKey { get; set; }
    }

    public class VpcStack : Construct
    {
        public Vpc MainVpc { get; }

        public VpcStack(Construct scope, string id, VpcStackProps props) : base(scope, id)
        {
            var natGatewayProvider = NatProvider.Instance(new NatInstanceProps
            {
                InstanceType = InstanceType.Of(InstanceClass.BURSTABLE3, InstanceSize.NANO) // "t3.nano"
            });

            MainVpc = new Vpc(this, "MainVpc", new VpcProps
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

            new StringParameter(this, "VPC Private Subnets", new StringParameterProps
            {
                ParameterName = props.PrivateSubnetIdsSSMKey,
                Type = ParameterType.STRING_LIST,
                StringValue = string.Join(',', MainVpc.PrivateSubnets.Select(s => s.SubnetId))
            });
            

            /*
             *
             *const natGatewayProvider = NatProvider.instance({
            instanceType: new InstanceType("t2.nano")
        })

        this.MainVpc = new Vpc(this, "MainVpc", {
            natGatewayProvider,
            cidr: "10.0.0.0/16",
            maxAzs: 1,
            subnetConfiguration: [{
                name: "PublicSubnet",
                cidrMask: 17,
                subnetType: SubnetType.PUBLIC
            }, {
                name: "PrivateSubnet",
                cidrMask: 17,
                subnetType: SubnetType.PRIVATE
            }],
            // Note, natGateways now controls the number of gateway instances, since we
            // passed in the natGatewayProvider prop
            natGateways: 1,
        });
             */
        }
    }
}
