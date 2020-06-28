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
using McMaster.Extensions.CommandLineUtils;

namespace PrivateCloud
{
    sealed class Program
    {
        private enum DeploymentType
        {
            NoDeployment,
            PrivateCloud,
            DockerRepositories,
            ClientVpnCertificates,
            ClientVpnBlacklist,
            Roles
        }

        public static async Task SynthesizeDockerRepositoriesStack(IServiceProvider serviceProvider)
        {
            Console.WriteLine("DEPLOYING DOCKER REPOSITORIES");

            var ssmUtils = serviceProvider.GetService<ISsmUtils>();
            var nginxRouterLatestTag = await ssmUtils.GetLatestPrivateNginxRouterTag();
            var app = new App();
            new RepositoriesStack(app, "RepositoriesStack", new RepositoriesStackProps
            {
                NginxRouterRepositoryName = StackInfo.NginxRouterRepositoryName,
                NginxRouterLatestTag = nginxRouterLatestTag,
            });
            app.Synth();
        }

        public static async Task SynthesizePrivateCloudFormationStacks(IServiceProvider serviceProvider)
        {
            Console.WriteLine("DEPLOYING PRIVATE CLOUD");
            var ssmUtils = serviceProvider.GetService<ISsmUtils>();
            var serverSsmUtils = ssmUtils.GetServerCertificateParameters();
            var serverCertificateArn = await serverSsmUtils.GetCertificateArn();
            var nginxRouterLatestTag = await ssmUtils.GetLatestPrivateNginxRouterTag();
            var app = new App();
            new PrivateCloudStack(app, "PrivateCloudStack", new PrivateCloudStackProps
            {
                ServerCertificateArn = serverCertificateArn,
                Env = new CDKEnvironment
                {
                    Region = System.Environment.GetEnvironmentVariable("AWS_REGION"),
                    Account = System.Environment.GetEnvironmentVariable("AWS_ACCOUNT"),
                },
                NginxRouterTag = nginxRouterLatestTag,
            });

            app.Synth();
        }

        private static async Task DeployClientVpnBlacklist(IServiceProvider serviceProvider)
        {
            Console.WriteLine("DEPLOYING CLIENT VPN BLACKLIST");
            var blacklistedUserManager = serviceProvider.GetService<IBlacklistedUserManager>();
            var blacklistedUsers = new BlacklistedUsers();
            var crl = await blacklistedUserManager.GenerateCrl(blacklistedUsers);
            await blacklistedUserManager.UploadCrl(crl);
        }

        private static async Task DeployClientVpnClientCertificates(IServiceProvider serviceProvider)
        {
            Console.WriteLine("DEPLOYING CLIENT VPN CERTIFICATES");
            var certificateUtils = serviceProvider.GetService<ICertificateUtils>();
            var certificateManager = serviceProvider.GetService<ICertificateManager>();
            var userManager = serviceProvider.GetService<IUserManager>();

            var serverCertificate = await certificateManager.GetServerCertificate();
            if (serverCertificate == null)
            {
                var newCert = certificateUtils.IssueRootCertificate();
                await certificateManager.UploadServerCertificate(newCert);
                serverCertificate = await certificateManager.GetServerCertificate();
            }

            var users = new Users();
            foreach (var user in users)
            {
                if (!await userManager.UserExists(user))
                {
                    await userManager.CreateUser(user, serverCertificate);
                }
            }
        }

        private static void DeployPrivateCloudRoles()
        {
            Console.WriteLine("DEPLOYING PRIVATE CLOUD ROLES");
            var app = new App();
            new RolesStack(app, "RolesStack");
            app.Synth();
        }

        public static void Main(string[] args)
        {
            var app = new CommandLineApplication();

            app.HelpOption("-h|--help");
            var optionDeploymentType = app.Option("-d|--deployment-type <DEPLOYMENT_TYPE>", "Whether or not we are deploying something to the cloud", CommandOptionType.SingleOrNoValue);
            var optionDownloadVpnConfig = app.Option("-c|--download-vpn-config <CLIENT_NAME>", "Download the vpn config for a client", CommandOptionType.SingleOrNoValue);
            var optionDownloadOutputDir = app.Option("-o|--output-dir <DIRECTORY>", "Specify the output directory for downloaded files", CommandOptionType.SingleOrNoValue);

            DeploymentType GetDeploymentType()
            {
                var deploymentTypeValueString = optionDeploymentType.HasValue() ? optionDeploymentType.Value() : DeploymentType.NoDeployment.ToString();
                DeploymentType deploymentType;
                if (Enum.TryParse(deploymentTypeValueString, out deploymentType))
                {
                    return deploymentType;
                }

                return DeploymentType.NoDeployment;
            }

            string GetAWSProfile()
            {
                var awsProfile = System.Environment.GetEnvironmentVariable("AWS_PROFILE");
                return string.IsNullOrWhiteSpace(awsProfile) ? "default" : awsProfile;
            }

            app.OnExecuteAsync(async cancellationToken =>
            {
                var deploymentType = GetDeploymentType();
                var awsProfile = GetAWSProfile();
                var serviceProvider = DependencyInjection.GetServiceProvider(awsProfile);

                switch (deploymentType)
                {
                    case DeploymentType.DockerRepositories:
                        await SynthesizeDockerRepositoriesStack(serviceProvider);
                        return;
                    case DeploymentType.PrivateCloud:
                        await SynthesizePrivateCloudFormationStacks(serviceProvider);
                        return;
                    case DeploymentType.ClientVpnBlacklist:
                        await DeployClientVpnBlacklist(serviceProvider);
                        return;

                    case DeploymentType.ClientVpnCertificates:
                        await DeployClientVpnClientCertificates(serviceProvider);
                        return;
                    case DeploymentType.Roles:
                        DeployPrivateCloudRoles();
                        return;
                }

                if (optionDownloadVpnConfig.HasValue())
                {
                    var clientName = optionDownloadVpnConfig.Value();
                    var outputDir = optionDownloadOutputDir.HasValue() ? optionDownloadOutputDir.Value() : Directory.GetCurrentDirectory();

                    var userManager = serviceProvider.GetService<IUserManager>();
                    var config = await userManager.GetVpnConfigForUser(clientName);
                    using StreamWriter writer = new StreamWriter(Path.Combine(outputDir, $"{clientName}.ovpn"));
                    writer.Write(config);
                }
            });

            app.Execute(args);
        }
    }
}
