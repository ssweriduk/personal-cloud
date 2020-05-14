using Amazon.CDK;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using Amazon.Runtime.CredentialManagement;
using Amazon;
using Amazon.Runtime;
using Amazon.Extensions.NETCore.Setup;
using PrivateCloud.CertificateManagement;
using Org.BouncyCastle.Asn1.X509;
using PrivateCloud.UserManagement;

using System.IO;
using CDKEnvironment = Amazon.CDK.Environment;
using PrivateCloud.CDK.Stacks;

namespace PrivateCloud
{
    sealed class Program
    {
        public static async Task Main(string[] args)
        {
            CredentialProfile basicProfile;
            AWSCredentials awsCredentials;
            var sharedFile = new SharedCredentialsFile();
            if (sharedFile.TryGetProfile("pcadmin", out basicProfile) &&
                AWSCredentialsFactory.TryGetAWSCredentials(basicProfile, sharedFile, out awsCredentials))
            {
                var awsOptions = new AWSOptions
                {
                    Credentials = awsCredentials
                };
                var serviceProvider = DependencyInjection.GetServiceProvider(awsOptions);
                var certificateUtils = serviceProvider.GetService<ICertificateUtils>();
                var certificateManager = serviceProvider.GetService<ICertificateManager>();
                var userManager = serviceProvider.GetService<IUserManager>();
                var blacklistedUserManager = serviceProvider.GetService<IBlacklistedUserManager>();
                var ssmUtils = serviceProvider.GetService<ISsmUtils>();




                //var serverCertificate = await certificateManager.GetServerCertificate();
                //if (serverCertificate == null)
                //{
                //    var newCert = certificateUtils.IssueRootCertificate();
                //    await certificateManager.UploadServerCertificate(newCert);
                //    serverCertificate = await certificateManager.GetServerCertificate();
                //}

                //var users = new Users();
                //foreach (var user in users)
                //{
                //    if (!await userManager.UserExists(user))
                //    {
                //        await userManager.CreateUser(user, serverCertificate);
                //    }
                //}



                var serverSsmUtils = ssmUtils.GetServerCertificateParameters();
                var serverCertificateArn = await serverSsmUtils.GetCertificateArn();
                var app = new App();
                new PrivateCloudStack(app, "PrivateCloudStack", new PrivateCloudStackProps
                {
                    ServerCertificateArn = serverCertificateArn,
                    Env = new CDKEnvironment
                    {
                        // Apparently this is bad for prod environments.
                        // These should eventually be set by tc
                        Region = System.Environment.GetEnvironmentVariable("CDK_DEFAULT_REGION"),
                        Account = System.Environment.GetEnvironmentVariable("CDK_DEFAULT_ACCOUNT")
                    },
                });
                
                app.Synth();



                //var blacklistedUsers = new BlacklistedUsers();
                //var crl = await blacklistedUserManager.GenerateCrl(blacklistedUsers);
                //await blacklistedUserManager.UploadCrl(crl);



                //var config = await userManager.GetVpnConfigForUser("steve");
                //using (StreamWriter writer = new StreamWriter("./steve.ovpn"))
                //{
                //    writer.Write(config);
                //}
            }
        }
    }
}
