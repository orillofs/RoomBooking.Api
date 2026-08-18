using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RoomBooking.Api.Data;

namespace RoomBooking.Api.Auth;

/// <summary>
/// Development-only authentication scheme. Maps a bearer token to a seeded user
/// via the <c>DevAuth:Tokens</c> configuration section and produces a
/// ClaimsPrincipal carrying the user's id, email, and role. Replaces real
/// authentication during training; not for production use.
/// </summary>
public class DevAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly IConfiguration _configuration;

    public DevAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IConfiguration configuration)
        : base(options, logger, encoder)
    {
        _configuration = configuration;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("Authorization", out var values) || values.Count == 0)
        {
            return AuthenticateResult.NoResult();
        }

        var header = values.ToString();
        const string prefix = "Bearer ";
        if (!header.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return AuthenticateResult.NoResult();
        }

        var token = header[prefix.Length..].Trim();
        if (!int.TryParse(_configuration[$"DevAuth:Tokens:{token}"], out var userId))
        {
            return AuthenticateResult.NoResult();
        }

        // Authentication handlers are registered transient; scoped services are
        // resolved from RequestServices inside the request, not the constructor.
        var dbContext = Context.RequestServices.GetRequiredService<AppDbContext>();
        var user = await dbContext.Users
            .Include(u => u.UserRole)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, Request.HttpContext.RequestAborted);

        if (user is null)
        {
            return AuthenticateResult.NoResult();
        }

        var identity = new ClaimsIdentity(
            new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Email),
                new Claim(ClaimTypes.Role, user.UserRole.Name)
            },
            Scheme.Name);

        var principal = new ClaimsPrincipal(identity);
        return AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name));
    }
}