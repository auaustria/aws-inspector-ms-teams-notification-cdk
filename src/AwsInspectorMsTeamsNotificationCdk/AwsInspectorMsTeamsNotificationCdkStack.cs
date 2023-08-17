using Amazon.CDK;
using Amazon.CDK.AWS.Events.Targets;
using Amazon.CDK.AWS.Events;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.Lambda;
using Constructs;
using System.Collections.Generic;

namespace AwsInspectorMsTeamsNotificationCdk
{
    public class AwsInspectorMsTeamsNotificationCdkStack : Stack
    {
        internal AwsInspectorMsTeamsNotificationCdkStack(Construct scope, string id, IStackProps props = null) : base(scope, id, props)
        {
            var stackId = $"aws-inspector-ms-teams-notif-{props.Env.Account}-{props.Env.Region}";
            var lambdaExecutionRoleName = $"{stackId}-role";

            var lambdaExecutionRole = new Role(this, lambdaExecutionRoleName, new RoleProps
            {
                RoleName = lambdaExecutionRoleName,
                AssumedBy = new ServicePrincipal("lambda.amazonaws.com"),
                Description = $"Lambda Execution Role for AWS Inspector Notifications",
                InlinePolicies = new Dictionary<string, PolicyDocument>()
                {
                    { $"{stackId}-read-secret-policy", new PolicyDocument(new PolicyDocumentProps
                        {
                            Statements = new PolicyStatement[] { new PolicyStatement(
                                new PolicyStatementProps
                                {
                                    Resources = new[] { "*" },
                                    Actions = new[] { "secretsmanager:GetSecretValue" },
                                    Effect = Effect.ALLOW
                                })
                            }
                        })
                    }
                },
                ManagedPolicies = new IManagedPolicy[]
                {
                    ManagedPolicy.FromAwsManagedPolicyName("service-role/AWSLambdaVPCAccessExecutionRole"),
                    ManagedPolicy.FromAwsManagedPolicyName("service-role/AWSLambdaBasicExecutionRole")
                }
            });

            var lambdaCodeFile = Code.FromAsset("src/AwsInspectorMsTeamsNotificationCdk/asset");
            var functionName = $"{stackId}-func";

            var function = new Function(this, functionName, new FunctionProps
            {
                FunctionName = functionName,
                Runtime = Runtime.PYTHON_3_10,
                Handler = "lambda.handler",
                Code = lambdaCodeFile,
                Role = lambdaExecutionRole,
                LogRetention = Amazon.CDK.AWS.Logs.RetentionDays.ONE_DAY
            });

            var eventBus = Amazon.CDK.AWS.Events.EventBus.FromEventBusName(this, "default-event-bus", "default");

            var rule = new Rule(this, $"{stackId}-rule",
                new RuleProps
                {
                    RuleName = $"{stackId}-rule",
                    EventBus = eventBus,
                    Description = "Sends AWS Inspector notification to MS Teams",
                    Enabled = true,
                    EventPattern = new EventPattern
                    {
                        Account = new[] { props.Env.Account },
                        Source = new[] { "aws.inspector2" },
                        DetailType = new[] { "Inspector2 Finding" }
                    },
                });

            rule.AddTarget(new LambdaFunction(function));
        }
    }
}
