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

namespace PrivateCloud
{
    public interface IDependencyInjection
    {
        IServiceProvider GetServiceProvider(AWSCredentials awsCredentials);
    }

    public static class DependencyInjection
    {
        public static IServiceProvider GetServiceProvider(AWSOptions awsOptions)
        {
            var serviceCollection = new ServiceCollection();


            // From Nuget
            serviceCollection.AddAWSService<IAmazonSimpleSystemsManagement>(awsOptions);
            serviceCollection.AddAWSService<IAmazonCertificateManager>(awsOptions);
            serviceCollection.AddAWSService<IAmazonKeyManagementService>(awsOptions);
            serviceCollection.AddSingleton<IClock>(SystemClock.Instance);


            // Locally Created Files
            serviceCollection.AddSingleton<ISsmUtils, SsmUtils>();
            serviceCollection.AddSingleton<ICertificateAuthority, CertificateAuthority>();
            serviceCollection.AddSingleton<ICertificateManager, CertificateManager>();

            return serviceCollection.BuildServiceProvider();
        }
    }
}
