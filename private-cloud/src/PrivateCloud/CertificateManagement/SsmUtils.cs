using System;
using System.IO;
using System.Threading.Tasks;
using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;

public interface ISsmUtils
{
    Task<string> GetServerCaKmsKeyId();
    Task<string> GetServerCaArn();
    Task<string> GetServerCaKeyPair();
    Task SetServerCaArn(string serverCaArn);
    Task SetServerCaKmsKeyId(string kmsId);
    Task SetServerCaKeyPair(string keyId, string keyPair);
}

namespace PrivateCloud.CertificateManagement
{
    public class SsmUtils : ISsmUtils
    {
        private readonly IAmazonSimpleSystemsManagement _amazonSimpleSystemsManagement;

        private readonly string SSM_VPN_KEY = "Vpn";
        private readonly string SSM_VPN_SERVER_CA = "ServerCA";
        private readonly string SSM_VPN_SERVER_CA_CERTIFICATE_ARN_KEY = "CertificateArn";
        private readonly string SSM_VPN_SERVER_CA_KEY_PAIR_KMS_ID = "KeyPairKMSId";
        private readonly string SSM_VPN_SERVER_CA_KEY_PAIR = "KeyPair";
        private readonly string SSM_VPN_CLIENTS_KEY = "Clients";

        private string GetServerCaArnPath() => Path.Join("/", SSM_VPN_KEY, SSM_VPN_SERVER_CA, SSM_VPN_SERVER_CA_CERTIFICATE_ARN_KEY);
        private string GetServerCaKeyPairPath() => Path.Join("/", SSM_VPN_KEY, SSM_VPN_SERVER_CA, SSM_VPN_SERVER_CA_KEY_PAIR);
        private string GetClientsPath() => Path.Join("/", SSM_VPN_KEY, SSM_VPN_CLIENTS_KEY);
        private string GetServerCaKmsKeyIdPath() => Path.Join("/", SSM_VPN_KEY, SSM_VPN_SERVER_CA, SSM_VPN_SERVER_CA_KEY_PAIR_KMS_ID);

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

        public async Task SetServerCaKeyPair(string keyId, string keyPair)
        {
            var name = GetServerCaKeyPairPath();
            await SetSecureSsmParameterValue(name, keyId, keyPair);
        }

        public async Task SetServerCaKmsKeyId(string kmsId)
        {
            var name = GetServerCaKmsKeyIdPath();
            await SetStandardSsmParameterValue(name, kmsId);
        }
        

        public async Task SetServerCaArn(string serverCaArn)
        {
            var name = GetServerCaArnPath();
            await SetStandardSsmParameterValue(name, serverCaArn);
        }
        
        public async Task<string> GetServerCaKeyPair()
        {
            var name = GetServerCaKeyPairPath();
            return await GetSecureSsmParameterValue(name);
        }

        public async Task<string> GetServerCaKmsKeyId()
        {
            var name = GetServerCaKmsKeyIdPath();
            return await GetSsmParameterValue(name);

        }

        public async Task<string> GetServerCaArn()
        {
            var name = GetServerCaArnPath();
            return await GetSsmParameterValue(name);
        }
    }
}
