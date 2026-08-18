namespace RoomBooking.Api.Authentication;

/// <summary>
/// Strongly-typed settings for the <c>Jwt</c> configuration section in
/// appsettings.json. Bound via the options pattern and injected as
/// <see cref="Microsoft.Extensions.Options.IOptions{T}"/>.
/// </summary>
public sealed class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Issuer { get; init; } = "RoomBooking.Api";
    public string Audience { get; init; } = "RoomBooking.Clients";

    /// <summary>HMAC-SHA256 signing key. Development-only value lives in appsettings.</summary>
    public string SecretKey { get; init; } = "";

    public int ExpirationMinutes { get; init; } = 60;
}