using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace BeerApi.Services;

// Verifies and decodes Facebook's data-deletion callback contract: a POST body of
// "{base64url signature}.{base64url JSON payload}", signed HMAC-SHA256 over the raw
// (still-encoded) payload segment using the app secret.
// https://developers.facebook.com/docs/facebook-login/features-reference/deletion-callback
public static class FacebookSignedRequestParser
{
    public static bool TryParse(string signedRequest, string appSecret, out string? userId)
    {
        userId = null;

        var parts = signedRequest.Split('.');
        if (parts.Length != 2)
        {
            return false;
        }

        var (encodedSignature, encodedPayload) = (parts[0], parts[1]);

        byte[] signature;
        string payloadJson;
        try
        {
            signature = Base64UrlDecode(encodedSignature);
            payloadJson = Encoding.UTF8.GetString(Base64UrlDecode(encodedPayload));
        }
        catch (FormatException)
        {
            return false;
        }

        var expectedSignature = HMACSHA256.HashData(Encoding.UTF8.GetBytes(appSecret), Encoding.UTF8.GetBytes(encodedPayload));
        if (!CryptographicOperations.FixedTimeEquals(signature, expectedSignature))
        {
            return false;
        }

        using var payload = JsonDocument.Parse(payloadJson);
        var root = payload.RootElement;

        if (root.TryGetProperty("algorithm", out var algorithm) &&
            !string.Equals(algorithm.GetString(), "HMAC-SHA256", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!root.TryGetProperty("user_id", out var userIdElement))
        {
            return false;
        }

        userId = userIdElement.GetString();
        return !string.IsNullOrWhiteSpace(userId);
    }

    private static byte[] Base64UrlDecode(string input)
    {
        var padded = input.Replace('-', '+').Replace('_', '/');
        padded += (padded.Length % 4) switch
        {
            2 => "==",
            3 => "=",
            _ => "",
        };
        return Convert.FromBase64String(padded);
    }
}
