using System;
using System.IO;
using System.Threading.Tasks;
using Amazon.EC2;
using Amazon.EC2.Model;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.X509;
using PrivateCloud.CertificateManagement;

namespace PrivateCloud.UserManagement
{
    public interface IUserManager
    {
        Task CreateUser(string userName, CertificateWithKeyPair serverCertificateWithKeyPair);
        Task<bool> UserExists(string userName);
        Task<string> GetVpnConfigForUser(string userName);
    }

    public class UserManager : IUserManager
    {
        private readonly ICertificateManager _certificateManager;
        private readonly ICertificateUtils _certificateUtils;
        private readonly ISsmUtils _ssmUtils;
        private readonly IAmazonEC2 _amazonEC2;

        public UserManager(ICertificateManager certificateManager, ICertificateUtils certificateUtils, ISsmUtils ssmUtils, IAmazonEC2 amazonEC2)
        {
            _certificateManager = certificateManager;
            _certificateUtils = certificateUtils;
            _ssmUtils = ssmUtils;
            _amazonEC2 = amazonEC2;
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

        public async Task<string> GetVpnConfigForUser(string userName)
        {
            var clientVpnEndpointId = await _ssmUtils.GetVPNServerEndpointID();
            var config = await _amazonEC2.ExportClientVpnClientConfigurationAsync(new ExportClientVpnClientConfigurationRequest
            {
                ClientVpnEndpointId = clientVpnEndpointId
            });

            var userCertificate = await _certificateManager.GetClientCertificate(userName);

            var certificateString = ConvertPemObjectToString(userCertificate.Certificate);
            var privateKeyString = ConvertPemObjectToString(userCertificate.KeyPair);

            return $@"{config.ClientConfiguration}
<cert>
{certificateString}
</cert>
<key>
{privateKeyString}
</key>";
        }


        public async Task CreateUser(string userName, CertificateWithKeyPair serverCertificateWithKeyPair)
        {
            // Needs a domain! Lets see if we can clean these certificates up next
            var userCertificate = _certificateUtils.IssueClientCertificate(userName, serverCertificateWithKeyPair);

            await _certificateManager.UploadClientCertificate(userName, userCertificate, serverCertificateWithKeyPair.Certificate);
        }

        public async Task<bool> UserExists(string userName)
        {
            var clientSsmParameters = _ssmUtils.GetClientCertificateParameters(userName);
            var clientCertificateArnTask = clientSsmParameters.GetCertificateArn();
            var keyPairTask = clientSsmParameters.GetCertificateKeyPair();

            var clientCertificateArn = await clientCertificateArnTask;
            var keyPair = await keyPairTask;

            return !string.IsNullOrWhiteSpace(clientCertificateArn) && !string.IsNullOrWhiteSpace(keyPair);
        }
    }
}
