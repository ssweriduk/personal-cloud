using System;
using Amazon.CDK;
using PrivateCloud.CDK.Constructs.Docker;
using PrivateCloud.CDK.Constructs.Vpn;

using CDKEnvironment = Amazon.CDK.Environment;
using Environment = System.Environment;

namespace PrivateCloud.CDK.Stacks
{
    public class PrivateCloudStackProps : StackProps
    {
        public string ServerCertificateArn { get; set; }
        public string NginxRouterTag { get; set; }
    }

    public class PrivateCloudStack : Stack
    {
        public PrivateCloudStack(Construct scope, string id, PrivateCloudStackProps props) : base(scope, id, props)
        {
            var vpnStackProps = new VpnStackProps
            {
                ServerCertificateArn = props.ServerCertificateArn,
                ClientCidrBlock = "172.17.0.0/22",
                EndpointIdSSMKey = "/Vpn/Server/EndpointId"
            };
            _ = new VpnStack(this, "VpnStack", vpnStackProps);
            var vpcStack = new VpcStack(this, "VpcStack", new VpcStackProps
            {
                PrivateSubnetIdsSSMKey = "/Vpc/MainVpc/Subnets/Private"
            });


            new PrivateECSStack(this, "PrivateECSStack", new PrivateECSStackProps
            {
                MainVpc = vpcStack.MainVpc,
                NginxRouterRepositoryName = StackInfo.NginxRouterRepositoryName,
                NginxRouterTag = props.NginxRouterTag
            });
        }
    }
}
