using System.IdentityModel.Tokens.Jwt;

namespace RoomBooking.Api.Authentication;

/// <summary>
/// Pure JWT generator: turns <see cref="TokenOptions"/> into a signed JWT string.
/// It holds no configuration and no services, so a single instance is safe to
/// share for the whole application.
/// </summary>
public class TokenGenerator
{
    private static readonly JwtSecurityTokenHandler Handler =
        new() { SetDefaultTimesOnTokenCreation = false };

    public string GenerateToken(TokenOptions options)
    {
        var jwt = new JwtSecurityToken(
            issuer: options.Issuer,
            audience: options.Audience,
            claims: options.Claims,
            notBefore: options.NotBefore,
            expires: options.ExpiresAt,
            signingCredentials: options.SigningCredentials);

        return Handler.WriteToken(jwt);
    }
}