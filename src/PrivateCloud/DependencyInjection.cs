using System;
using Microsoft.Extensions.DependencyInjection;
using Amazon.SimpleSystemsManagement;
using Amazon.Runtime;
using NodaTime;
using Amazon.CertificateManager;
using Amazon.Extensions.NETCore.Setup;
using System.Threading.Tasks;
using Amazon.KeyManagementService;
using PrivateCloud.CertificateManagement;
using PrivateCloud.UserManagement;
using Amazon.EC2;
using Amazon.Runtime.CredentialManagement;

namespace PrivateCloud
{
    public class DependencyInjection
    {
        public static IServiceProvider GetServiceProvider(string profile)
        {
            CredentialProfile basicProfile;
            AWSCredentials awsCredentials;
            var sharedFile = new SharedCredentialsFile();
            if (sharedFile.TryGetProfile(profile, out basicProfile) &&
                AWSCredentialsFactory.TryGetAWSCredentials(basicProfile, sharedFile, out awsCredentials))
            {
                var awsOptions = new AWSOptions
                {
                    Credentials = awsCredentials
                };
                

                var serviceCollection = new ServiceCollection();


                // From Nuget
                serviceCollection.AddAWSService<IAmazonSimpleSystemsManagement>(awsOptions);
                serviceCollection.AddAWSService<IAmazonCertificateManager>(awsOptions);
                serviceCollection.AddAWSService<IAmazonKeyManagementService>(awsOptions);
                serviceCollection.AddAWSService<IAmazonEC2>(awsOptions);
                serviceCollection.AddSingleton<IClock>(SystemClock.Instance);


                // Locally Created Files
                serviceCollection.AddSingleton<ICertificateUtils, CertificateUtils>();
                serviceCollection.AddSingleton<ISsmUtils, SsmUtils>();
                serviceCollection.AddSingleton<ICertificateManager, CertificateManager>();
                serviceCollection.AddSingleton<IUserManager, UserManager>();
                serviceCollection.AddSingleton<IBlacklistedUserManager, BlacklistedUserManager>();

                return serviceCollection.BuildServiceProvider();
            }

            throw new Exception($"AWS Profile {profile} was not found in your config file");
        }
    }
}
