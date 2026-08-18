using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace RoomBooking.Api.Authentication;

/// <summary>
/// Everything required to mint one access token: the claims, timestamps, and
/// signing key. <see cref="TokenProvider"/> builds one of these per token from
/// the user and <see cref="JwtSettings"/>; <see cref="TokenGenerator"/> consumes
/// it. Keeping the input a plain value object makes generation pure and testable.
/// </summary>
public sealed class TokenOptions
{
    public required Claim[] Claims { get; init; }
    public required string Issuer { get; init; }
    public required string Audience { get; init; }
    public required DateTime NotBefore { get; init; }
    public required DateTime ExpiresAt { get; init; }
    public required SigningCredentials SigningCredentials { get; init; }
}