using System;
using System.Collections.Generic;
using Amazon.Auth.AccessControlPolicy;
using Amazon.CDK;
using Amazon.CDK.AWS.IAM;

namespace PrivateCloud.CDK.Constructs.Roles
{
    public class TeamCityRole : Construct
    {
        public TeamCityRole(Construct scope, string id) : base(scope, id)
        {
            var role = new Role(this, "TeamCity Agent Role", new RoleProps
            {
                RoleName = "TeamCityAgentRole",
                AssumedBy = new ServicePrincipal("ecs-tasks.amazonaws.com"),
            });

            var superUserPolicy = new PolicyStatement();
            superUserPolicy.AddAllResources();
            superUserPolicy.AddActions(
                "acm:Describe*",
                "acm:Get*",
                "acm:List*",
                "apigateway:*",
                "application-autoscaling:*",
                "autoscaling:*",
                "cloudformation:*",
                "cloudfront:*",
                "cloudwatch:*",
                "codedeploy:*",
                "cognito-idp:*",
                "config:*",
                "cognito-identity:*",
                "datapipeline:*",
                "dax:*",
                "dynamodb:*",
                "ebs:*",
                "ec2:*",
                "ecr:*",
                "ecs:*",
                "eks:*",
                "elasticache:*",
                "elasticloadbalancing:*",
                "elasticmapreduce:*",
                "es:*",
                "events:*",
                "execute-api:Invoke",
                "firehose:*",
                "glue:*",
                "iam:*",
                "kinesis:*",
                "kms:*",
                "lambda:*",
                "logs:*",
                "rds:*",
                "redshift:*",
                "route53:*",
                "s3:*",
                "sdb:*",
                "sns:*",
                "sqs:*",
                "ssm:*",
                "translate:*",
                "transfer:*",
                "sts:AssumeRole"
            );
            superUserPolicy.Effect = Effect.ALLOW;

            role.AddToPolicy(superUserPolicy);
        }
    }
}
