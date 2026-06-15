using System.ClientModel;
using System.ClientModel.Primitives;
using Amazon.BedrockRuntime;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using OpenAI;

namespace BedrockMantleExperiments;

[TestClass]
public sealed partial class BedrockTests
{
    [TestMethod]
    public async Task BedrockRuntimeTest()
    {
        var response = await BedrockRuntime().GetResponseAsync("Why is the sky blue?");
        Assert.IsTrue(!string.IsNullOrWhiteSpace(response.Text));
    }

    static IChatClient BedrockRuntime()
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.local.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var runtime = new AmazonBedrockRuntimeClient(
            awsAccessKeyId: configuration["AWSBedrockAccessKeyId"],
            awsSecretAccessKey: configuration["AWSBedrockSecretAccessKey"],
            region: Amazon.RegionEndpoint.GetBySystemName(configuration["AWSBedrockRegion"]));

        return runtime
            .AsIChatClient(defaultModelId: "eu.anthropic.claude-sonnet-4-5-20250929-v1:0")
            .AsBuilder()
            .Build();
    }

    [TestMethod]
    public async Task BedrockMantleTest()
    {
        var response = await BedrockMantle().GetResponseAsync("Why is the sky blue?");
        Assert.IsTrue(!string.IsNullOrWhiteSpace(response.Text));
    }

    static IChatClient BedrockMantle()
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.local.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var provideToken = BedrockTokenGenerator.GetTokenProvider(
            accessKeyId: configuration["AWSBedrockAccessKeyId"]!,
            secretAccessKey: configuration["AWSBedrockSecretAccessKey"]!,
            region: configuration["AWSBedrockRegion"]!);

        var credential = new ApiKeyCredential(provideToken());
        var options = new OpenAIClientOptions
        {
            Endpoint = new Uri($"https://bedrock-mantle.{configuration["AWSBedrockRegion"]}.api.aws/v1"),
            ProjectId = configuration["BedrockMantleProjectId"],
        };
        options.AddPolicy(new RenewTokenPolicy(credential, provideToken), PipelinePosition.PerCall);

        return new OpenAIClient(credential, options)
            .GetChatClient(model: "minimax.minimax-m2.5")
            .AsIChatClient()
            .AsBuilder()
            .Build();
    }
}
