using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace A2aProtocol;

/// <summary>
/// Handles push-notification JWT signing for an A2A server.
/// Generates an RSA key pair on construction, exposes the public key as a
/// JWKs document, and signs outgoing bearer tokens.
/// Ported from <c>common/utils/push_notification_auth.py</c>.
/// </summary>
public sealed class PushNotificationSenderAuth : IDisposable
{
    private static readonly string[] SupportedAlgorithms = ["RS256"];

    private readonly RSA _rsa;

    public PushNotificationSenderAuth()
    {
        _rsa = RSA.Create(2048);
    }

    // ── Public JWKs endpoint ──────────────────────────────────────────────────

    /// <summary>
    /// Returns the RSA public key as a minimal JWKs document (RFC 7517) that
    /// remote receivers can fetch and cache.
    /// </summary>
    public string GetJwksJson()
    {
        var parameters = _rsa.ExportParameters(includePrivateParameters: false);

        var jwk = new
        {
            keys = new[]
            {
                new
                {
                    kty = "RSA",
                    use = "sig",
                    alg = "RS256",
                    n   = Base64UrlEncode(parameters.Modulus!),
                    e   = Base64UrlEncode(parameters.Exponent!),
                }
            }
        };

        return JsonSerializer.Serialize(jwk);
    }

    // ── Token generation ──────────────────────────────────────────────────────

    /// <summary>
    /// Creates a signed RS256 JWT with a 1-hour expiry for the given
    /// <paramref name="audienceUrl"/>.
    /// </summary>
    public string GenerateJwt(string audienceUrl)
    {
        var now = DateTimeOffset.UtcNow;

        var headerJson  = JsonSerializer.Serialize(new { alg = "RS256", typ = "JWT" });
        var payloadJson = JsonSerializer.Serialize(new
        {
            iss = "a2a-server",
            aud = audienceUrl,
            iat = now.ToUnixTimeSeconds(),
            exp = now.AddHours(1).ToUnixTimeSeconds(),
        });

        var headerB64  = Base64UrlEncode(Encoding.UTF8.GetBytes(headerJson));
        var payloadB64 = Base64UrlEncode(Encoding.UTF8.GetBytes(payloadJson));
        var signingInput = $"{headerB64}.{payloadB64}";

        var signature = _rsa.SignData(
            Encoding.UTF8.GetBytes(signingInput),
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        return $"{signingInput}.{Base64UrlEncode(signature)}";
    }

    // ── Receiver verification helper ──────────────────────────────────────────

    /// <summary>
    /// Fetches the JWKs document from <paramref name="jwksUrl"/> and verifies
    /// the <paramref name="token"/> (RS256, audience = <paramref name="expectedAudience"/>).
    /// Returns <c>true</c> on success.
    /// </summary>
    public static async Task<bool> VerifyTokenAsync(
        string token,
        string jwksUrl,
        string expectedAudience,
        HttpClient? http = null)
    {
        try
        {
            http ??= new HttpClient();
            var jwksJson = await http.GetStringAsync(jwksUrl);
            var jwks     = JsonSerializer.Deserialize<JsonElement>(jwksJson);

            if (!jwks.TryGetProperty("keys", out var keys) || keys.GetArrayLength() == 0)
                return false;

            var key  = keys[0];
            var n    = Base64UrlDecode(key.GetProperty("n").GetString()!);
            var e    = Base64UrlDecode(key.GetProperty("e").GetString()!);

            using var rsa = RSA.Create();
            rsa.ImportParameters(new RSAParameters { Modulus = n, Exponent = e });

            // Manual JWT verification (header.payload.signature)
            var parts = token.Split('.');
            if (parts.Length != 3) return false;

            var signingInput = $"{parts[0]}.{parts[1]}";
            var signature    = Base64UrlDecode(parts[2]);

            if (!rsa.VerifyData(
                    Encoding.UTF8.GetBytes(signingInput),
                    signature,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1))
                return false;

            // Validate claims
            var payloadJson = Encoding.UTF8.GetString(Base64UrlDecode(parts[1]));
            var payload     = JsonSerializer.Deserialize<JsonElement>(payloadJson);

            var exp = payload.TryGetProperty("exp", out var expEl) ? expEl.GetInt64() : 0L;
            if (DateTimeOffset.FromUnixTimeSeconds(exp) < DateTimeOffset.UtcNow)
                return false; // expired

            var aud = payload.TryGetProperty("aud", out var audEl) ? audEl.GetString() : null;
            return aud == expectedAudience;
        }
        catch
        {
            return false;
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string Base64UrlEncode(byte[] data)
        => Convert.ToBase64String(data)
                  .TrimEnd('=')
                  .Replace('+', '-')
                  .Replace('/', '_');

    private static byte[] Base64UrlDecode(string s)
    {
        s = s.Replace('-', '+').Replace('_', '/');
        switch (s.Length % 4)
        {
            case 2: s += "=="; break;
            case 3: s += "=";  break;
        }
        return Convert.FromBase64String(s);
    }

    public void Dispose() => _rsa.Dispose();
}

