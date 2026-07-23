using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BeerApi.Services;
using Xunit;

namespace BeerApi.Tests.Services;

public class FacebookSignedRequestParserTests
{
    private const string AppSecret = "test-app-secret";

    private static string BuildSignedRequest(object payload, string appSecret = AppSecret)
    {
        var payloadJson = JsonSerializer.Serialize(payload);
        var encodedPayload = Base64UrlEncode(Encoding.UTF8.GetBytes(payloadJson));
        var signature = HMACSHA256.HashData(Encoding.UTF8.GetBytes(appSecret), Encoding.UTF8.GetBytes(encodedPayload));
        var encodedSignature = Base64UrlEncode(signature);
        return $"{encodedSignature}.{encodedPayload}";
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');

    [Fact]
    public void TryParse_ValidSignedRequest_ReturnsUserId()
    {
        var signedRequest = BuildSignedRequest(new { algorithm = "HMAC-SHA256", user_id = "fb-user-123", issued_at = 1690000000 });

        var succeeded = FacebookSignedRequestParser.TryParse(signedRequest, AppSecret, out var userId);

        Assert.True(succeeded);
        Assert.Equal("fb-user-123", userId);
    }

    [Fact]
    public void TryParse_WrongAppSecret_Fails()
    {
        var signedRequest = BuildSignedRequest(new { algorithm = "HMAC-SHA256", user_id = "fb-user-123" });

        var succeeded = FacebookSignedRequestParser.TryParse(signedRequest, "wrong-secret", out var userId);

        Assert.False(succeeded);
        Assert.Null(userId);
    }

    [Fact]
    public void TryParse_TamperedPayload_Fails()
    {
        var signedRequest = BuildSignedRequest(new { algorithm = "HMAC-SHA256", user_id = "fb-user-123" });
        var parts = signedRequest.Split('.');
        var tamperedPayload = Base64UrlEncode(Encoding.UTF8.GetBytes("{\"user_id\":\"attacker-controlled\"}"));
        var tampered = $"{parts[0]}.{tamperedPayload}";

        var succeeded = FacebookSignedRequestParser.TryParse(tampered, AppSecret, out var userId);

        Assert.False(succeeded);
        Assert.Null(userId);
    }

    [Fact]
    public void TryParse_UnexpectedAlgorithm_Fails()
    {
        var signedRequest = BuildSignedRequest(new { algorithm = "none", user_id = "fb-user-123" });

        var succeeded = FacebookSignedRequestParser.TryParse(signedRequest, AppSecret, out var userId);

        Assert.False(succeeded);
    }

    [Fact]
    public void TryParse_MalformedInput_Fails()
    {
        var succeeded = FacebookSignedRequestParser.TryParse("not-a-valid-signed-request", AppSecret, out var userId);

        Assert.False(succeeded);
        Assert.Null(userId);
    }

    [Fact]
    public void TryParse_MissingUserId_Fails()
    {
        var signedRequest = BuildSignedRequest(new { algorithm = "HMAC-SHA256" });

        var succeeded = FacebookSignedRequestParser.TryParse(signedRequest, AppSecret, out var userId);

        Assert.False(succeeded);
    }
}
