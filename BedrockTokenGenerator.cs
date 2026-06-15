using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace BedrockMantleExperiments;

/// <summary>
/// .NET port of the <c>@aws/bedrock-token-generator</c> npm package
/// (<c>getToken</c> / <c>getTokenProvider</c> / <c>createToken</c>).
///
/// A token is a SigV4-presigned URL for a dummy <c>CallWithBearerToken</c> request against
/// <c>bedrock.amazonaws.com</c>, with the scheme stripped, <c>&amp;Version=1</c> appended,
/// Base64-encoded, and prefixed with <c>bedrock-api-key-</c>.
/// </summary>
public static class BedrockTokenGenerator
{
    const int MaxExpiresInSeconds = 43200; // 12 hours, also the default
    const string Service = "bedrock";
    const string Host = "bedrock.amazonaws.com";
    const string Algorithm = "AWS4-HMAC-SHA256";
    const string Action = "CallWithBearerToken";
    const string AuthPrefix = "bedrock-api-key-";
    const string TokenVersion = "&Version=1";

    // SHA-256 of an empty payload. @smithy/signature-v4 uses this (not "UNSIGNED-PAYLOAD")
    // because the request has no body.
    const string EmptyPayloadHash = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";

    /// <summary>
    /// Equivalent of <c>getTokenProvider(...)</c>: returns a function that produces a fresh
    /// token (new date + signature) on each invocation. Credentials are captured up front,
    /// matching the JS provider which only caches the resolved credential/region.
    /// </summary>
    public static Func<string> GetTokenProvider(
        string accessKeyId,
        string secretAccessKey,
        string region,
        string? sessionToken = null,
        int expiresInSeconds = MaxExpiresInSeconds)
    {
        ValidateTokenExpiryInput(expiresInSeconds);
        return () => GetToken(accessKeyId, secretAccessKey, region, sessionToken, expiresInSeconds);
    }

    /// <summary>
    /// Equivalent of the stateless <c>getToken(...)</c>: generates a single bearer token.
    /// </summary>
    /// <param name="signingDate">Override the signing instant (for deterministic tests). Defaults to now (UTC).</param>
    public static string GetToken(
        string accessKeyId,
        string secretAccessKey,
        string region,
        string? sessionToken = null,
        int expiresInSeconds = MaxExpiresInSeconds,
        DateTimeOffset? signingDate = null)
    {
        ValidateTokenExpiryInput(expiresInSeconds);

        var now = (signingDate ?? DateTimeOffset.UtcNow).ToUniversalTime();
        var amzDate = now.ToString("yyyyMMddTHHmmssZ", CultureInfo.InvariantCulture);
        var dateStamp = now.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        var scope = $"{dateStamp}/{region}/{Service}/aws4_request";

        // Query params that participate in the signature, sorted by key (ordinal/UTF-16 order,
        // matching JS Array.prototype.sort()).
        var query = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["Action"] = Action,
            ["X-Amz-Algorithm"] = Algorithm,
            ["X-Amz-Credential"] = $"{accessKeyId}/{scope}",
            ["X-Amz-Date"] = amzDate,
            ["X-Amz-Expires"] = expiresInSeconds.ToString(CultureInfo.InvariantCulture),
            ["X-Amz-SignedHeaders"] = "host",
        };
        if (!string.IsNullOrEmpty(sessionToken))
            query["X-Amz-Security-Token"] = sessionToken;

        // 1) Canonical request.
        var canonicalRequest = string.Join("\n",
            "POST",
            "/",
            CanonicalQuery(query),
            $"host:{Host}\n", // canonical headers block (own trailing newline) ...
            "host",           // ... then the signed-headers line (Join inserts the blank line between)
            EmptyPayloadHash);

        // 2) String to sign.
        var stringToSign = string.Join("\n",
            Algorithm,
            amzDate,
            scope,
            ToHex(Sha256(canonicalRequest)));

        // 3) Derive the signing key and sign.
        var signingKey = GetSigningKey(secretAccessKey, dateStamp, region);
        query["X-Amz-Signature"] = ToHex(HmacSha256(signingKey, stringToSign));

        // 4) formatUrl() minus the scheme, + version, Base64, prefix.
        var presignedUrl = $"{Host}/?{CanonicalQuery(query)}{TokenVersion}";
        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(presignedUrl));
        return $"{AuthPrefix}{encoded}";
    }

    static void ValidateTokenExpiryInput(int expiresInSeconds)
    {
        if (expiresInSeconds <= 0 || expiresInSeconds > MaxExpiresInSeconds)
            throw new ArgumentOutOfRangeException(nameof(expiresInSeconds),
                $"ExpiresInSeconds must be in the range (0, {MaxExpiresInSeconds}] seconds.");
    }

    static string CanonicalQuery(SortedDictionary<string, string> query) =>
        string.Join("&", query.Select(kv =>
            $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));

    static byte[] GetSigningKey(string secretKey, string dateStamp, string region)
    {
        var kDate = HmacSha256(Encoding.UTF8.GetBytes($"AWS4{secretKey}"), dateStamp);
        var kRegion = HmacSha256(kDate, region);
        var kService = HmacSha256(kRegion, Service);
        return HmacSha256(kService, "aws4_request");
    }

    static byte[] Sha256(string data) => SHA256.HashData(Encoding.UTF8.GetBytes(data));
    static byte[] HmacSha256(byte[] key, string data) => HMACSHA256.HashData(key, Encoding.UTF8.GetBytes(data));
    static string ToHex(byte[] bytes) => Convert.ToHexStringLower(bytes);
}
