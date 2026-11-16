namespace Codexo.Core.Tests.Security;

using Codexo.Core.Security;
using Xunit;

public class SecretRedactorTests
{
    [Fact]
    public void Redact_MasksGoogleApiKeys()
    {
        var input = "const key = 'AIzaSyD-1234567890abcdefghijklmnopqrst'; fetch();";
        var result = SecretRedactor.Redact(input);

        Assert.NotNull(result);
        Assert.DoesNotContain("AIzaSyD-1234567890abcdefghijklmnopqrst", result);
        Assert.Contains("[REDACTED_SECRET]", result);
    }

    [Fact]
    public void Redact_MasksOpenAiKeysAndBearerTokens()
    {
        var input = "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.e30.signature\napi_key = 'sk-abcdef1234567890abcdef1234567890'";
        var result = SecretRedactor.Redact(input);

        Assert.NotNull(result);
        Assert.DoesNotContain("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9", result);
        Assert.DoesNotContain("sk-abcdef1234567890abcdef1234567890", result);
        Assert.Contains("[REDACTED_SECRET]", result);
    }

    [Fact]
    public void Redact_MasksPrivateKeyBlocks()
    {
        var input = "-----BEGIN RSA PRIVATE KEY-----\nMIIEowIBAAKCAQEA0Y...\n-----END RSA PRIVATE KEY-----";
        var result = SecretRedactor.Redact(input);

        Assert.NotNull(result);
        Assert.DoesNotContain("MIIEowIBAAKCAQEA0Y", result);
        Assert.Contains("[REDACTED_SECRET]", result);
    }

    [Fact]
    public void Redact_LeavesNormalCodeUnchanged()
    {
        var input = "def two_sum(nums, target):\n    lookup = {}\n    for i, num in enumerate(nums):\n        if target - num in lookup:\n            return [lookup[target - num], i]\n        lookup[num] = i";
        var result = SecretRedactor.Redact(input);

        Assert.Equal(input, result);
    }
}
