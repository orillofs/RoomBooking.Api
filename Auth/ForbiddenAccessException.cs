namespace RoomBooking.Api.Auth;

/// <summary>
/// Thrown by the service layer when an authenticated caller tries to read or
/// mutate a resource they do not own and are not an admin of. Mapped to a 403
/// problem-details response by the controller.
/// </summary>
public sealed class ForbiddenAccessException(string message) : Exception(message);