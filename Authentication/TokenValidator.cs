using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace RoomBooking.Api.Authentication;

/// <summary>
/// Validates a JWT against <see cref="JwtSettings"/> and returns its
/// <see cref="ClaimsPrincipal"/>, or null when the token is malformed, expired,
/// or signed with a different key. The JwtBearer scheme in Program.cs reuses the
/// same <see cref="TokenValidationParameters"/> via <see cref="CreateParameters"/>
/// so acceptance criteria are defined once.
/// </summary>
public class TokenValidator
{
    private readonly TokenValidationParameters _parameters;

    public TokenValidator(IOptions<JwtSettings> options)
    {
        _parameters = CreateParameters(options.Value);
    }

    public ClaimsPrincipal? Validate(string token)
    {
        try
        {
            return new JwtSecurityTokenHandler().ValidateToken(token, _parameters, out _);
        }
        catch (SecurityTokenException)
        {
            return null;
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    /// <summary>The single set of validation rules shared by the JwtBearer scheme.</summary>
    public static TokenValidationParameters CreateParameters(JwtSettings settings) => new()
    {
        ValidateIssuer = true,
        ValidIssuer = settings.Issuer,
        ValidateAudience = true,
        ValidAudience = settings.Audience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SecretKey)),
        ValidateLifetime = true,
        RequireExpirationTime = true,
        ClockSkew = TimeSpan.FromSeconds(30)
    };
}