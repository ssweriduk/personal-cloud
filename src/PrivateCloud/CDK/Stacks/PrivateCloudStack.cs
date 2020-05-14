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
            var repositoriesStack = new RepositoriesStack(this, "RepositoriesStack");

            var fargateCluster = new FargateCluster(this, "FargateCluster", new FargateClusterProps
            {
                MainVpc = vpcStack.MainVpc,
            });
            _ = new PrivateRouting(this, "PrivateRouting", new PrivateRoutingStackProps
            {
                Tag = "version_2020-05-13-224127",
                RouterRepository = repositoriesStack.NginxRouterRepository,
                FargateCluster = fargateCluster.Cluster,
            });
        }
    }
}
