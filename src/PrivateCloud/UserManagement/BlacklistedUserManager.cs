using System;
using System.Threading.Tasks;
using Amazon.EC2;
using NodaTime;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Crypto.Prng;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;
using Org.BouncyCastle.X509.Extension;
using PrivateCloud.CertificateManagement;
using Amazon.EC2.Model;
using System.IO;
using Org.BouncyCastle.OpenSsl;

namespace PrivateCloud.UserManagement
{
    // Should probably use this...
    //https://github.com/OPCFoundation/UA-.NETStandard/blob/master/Stack/Opc.Ua.Core/Security/Certificates/CertificateFactory.cs
    public interface IBlacklistedUserManager
    {
        Task<X509Crl> GenerateCrl(BlacklistedUsers blacklistedUsers);
        Task UploadCrl(X509Crl crl);
    }

    public class BlacklistedUserManager : IBlacklistedUserManager
    {
        private readonly ICertificateManager _certificateManager;
        private readonly IClock _clock;
        private readonly ISsmUtils _ssmUtils;
        private readonly IAmazonEC2 _amazonEC2;

        public BlacklistedUserManager(ICertificateManager certificateManager, IClock clock, ISsmUtils ssmUtils, IAmazonEC2 amazonEC2)
        {
            _certificateManager = certificateManager;
            _clock = clock;
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

        public async Task UploadCrl(X509Crl crl)
        {
            var endpointId = await _ssmUtils.GetVPNServerEndpointID();
            await _amazonEC2.ImportClientVpnClientCertificateRevocationListAsync(new ImportClientVpnClientCertificateRevocationListRequest
            {
                ClientVpnEndpointId = endpointId,
                CertificateRevocationList = ConvertPemObjectToString(crl)
            });
        }

        public async Task<X509Crl> GenerateCrl(BlacklistedUsers blacklistedUsers)
        {
            var certificateAuthority = await _certificateManager.GetServerCertificate();

            // Set up the signing parts of this operation
            var randomGenerator = new CryptoApiRandomGenerator();
            var random = new SecureRandom(randomGenerator);
            var signatureFactory = new Asn1SignatureFactory("SHA256WITHRSA", certificateAuthority.KeyPair.Private, random);

            // Set up the clr generator
            var clrGenerator = new X509V2CrlGenerator();
            clrGenerator.SetIssuerDN(certificateAuthority.Certificate.IssuerDN);
            clrGenerator.SetThisUpdate(_clock.GetCurrentInstant().ToDateTimeUtc());
            clrGenerator.SetNextUpdate(_clock.GetCurrentInstant().ToDateTimeUtc().AddYears(2));

            // Let's add in those certificates now
            foreach (string blacklistedUser in blacklistedUsers)
            {
                var blacklistedCertificate = await _certificateManager.GetClientCertificate(blacklistedUser);
                if (blacklistedCertificate != null)
                {
                    clrGenerator.AddCrlEntry(blacklistedCertificate.Certificate.SerialNumber, _clock.GetCurrentInstant().ToDateTimeUtc(), CrlReason.PrivilegeWithdrawn);
                }
            }

            // I don't know what this does. Maybe we can kill it later
            clrGenerator.AddExtension(X509Extensions.AuthorityKeyIdentifier, false, new AuthorityKeyIdentifierStructure(certificateAuthority.Certificate));

            // For now, let's just set this to one, but normally it's set to one higher
            // than the last crl in the chain. Whatevs
            clrGenerator.AddExtension(X509Extensions.CrlNumber, false, new CrlNumber(BigInteger.One));

            // Sign that request
            return clrGenerator.Generate(signatureFactory);
        }
    }
}
