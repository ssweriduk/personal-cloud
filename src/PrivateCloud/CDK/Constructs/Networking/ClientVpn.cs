using System;
using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.Logs;
using Amazon.CDK.AWS.SSM;
using static Amazon.CDK.AWS.EC2.CfnClientVpnEndpoint;

namespace PrivateCloud.CDK.Constructs.Networking
{
    public class ClientVpnProps
    {
        public string ServerCertificateArn { get; set; }
        public string ClientCidrBlock { get; set; }
        public string EndpointIdSSMKey { get; set; }
    }

    public class ClientVpn : Construct
    {
        public ClientVpn(Construct scope, string id, ClientVpnProps props) : base(scope, id)
        {

            // Client VPN Endpoint
            var vpnLogGroup = new LogGroup(this, "VpnLogGroup", new LogGroupProps
            {
                LogGroupName = "vpn/",
                RemovalPolicy = RemovalPolicy.DESTROY,
                Retention = RetentionDays.ONE_YEAR,
            });

            var vpnLogStream = new LogStream(this, "VpnEndpointLogStream", new LogStreamProps
            {
                LogGroup = vpnLogGroup,
                LogStreamName = "defaultendpoint",
                RemovalPolicy = RemovalPolicy.DESTROY
            });


            // We will manually associate a subnet as an on/off switch
            var endpoint = new CfnClientVpnEndpoint(this, "VpnEndpoint", new CfnClientVpnEndpointProps
            {
                AuthenticationOptions = new ClientAuthenticationRequestProperty[1]
                {
                    new ClientAuthenticationRequestProperty
                    {
                        MutualAuthentication = new CertificateAuthenticationRequestProperty
                        {
                            ClientRootCertificateChainArn = props.ServerCertificateArn,
                        },
                        Type = "certificate-authentication",
                    }
                },
                ClientCidrBlock = props.ClientCidrBlock,
                ServerCertificateArn = props.ServerCertificateArn,
                SplitTunnel = true,
                TransportProtocol = "tcp",
                ConnectionLogOptions = new ConnectionLogOptionsProperty
                {
                    Enabled = true,
                    CloudwatchLogGroup = vpnLogGroup.LogGroupName,  // This seems to be incorrect
                    CloudwatchLogStream = vpnLogStream.LogStreamName,
                },
                DnsServers = new string[] { "10.0.0.2" } 
            });
            new CfnClientVpnAuthorizationRule(this, "VpnAuthorizations", new CfnClientVpnAuthorizationRuleProps
            {
                TargetNetworkCidr = "0.0.0.0/0",
                AuthorizeAllGroups = true,
                ClientVpnEndpointId = endpoint.Ref
            });

            new StringParameter(this, "Vpn EndpointID SSM Key", new StringParameterProps
            {
                ParameterName = props.EndpointIdSSMKey,
                Type = ParameterType.STRING,
                StringValue = endpoint.Ref
            });
        }
    }
}
