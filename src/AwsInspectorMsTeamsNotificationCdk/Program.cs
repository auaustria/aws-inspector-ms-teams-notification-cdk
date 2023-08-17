using Amazon.CDK;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AwsInspectorMsTeamsNotificationCdk
{
    sealed class Program
    {
        public static void Main(string[] args)
        {
            var app = new App();
            
            var account = app.Node.TryGetContext("account")?.ToString().ToLower();
            var region = app.Node.TryGetContext("region")?.ToString().ToLower();

            new AwsInspectorMsTeamsNotificationCdkStack(app, "AwsInspectorMsTeamsNotificationCdkStack", new StackProps
            {
                // If you don't specify 'env', this stack will be environment-agnostic.
                // Account/Region-dependent features and context lookups will not work,
                // but a single synthesized template can be deployed anywhere.

                // Uncomment the next block to specialize this stack for the AWS Account
                // and Region that are implied by the current CLI configuration.
                /*
                Env = new Amazon.CDK.Environment
                {
                    Account = System.Environment.GetEnvironmentVariable("CDK_DEFAULT_ACCOUNT"),
                    Region = System.Environment.GetEnvironmentVariable("CDK_DEFAULT_REGION"),
                }
                */

                Env = new Amazon.CDK.Environment
                {
                    Account = account,
                    Region = region,
                },

                StackName = $"aws-inspector-ms-teams-notif-stack",
                CrossRegionReferences = true,
                Description = "Application Stack for AWS Inspector to MS Teams notification built via CDK"

                // For more information, see https://docs.aws.amazon.com/cdk/latest/guide/environments.html
            });
            app.Synth();
        }
    }
}
