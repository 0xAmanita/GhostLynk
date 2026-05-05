using backend_deob.Services;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace backend_deob.Tests;

public class ObfuscationServiceTests
{
    private readonly IObfuscationService _obfuscationService;

    public ObfuscationServiceTests()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Obfuscation:CaesarShift", "13" },
                { "Obfuscation:XorKey", "GhostLynkSecretKey2024" }
            })
            .Build();

        _obfuscationService = new ObfuscationService(configuration);
    }

    [Fact]
    public void Obfuscate_ShouldReturnBase64String()
    {
        var originalUrl = "https://example.com";
        var result = _obfuscationService.Obfuscate(originalUrl);
        
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.True(IsBase64String(result));
    }

    [Fact]
    public void Deobfuscate_ShouldReturnOriginalUrl()
    {
        var originalUrl = "https://example.com";
        var obfuscated = _obfuscationService.Obfuscate(originalUrl);
        var deobfuscated = _obfuscationService.Deobfuscate(obfuscated);
        
        Assert.Equal(originalUrl, deobfuscated);
    }

    [Theory]
    [InlineData("https://example.com")]
    [InlineData("https://github.com/user/repo")]
    [InlineData("https://www.google.com/search?q=test")]
    [InlineData("https://api.example.com/v1/users/123")]
    public void RoundTrip_ShouldPreserveOriginalUrl(string originalUrl)
    {
        var obfuscated = _obfuscationService.Obfuscate(originalUrl);
        var deobfuscated = _obfuscationService.Deobfuscate(obfuscated);
        
        Assert.Equal(originalUrl, deobfuscated);
    }

    [Fact]
    public void Obfuscate_SameInput_ShouldProduceSameOutput()
    {
        var originalUrl = "https://example.com";
        var result1 = _obfuscationService.Obfuscate(originalUrl);
        var result2 = _obfuscationService.Obfuscate(originalUrl);
        
        Assert.Equal(result1, result2);
    }

    [Fact]
    public void Obfuscate_DifferentInputs_ShouldProduceDifferentOutputs()
    {
        var url1 = "https://example.com";
        var url2 = "https://example.org";
        
        var result1 = _obfuscationService.Obfuscate(url1);
        var result2 = _obfuscationService.Obfuscate(url2);
        
        Assert.NotEqual(result1, result2);
    }

    [Fact]
    public void Obfuscate_EmptyString_ShouldReturnBase64String()
    {
        var result = _obfuscationService.Obfuscate("");
        
        Assert.NotNull(result);
        Assert.True(IsBase64String(result));
    }

    [Fact]
    public void RoundTrip_EmptyString_ShouldPreserveEmptyString()
    {
        var originalUrl = "";
        var obfuscated = _obfuscationService.Obfuscate(originalUrl);
        var deobfuscated = _obfuscationService.Deobfuscate(obfuscated);
        
        Assert.Equal(originalUrl, deobfuscated);
    }

    [Fact]
    public void RoundTrip_SpecialCharacters_ShouldPreserveOriginal()
    {
        var originalUrl = "https://example.com/path?param=value&special=!@#$%^&*()";
        var obfuscated = _obfuscationService.Obfuscate(originalUrl);
        var deobfuscated = _obfuscationService.Deobfuscate(obfuscated);
        
        Assert.Equal(originalUrl, deobfuscated);
    }

    [Fact]
    public void RoundTrip_UnicodeCharacters_ShouldPreserveOriginal()
    {
        var originalUrl = "https://example.com/测试/тест/🔒";
        var obfuscated = _obfuscationService.Obfuscate(originalUrl);
        var deobfuscated = _obfuscationService.Deobfuscate(obfuscated);
        
        Assert.Equal(originalUrl, deobfuscated);
    }

    [Fact]
    public void RoundTrip_VeryLongUrl_ShouldPreserveOriginal()
    {
        var originalUrl = "https://example.com/" + new string('a', 1000);
        var obfuscated = _obfuscationService.Obfuscate(originalUrl);
        var deobfuscated = _obfuscationService.Deobfuscate(obfuscated);
        
        Assert.Equal(originalUrl, deobfuscated);
    }

    private bool IsBase64String(string value)
    {
        if (string.IsNullOrEmpty(value))
            return false;

        try
        {
            Convert.FromBase64String(value);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
