namespace RoomBooking.Api.Services.Auth;

/// <summary>
/// Thrown by the auth service when a sign-up targets an email address that already
/// has an account. Mapped to a 409 problem-details response by the controller.
/// </summary>
public sealed class DuplicateEmailException(string email)
    : Exception($"An account with email `{email}` already exists")
{
    public string Email { get; } = email;
}