namespace BedrockMantleExperiments;

[TestClass]
public sealed partial class BedrockTokenGeneratorTests
{
    [TestMethod]
    public void BedrockGeneratedTokenMatchesReferenceImplementation()
    {
        // Reference token produced by @aws/bedrock-token-generator (getToken) for the exact
        // same inputs and signing instant. Guards the SigV4 port against regressions offline.
        const string expected = "bedrock-api-key-YmVkcm9jay5hbWF6b25hd3MuY29tLz9BY3Rpb249Q2FsbFdpdGhCZWFyZXJUb2tlbiZYLUFtei1BbGdvcml0aG09QVdTNC1ITUFDLVNIQTI1NiZYLUFtei1DcmVkZW50aWFsPUFLSUFJT1NGT0ROTjdFWEFNUExFJTJGMjAyNjA2MTUlMkZldS1jZW50cmFsLTElMkZiZWRyb2NrJTJGYXdzNF9yZXF1ZXN0JlgtQW16LURhdGU9MjAyNjA2MTVUMTIwMDAwWiZYLUFtei1FeHBpcmVzPTcyMDAmWC1BbXotU2VjdXJpdHktVG9rZW49RkFLRSUyRlNFU1NJT04lMkJUT0tFTiUzRCUzRCZYLUFtei1TaWduYXR1cmU9MjFhNGQ3MzZjYjVlNjJhYTAzMmFiZjc2YjlmNGQxNTZiNGNlZmE2YTc0MTRiZTJmYWM5OWFlNDgyMmE0ODIxOSZYLUFtei1TaWduZWRIZWFkZXJzPWhvc3QmVmVyc2lvbj0x";

        var token = BedrockTokenGenerator.GetToken(
            accessKeyId: "AKIAIOSFODNN7EXAMPLE",
            secretAccessKey: "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY",
            region: "eu-central-1",
            sessionToken: "FAKE/SESSION+TOKEN==",
            expiresInSeconds: 7200,
            signingDate: new DateTimeOffset(2026, 6, 15, 12, 0, 0, TimeSpan.Zero));

        Assert.AreEqual(expected, token);
    }
}
