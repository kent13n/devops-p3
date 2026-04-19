using System.IdentityModel.Tokens.Jwt;
using DataShare.Infrastructure.Services;
using AwesomeAssertions;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace DataShare.Tests.Unit.Services;

public class TokenServiceTests
{
    private static IConfiguration BuildConfig(string? secret = "super-secret-key-for-tests-minimum-32-characters",
                                              string issuer = "DataShare",
                                              string audience = "DataShare",
                                              string expirationHours = "24")
    {
        var values = new Dictionary<string, string?>
        {
            ["Jwt:Secret"] = secret,
            ["Jwt:Issuer"] = issuer,
            ["Jwt:Audience"] = audience,
            ["Jwt:ExpirationInHours"] = expirationHours
        };
        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    [Fact]
    public void GenerateToken_ReturnsValidJwtWithExpectedClaims()
    {
        var service = new TokenService(BuildConfig());
        var userId = Guid.NewGuid();
        const string email = "user@datashare.com";

        var (token, expiresAt) = service.GenerateToken(userId, email);

        token.Should().NotBeNullOrWhiteSpace();
        expiresAt.Should().BeAfter(DateTime.UtcNow);
        expiresAt.Should().BeBefore(DateTime.UtcNow.AddHours(25));

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == userId.ToString());
        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Email && c.Value == email);
        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Jti);
        jwt.Issuer.Should().Be("DataShare");
    }

    [Fact]
    public void GenerateToken_WithMissingSecret_Throws()
    {
        var service = new TokenService(BuildConfig(secret: null));

        var act = () => service.GenerateToken(Guid.NewGuid(), "user@datashare.com");

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*Jwt:Secret*");
    }

    [Fact]
    public void GenerateToken_DifferentUsers_ProduceDifferentJti()
    {
        var service = new TokenService(BuildConfig());

        var (token1, _) = service.GenerateToken(Guid.NewGuid(), "a@test.com");
        var (token2, _) = service.GenerateToken(Guid.NewGuid(), "b@test.com");

        token1.Should().NotBe(token2);
    }
}
