using System;
using System.IO;
using System.Threading.Tasks;
using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;
using Amazon.CertificateManager;
using Amazon.CertificateManager.Model;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
using NodaTime;
using System.Text;
using Amazon.KeyManagementService;
using Amazon.KeyManagementService.Model;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto;
using X509Certificate = Org.BouncyCastle.X509.X509Certificate;


namespace PrivateCloud.CertificateManagement
{

    public interface ICertificateAuthority
    {
        Task<CertificateWithKeyPair> GetCertificateAuthority();
    }

    public class CertificateAuthority : ICertificateAuthority
    {
        
        private readonly string SERVER_NAME = "sweriduk.com";
        
        private readonly IAmazonCertificateManager _amazonCertificateManager;
        private readonly IAmazonKeyManagementService _amazonKeyManagementService;
        private readonly ISsmUtils _ssmUtils;


        public CertificateAuthority(IAmazonCertificateManager amazonCertificateManager, IAmazonKeyManagementService amazonKeyManagementService, ISsmUtils ssmUtils)
        {
            _amazonCertificateManager = amazonCertificateManager;
            _amazonKeyManagementService = amazonKeyManagementService;
            _ssmUtils = ssmUtils;
        }

        public async Task<CertificateWithKeyPair> GetCertificateAuthority()
        {
            var serverCaArn = await _ssmUtils.GetServerCaArn();
            if (string.IsNullOrWhiteSpace(serverCaArn))
            {
                var serverCa = GenerateServerCa();
                serverCaArn = await UploadServerCa(serverCa);
                await SaveServerCaArn(serverCaArn);
            }

            return await GetServerCertificateFromACM(serverCaArn);
        }

        private async Task SaveServerCaArn(string serverCaArn)
        {
            await _ssmUtils.SetServerCaArn(serverCaArn);
        }

        private async Task<CertificateWithKeyPair> GetServerCertificateFromACM(string certificateArn)
        {
            var certificateResponseTask = _amazonCertificateManager.GetCertificateAsync(new GetCertificateRequest
            {
                CertificateArn = certificateArn
            });

            var keyPairTask = _ssmUtils.GetServerCaKeyPair();

            var certificateResponse = await certificateResponseTask;
            var keyPairString = await keyPairTask;

            X509Certificate certificate;
            using (var certificateStringReader = new StringReader(certificateResponse.Certificate))
            {
                var certificatePemReader = new PemReader(certificateStringReader);
                certificate = (X509Certificate)certificatePemReader.ReadObject();
            }


            AsymmetricCipherKeyPair keyPair;
            using (var keyPairStringReader = new StringReader(keyPairString))
            {
                var keyPairPemReader = new PemReader(keyPairStringReader);
                keyPair = (AsymmetricCipherKeyPair)keyPairPemReader.ReadObject();
            }

            return new CertificateWithKeyPair
            {
                Certificate = certificate,
                KeyPair = keyPair,
            };
        }

        private CertificateWithKeyPair GenerateServerCa()
        {
            return CertificateUtils.CreateCertificateAuthorityCertificate(new Org.BouncyCastle.Asn1.X509.X509Name($"CN={SERVER_NAME} VPN Server Cert"), null, null);
        }

        private async Task<string> UploadServerCa(CertificateWithKeyPair certificateWithKeyPair)
        {
            ImportCertificateResponse acmCertificateResponse;
            using (var certificateStream = new MemoryStream())
            using (var privateKeyStream = new MemoryStream())
            {
                using (var certificateTextWriter = new StringWriter())
                {
                    var certificatePemWriter = new PemWriter(certificateTextWriter);
                    certificatePemWriter.WriteObject(certificateWithKeyPair.Certificate);
                    certificatePemWriter.Writer.Flush();

                    certificateStream.Write(Encoding.UTF8.GetBytes(certificateTextWriter.ToString()));
                }

                using (var privateKeyTextWriter = new StringWriter())
                {
                    var privateKeyPemWriter = new PemWriter(privateKeyTextWriter);
                    privateKeyPemWriter.WriteObject(certificateWithKeyPair.KeyPair.Private);
                    privateKeyPemWriter.Writer.Flush();

                    privateKeyStream.Write(Encoding.UTF8.GetBytes(privateKeyTextWriter.ToString()));
                }

                certificateStream.Position = 0;
                privateKeyStream.Position = 0;


                // Upload certificate to acm
                acmCertificateResponse = await _amazonCertificateManager.ImportCertificateAsync(new ImportCertificateRequest
                {
                    Certificate = certificateStream,
                    PrivateKey = privateKeyStream
                });
            }

            var keyId = await _ssmUtils.GetServerCaKmsKeyId();
            if (string.IsNullOrWhiteSpace(keyId))
            {
                // Create a kms key to encrypt the key pair
                var key = await _amazonKeyManagementService.CreateKeyAsync(new CreateKeyRequest());
                keyId = key.KeyMetadata.KeyId;

                await _ssmUtils.SetServerCaKmsKeyId(keyId);
            }

            string keyPairString;
            using (var keyPairTextWriter = new StringWriter())
            {
                var keyPairPemWriter = new PemWriter(keyPairTextWriter);
                keyPairPemWriter.WriteObject(certificateWithKeyPair.KeyPair);
                keyPairPemWriter.Writer.Flush();

                keyPairString = keyPairTextWriter.ToString();
            }

            // Upload encrypted private key to SSM
            await _ssmUtils.SetServerCaKeyPair(keyId, keyPairString);

            return acmCertificateResponse.CertificateArn;

        }
    }
}