namespace RoomBooking.Api.Auth;

/// <summary>Identity of the caller for the current request, derived from the authenticated principal.</summary>
public sealed record CurrentUser(int Id, bool IsAdmin);