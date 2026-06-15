# Amazon Bedrock Mantle Experiments

Experiments calling Amazon Bedrock from .NET 10 through [Microsoft.Extensions.AI](https://learn.microsoft.com/dotnet/ai/), comparing two access paths and porting the Bedrock bearer-token generator to C#.

## What is the Bedrock Mantle endpoint?

`bedrock-mantle` is Amazon Bedrock's **OpenAI- and Anthropic-compatible** endpoint, powered by *Mantle*, a distributed inference engine for large-scale model serving. It exposes the OpenAI Responses and Chat Completions APIs (plus the Anthropic Messages API), so you can point an existing OpenAI/Anthropic SDK codebase at Bedrock by changing only the **base URL** and **API key** — no rewrite.

- **Base URL:** `https://bedrock-mantle.{region}.api.aws/v1`
- **Auth:** a Bedrock API key (bearer token). This repo generates one from AWS credentials via [BedrockTokenGenerator.cs](BedrockTokenGenerator.cs).
- **vs. `bedrock-runtime`:** the runtime endpoint is the native AWS SDK surface (used in [BedrockTests.cs](BedrockTests.cs) too). Mantle adds OpenAI-shaped APIs with extras like stateful conversations (`previous_response_id`) and is governed by separate quotas.

## What's inside

| File | Purpose |
| --- | --- |
| [BedrockTokenGenerator.cs](BedrockTokenGenerator.cs) | .NET port of the `@aws/bedrock-token-generator` npm package. Builds a SigV4-presigned URL for a dummy `CallWithBearerToken` request and encodes it into a `bedrock-api-key-…` bearer token. |
| [RenewTokenPolicy.cs](RenewTokenPolicy.cs) | `PipelinePolicy` that refreshes the bearer token every 30 minutes (double-checked locking) so long-lived clients keep working. |
| [BedrockTests.cs](BedrockTests.cs) | Two ways to reach an `IChatClient`: the native **Bedrock Runtime** SDK, and the OpenAI-compatible **Bedrock Mantle** endpoint authenticated with a generated bearer token. |
| [BedrockTokenGeneratorTests.cs](BedrockTokenGeneratorTests.cs) | Verifies the generated token byte-for-byte against the reference JS implementation (deterministic, offline). |

## Configuration

Credentials are read from `appsettings.json`, `appsettings.local.json`, or environment variables. `appsettings.local.json` is git-ignored — put your real secrets there:

```json
{
  "AWSBedrockAccessKeyId": "…",
  "AWSBedrockSecretAccessKey": "…",
  "AWSBedrockRegion": "eu-central-1",
  "BedrockMantleProjectId": "…"
}
```

## Running

```bash
dotnet test
```

The token-generator test runs offline; the two chat tests require valid AWS Bedrock credentials.
