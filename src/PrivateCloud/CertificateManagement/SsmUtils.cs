using System;
using System.IO;
using System.Threading.Tasks;
using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;


public interface ICertificateParameters
{
    Task<string> GetCertificateKmsKeyId();
    Task<string> GetCertificateArn();
    Task<string> GetCertificateKeyPair();
    Task SetCertificateArn(string serverCaArn);
    Task SetCertificateKmsKeyId(string kmsId);
    Task SetCertificateKeyPair(string keyId, string keyPair);
}

public interface ISsmUtils
{
    ICertificateParameters GetServerCertificateParameters();
    ICertificateParameters GetClientCertificateParameters(string clientName);

    Task<string> GetVPNServerEndpointID();
    Task<string> GetLatestPrivateNginxRouterTag();
    Task<string> GetLatestPublicNginxRouterTag();
}

namespace PrivateCloud.CertificateManagement
{
    public class SsmUtils : ISsmUtils
    {
        private readonly IAmazonSimpleSystemsManagement _amazonSimpleSystemsManagement;

        private readonly string ROOT = "/";

        private readonly string SSM_VPN = "Vpn";
        private readonly string SSM_VPN_SERVER = "Server";
        private readonly string SSM_VPN_CERTIFICATE_ARN = "CertificateArn";
        private readonly string SSM_VPN_KEY_PAIR_KMS_ID = "KeyPairKMSId";
        private readonly string SSM_VPN_KEY_PAIR = "KeyPair";
        private readonly string SSM_VPN_CLIENTS = "Clients";
        private readonly string SSM_VPN_SERVER_ENDPOINT_ID = "EndpointId";

        private readonly string DOCKER_REPOSITORIES = "Docker";
        private readonly string NGINX_ROUTER_REPOSITORY = "private-nginx-router";
        private readonly string PUBLIC_NGINX_ROUTER_REPOSITORY = "public-nginx-router";
        private readonly string LATEST_TAG = "Latest";

        private string GetServerEndpointIdPath() => Path.Join(ROOT, SSM_VPN, SSM_VPN_SERVER, SSM_VPN_SERVER_ENDPOINT_ID);
        private string GetNginxRouterLatestTagPath() => Path.Join(ROOT, DOCKER_REPOSITORIES, NGINX_ROUTER_REPOSITORY, LATEST_TAG);
        private string GetPublicNginxRouterLatestTagPath() => Path.Join(ROOT, DOCKER_REPOSITORIES, PUBLIC_NGINX_ROUTER_REPOSITORY, LATEST_TAG);

        private string GetServerCertificateArnPath() => Path.Join(ROOT, SSM_VPN, SSM_VPN_SERVER, SSM_VPN_CERTIFICATE_ARN);
        private string GetServerCertificateKeyPairPath() => Path.Join(ROOT, SSM_VPN, SSM_VPN_SERVER, SSM_VPN_KEY_PAIR);
        private string GetServerCertificateKmsKeyIdPath() => Path.Join(ROOT, SSM_VPN, SSM_VPN_SERVER, SSM_VPN_KEY_PAIR_KMS_ID);

        private string GetClientCertificateArnPath(string clientName) => Path.Join(ROOT, SSM_VPN, SSM_VPN_CLIENTS, clientName, SSM_VPN_CERTIFICATE_ARN);
        private string GetClientCertificateKeyPairPath(string clientName) => Path.Join(ROOT, SSM_VPN, SSM_VPN_CLIENTS, clientName, SSM_VPN_KEY_PAIR);
        private string GetClientCertificateKmsKeyIdPath(string clientName) => Path.Join(ROOT, SSM_VPN, SSM_VPN_CLIENTS, clientName, SSM_VPN_KEY_PAIR_KMS_ID);

        public SsmUtils(IAmazonSimpleSystemsManagement amazonSimpleSystemsManagement)
        {
            _amazonSimpleSystemsManagement = amazonSimpleSystemsManagement;
        }

        private async Task<string> GetSsmParameterValue(string name)
        {
            try
            {
                var serverCaArn = await _amazonSimpleSystemsManagement.GetParameterAsync(new GetParameterRequest
                {
                    Name = name,
                    WithDecryption = false
                });

                return serverCaArn.Parameter.Value;
            }
            catch (ParameterNotFoundException) { return null; }
        }

        private async Task SetStandardSsmParameterValue(string name, string value)
        {
            await _amazonSimpleSystemsManagement.PutParameterAsync(new PutParameterRequest
            {
                Value = value,
                Name = name,
                Type = ParameterType.String
            });
        }

        private async Task SetSecureSsmParameterValue(string name, string keyId, string value)
        {
            await _amazonSimpleSystemsManagement.PutParameterAsync(new PutParameterRequest
            {
                Value = value,
                Name = name,
                Type = ParameterType.SecureString,
                KeyId = keyId,
            });
        }

        private async Task<string> GetSecureSsmParameterValue(string name)
        {
            try
            {
                var serverCaArn = await _amazonSimpleSystemsManagement.GetParameterAsync(new GetParameterRequest
                {
                    Name = name,
                    WithDecryption = true
                });

                return serverCaArn.Parameter.Value;
            }
            catch (ParameterNotFoundException) { return null; }
        }

