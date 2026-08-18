using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using RoomBooking.Api.Models.Entities;

namespace RoomBooking.Api.Authentication;

/// <summary>Result of issuing a token: the JWT string and when it expires.</summary>
public sealed record IssuedToken(string Token, DateTime ExpiresAt);

/// <summary>
/// High-level token issuance. Assembles the claims (via <see cref="ClaimsHelper"/>),
/// derives <see cref="TokenOptions"/> from <see cref="JwtSettings"/>, and delegates
/// the JWT itself to <see cref="TokenGenerator"/>. Both /signin and /signup route
/// through this class so every token in the system is minted the same way.
/// </summary>
public class TokenProvider
{
    private readonly JwtSettings _settings;
    private readonly TokenGenerator _generator;

    public TokenProvider(IOptions<JwtSettings> options, TokenGenerator generator)
    {
        _settings = options.Value;
        _generator = generator;
    }

    /// <param name="user">Must have its <see cref="User.UserRole"/> loaded.</param>
    public IssuedToken CreateAccessToken(User user)
    {
        ValidateSettings();
        var expiresAt = DateTime.UtcNow.AddMinutes(_settings.ExpirationMinutes);

        var options = new TokenOptions
        {
            Claims = ClaimsHelper.CreateClaims(user),
            Issuer = _settings.Issuer,
            Audience = _settings.Audience,
            NotBefore = DateTime.UtcNow,
            ExpiresAt = expiresAt,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SecretKey)),
                SecurityAlgorithms.HmacSha256)
        };

        return new IssuedToken(_generator.GenerateToken(options), expiresAt);
    }

    private void ValidateSettings()
    {
        if (string.IsNullOrWhiteSpace(_settings.SecretKey))
        {
            throw new InvalidOperationException(
                "Jwt:SecretKey is not configured. Add it to appsettings or user secrets before minting tokens.");
        }

        if (Encoding.UTF8.GetByteCount(_settings.SecretKey) < 32)
        {
            throw new InvalidOperationException(
                "Jwt:SecretKey must be at least 32 bytes for HMAC-SHA256 signing.");
        }
    }
}