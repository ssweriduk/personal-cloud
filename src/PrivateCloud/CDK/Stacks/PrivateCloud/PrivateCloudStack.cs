﻿using Amazon.CDK;
using Amazon.CDK.AWS.ECR;
using Amazon.CDK.AWS.ServiceDiscovery;
using PrivateCloud.CDK.Constructs.ECS;
using PrivateCloud.CDK.Constructs.Networking;
using PrivateCloud.CDK.Stacks.PrivateCloud;

namespace PrivateCloud.CDK.Stacks
{
    public class PrivateCloudStackProps : StackProps
    {
        public string ServerCertificateArn { get; set; }
        public string NginxRouterTag { get; set; }
        public string PublicNginxRouterTag { get; set; }
    }

    public class PrivateCloudStack : Stack
    {
        public PrivateCloudStack(Construct scope, string id, PrivateCloudStackProps props) : base(scope, id, props)
        {
            var vpnStackProps = new ClientVpnProps
            {
                ServerCertificateArn = props.ServerCertificateArn,
                ClientCidrBlock = "172.17.0.0/22",
                EndpointIdSSMKey = "/Vpn/Server/EndpointId"
            };
            _ = new ClientVpn(this, "VpnStack", vpnStackProps);
            var vpcStack = new MainVpc(this, "VpcStack");

            new EcsStack(this, "PrivateECSStack", new EcsStackProps
            {
                MainVpc = vpcStack.Vpc,
            });            
        }
    }
}
