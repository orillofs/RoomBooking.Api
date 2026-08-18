using RoomBooking.Api.Models.DTOs;

namespace RoomBooking.Api.Services.Auth;

public interface IAuthService
{
    /// <summary>Creates the account and returns an access token for the new user.</summary>
    Task<AuthResponse> SignUpAsync(SignUpRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Verifies credentials and returns an access token, or null when the email or
    /// password is wrong.
    /// </summary>
    Task<AuthResponse?> SignInAsync(SignInRequest request, CancellationToken cancellationToken);
}