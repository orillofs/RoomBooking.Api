using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using RoomBooking.Api.Authentication;

namespace RoomBooking.Api.Tests;

/// <summary>
/// Round-trip tests for token issuance: a token minted by <see cref="TokenGenerator"/>
/// must be accepted by <see cref="TokenValidator"/> back into the same identity,
/// and rejected when expired or signed with another key. No database required.
/// </summary>
public class TokenRoundTripTests
{
    private static readonly JwtSettings Settings = new()
    {
        Issuer = "TestIssuer",
        Audience = "TestAudience",
        SecretKey = "test-secret-key-that-is-at-least-32-bytes-long",
        ExpirationMinutes = 60
    };

    private static TokenOptions Options(DateTime expiresAt, params Claim[] claims)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Settings.SecretKey));
        return new TokenOptions
        {
            Claims = claims,
            Issuer = Settings.Issuer,
            Audience = Settings.Audience,
            NotBefore = DateTime.UtcNow.AddMinutes(-1),
            ExpiresAt = expiresAt,
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        };
    }

    [Fact]
    public void Validate_RoundTripsACurrentlyValidToken()
    {
        var token = new TokenGenerator().GenerateToken(Options(
            DateTime.UtcNow.AddMinutes(30),
            new Claim(ClaimTypes.NameIdentifier, "7"),
            new Claim(ClaimTypes.Name, "sam@fullscale.ph"),
            new Claim(ClaimTypes.Role, "User")));

        var principal = new TokenValidator(Options.Create(Settings)).Validate(token);

        Assert.NotNull(principal);
        Assert.Equal("7", principal!.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        Assert.Equal("sam@fullscale.ph", principal!.FindFirst(ClaimTypes.Name)!.Value);
        Assert.True(principal.IsInRole("User"));
    }

    [Fact]
    public void Validate_ExpiredToken_ReturnsNull()
    {
        var token = new TokenGenerator().GenerateToken(Options(
            DateTime.UtcNow.AddMinutes(-10),
            new Claim(ClaimTypes.NameIdentifier, "7")));

        var principal = new TokenValidator(Options.Create(Settings)).Validate(token);

        Assert.Null(principal);
    }

    [Fact]
    public void Validate_TokenSignedWithAnotherKey_ReturnsNull()
    {
        var other = new JwtSettings
        {
            Issuer = Settings.Issuer,
            Audience = Settings.Audience,
            SecretKey = "a-different-secret-key-that-is-also-long-enough",
            ExpirationMinutes = 60
        };

        var token = new TokenGenerator().GenerateToken(Options(
            DateTime.UtcNow.AddMinutes(30),
            new Claim(ClaimTypes.NameIdentifier, "7")));

        var principal = new TokenValidator(Options.Create(other)).Validate(token);

        Assert.Null(principal);
    }

    [Fact]
    public void Validate_GarbageString_ReturnsNull()
    {
        var principal = new TokenValidator(Options.Create(Settings)).Validate("not-a-jwt");

        Assert.Null(principal);
    }
}