        #region Client Keys
        private async Task SetClientCertificateKeyPair(string clientName, string keyId, string keyPair)
        {
            var name = GetClientCertificateKeyPairPath(clientName);
            await SetSecureSsmParameterValue(name, keyId, keyPair);
        }

        private async Task SetClientCertificateKmsKeyId(string clientName, string kmsId)
        {
            var name = GetClientCertificateKmsKeyIdPath(clientName);
            await SetStandardSsmParameterValue(name, kmsId);
        }

        private async Task SetClientCertificateArn(string clientName, string clientCertificateArn)
        {
            var name = GetClientCertificateArnPath(clientName);
            await SetStandardSsmParameterValue(name, clientCertificateArn);
        }

        private async Task<string> GetClientCertificateKeyPair(string clientName)
        {
            var name = GetClientCertificateKeyPairPath(clientName);
            return await GetSecureSsmParameterValue(name);
        }

        private async Task<string> GetClientCertificateKmsKeyId(string clientName)
        {
            var name = GetClientCertificateKmsKeyIdPath(clientName);
            return await GetSsmParameterValue(name);

        }

        private async Task<string> GetClientCertificateArn(string clientname)
        {
            var name = GetClientCertificateArnPath(clientname);
            return await GetSsmParameterValue(name);
        }
        #endregion

        #region Server Keys
        private async Task SetServerCertificateKeyPair(string keyId, string keyPair)
        {
            var name = GetServerCertificateKeyPairPath();
            await SetSecureSsmParameterValue(name, keyId, keyPair);
        }

        private async Task SetServerCertificateKmsKeyId(string kmsId)
        {
            var name = GetServerCertificateKmsKeyIdPath();
            await SetStandardSsmParameterValue(name, kmsId);
        }

        private async Task SetServerCertificateArn(string serverCertificateArn)
        {
            var name = GetServerCertificateArnPath();
            await SetStandardSsmParameterValue(name, serverCertificateArn);
        }

        private async Task<string> GetServerCertificateKeyPair()
        {
            var name = GetServerCertificateKeyPairPath();
            return await GetSecureSsmParameterValue(name);
        }

        private async Task<string> GetServerCertificateKmsKeyId()
        {
            var name = GetServerCertificateKmsKeyIdPath();
            return await GetSsmParameterValue(name);
        }

        private async Task<string> GetServerCertificateArn()
        {
            var name = GetServerCertificateArnPath();
            return await GetSsmParameterValue(name);
        }
        #endregion

        private class ServerCertificateParameters : ICertificateParameters
        {
            private readonly SsmUtils _ssmUtils;

            public ServerCertificateParameters(SsmUtils ssmUtils)
            {
                _ssmUtils = ssmUtils;
            }

            public async Task<string> GetCertificateArn() => await _ssmUtils.GetServerCertificateArn();

            public async Task<string> GetCertificateKeyPair() => await _ssmUtils.GetServerCertificateKeyPair();

            public async Task<string> GetCertificateKmsKeyId() => await _ssmUtils.GetServerCertificateKmsKeyId();

            public async Task SetCertificateArn(string certificateArn) => await _ssmUtils.SetServerCertificateArn(certificateArn);

            public async Task SetCertificateKeyPair(string keyId, string keyPair) => await _ssmUtils.SetServerCertificateKeyPair(keyId, keyPair);

            public async Task SetCertificateKmsKeyId(string kmsId) => await _ssmUtils.SetServerCertificateKmsKeyId(kmsId);
        }

        private class ClientCertificateParameters : ICertificateParameters
        {
            private readonly SsmUtils _ssmUtils;
            private readonly string _clientName;

            public ClientCertificateParameters(SsmUtils ssmUtils, string clientName)
            {
                _ssmUtils = ssmUtils;
                _clientName = clientName;
            }

            public async Task<string> GetCertificateArn() => await _ssmUtils.GetClientCertificateArn(_clientName);

            public async Task<string> GetCertificateKeyPair() => await _ssmUtils.GetClientCertificateKeyPair(_clientName);

            public async Task<string> GetCertificateKmsKeyId() => await _ssmUtils.GetClientCertificateKmsKeyId(_clientName);

            public async Task SetCertificateArn(string certificateArn) => await _ssmUtils.SetClientCertificateArn(_clientName, certificateArn);

            public async Task SetCertificateKeyPair(string keyId, string keyPair) => await _ssmUtils.SetClientCertificateKeyPair(_clientName, keyId, keyPair);

            public async Task SetCertificateKmsKeyId(string kmsId) => await _ssmUtils.SetClientCertificateKmsKeyId(_clientName, kmsId);
        }

        public ICertificateParameters GetServerCertificateParameters() => new ServerCertificateParameters(this);

        public ICertificateParameters GetClientCertificateParameters(string clientName) => new ClientCertificateParameters(this, clientName);

        public async Task<string> GetVPNServerEndpointID() => await GetSsmParameterValue(GetServerEndpointIdPath());

        public async Task<string> GetLatestPrivateNginxRouterTag() => await GetSsmParameterValue(GetNginxRouterLatestTagPath());

        public async Task<string> GetLatestPublicNginxRouterTag() => await GetSsmParameterValue(GetPublicNginxRouterLatestTagPath());
    }
}
