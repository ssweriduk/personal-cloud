using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Amazon.CertificateManager;
using Amazon.CertificateManager.Model;
using Amazon.KeyManagementService;
using Amazon.KeyManagementService.Model;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.X509;

namespace PrivateCloud.CertificateManagement
{
    public interface ICertificateManager
    {
        Task<CertificateWithKeyPair> GetClientCertificate(string clientName);
        Task<CertificateWithKeyPair> GetServerCertificate();
        Task UploadClientCertificate(string clientName, CertificateWithKeyPair certificateWithKeyPair, X509Certificate rootCertificate);
        Task UploadServerCertificate(CertificateWithKeyPair certificateWithKeyPair);
    }

    public class CertificateManager : ICertificateManager
    {
        private readonly ISsmUtils _ssmUtils;
        private readonly IAmazonCertificateManager _amazonCertificateManager;
        private readonly IAmazonKeyManagementService _amazonKeyManagementService;

        public CertificateManager(ISsmUtils ssmUtils, IAmazonCertificateManager amazonCertificateManager, IAmazonKeyManagementService amazonKeyManagementService)
        {
            _ssmUtils = ssmUtils;
            _amazonCertificateManager = amazonCertificateManager;
            _amazonKeyManagementService = amazonKeyManagementService;
        }

        private string ConvertPemObjectToString(object pemObject)
        {
            using (var stringWriter = new StringWriter())
            {
                var pemWriter = new PemWriter(stringWriter);
                pemWriter.WriteObject(pemObject);
                pemWriter.Writer.Flush();

                return stringWriter.ToString();
            }
        }

        private T ConvertStringToPemObject<T>(string pemObjectString)
        {
            using var stringReader = new StringReader(pemObjectString);
            var certificatePemReader = new PemReader(stringReader);
            return (T)certificatePemReader.ReadObject();
        }

        private void WritePemObjectToStream(Stream stream, object pemObject)
        {
            stream.Write(Encoding.UTF8.GetBytes(ConvertPemObjectToString(pemObject)));
        }

        private async Task UploadKeyPair(ICertificateParameters certificateParameters, AsymmetricCipherKeyPair keyPair)
        {
            var keyId = await certificateParameters.GetCertificateKmsKeyId();
            if(string.IsNullOrWhiteSpace(keyId))
            {
                var key = await _amazonKeyManagementService.CreateKeyAsync(new CreateKeyRequest());
                keyId = key.KeyMetadata.KeyId;

                await certificateParameters.SetCertificateKmsKeyId(keyId);
            }

            var keyPairString = ConvertPemObjectToString(keyPair);
            await certificateParameters.SetCertificateKeyPair(keyId, keyPairString);
        }

        private async Task<string> UploadCertificate(CertificateWithKeyPair certificateWithKeyPair, X509Certificate rootCertificate = null)
        {
            using (var certificateStream = new MemoryStream())
            using (var privateKeyStream = new MemoryStream())
            using (var certificateChainStream = new MemoryStream())
            {
                // Write Certificate to stream
                WritePemObjectToStream(certificateStream, certificateWithKeyPair.Certificate);

                // Write Private Key to stream
                WritePemObjectToStream(privateKeyStream, certificateWithKeyPair.KeyPair.Private);

                // Write Certificate Chain to stream
                if(rootCertificate != null)
                {
                    WritePemObjectToStream(certificateChainStream, rootCertificate);
                }

                // Reset the streams
                certificateStream.Position = 0;
                privateKeyStream.Position = 0;
                certificateChainStream.Position = 0;


                var acmCertificateResponse = await _amazonCertificateManager.ImportCertificateAsync(new ImportCertificateRequest
                {
                    Certificate = certificateStream,
                    PrivateKey = privateKeyStream,
                    CertificateChain = rootCertificate != null ? certificateChainStream : default
                });

                return acmCertificateResponse.CertificateArn;
            }
        }

        private async Task UploadCertificateWithKeyPair(ICertificateParameters certificateParameters, CertificateWithKeyPair certificateWithKeyPair, X509Certificate rootCertificate = null)
        {
            var certificateArn = await UploadCertificate(certificateWithKeyPair, rootCertificate);
            var setClientCertificateArnTask = certificateParameters.SetCertificateArn(certificateArn);

            var uploadKeyPairTask = UploadKeyPair(certificateParameters, certificateWithKeyPair.KeyPair);

            await setClientCertificateArnTask;
            await uploadKeyPairTask;
        }

        private async Task<CertificateWithKeyPair> GetCertificateWithKeyPair(ICertificateParameters certificateParameters)
        {
            var certificateArn = await certificateParameters.GetCertificateArn();
            if (string.IsNullOrWhiteSpace(certificateArn))
            {
                return null;
            }
            var certificateResponseTask = _amazonCertificateManager.GetCertificateAsync(new GetCertificateRequest
            {
                CertificateArn = certificateArn
            });

            var keyPairTask = certificateParameters.GetCertificateKeyPair();

            var certificateResponse = await certificateResponseTask;
            var keyPairString = await keyPairTask;

            var certificate = ConvertStringToPemObject<X509Certificate>(certificateResponse.Certificate);
            
            var keyPair = ConvertStringToPemObject<AsymmetricCipherKeyPair>(keyPairString);

            return new CertificateWithKeyPair
            {
                Certificate = certificate,
                KeyPair = keyPair,
            };
        }

        public async Task UploadClientCertificate(string clientName, CertificateWithKeyPair certificateWithKeyPair, X509Certificate rootCertificate)
        {
            var certificateParameters = _ssmUtils.GetClientCertificateParameters(clientName);

            await UploadCertificateWithKeyPair(certificateParameters, certificateWithKeyPair, rootCertificate);
        }

        public async Task UploadServerCertificate(CertificateWithKeyPair certificateWithKeyPair)
        {
            var certificateParameters = _ssmUtils.GetServerCertificateParameters();

            await UploadCertificateWithKeyPair(certificateParameters, certificateWithKeyPair);
        }

        public async Task<CertificateWithKeyPair> GetClientCertificate(string clientName)
        {
            var certificateParameters = _ssmUtils.GetClientCertificateParameters(clientName);

            return await GetCertificateWithKeyPair(certificateParameters);
        }

        public async Task<CertificateWithKeyPair> GetServerCertificate()
        {
            var certificateParameters = _ssmUtils.GetServerCertificateParameters();

            return await GetCertificateWithKeyPair(certificateParameters);
            
        }
    }
}
