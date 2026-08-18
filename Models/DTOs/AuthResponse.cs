namespace RoomBooking.Api.Models.DTOs;

/// <summary>Identity of the signed-in user, as returned by /signup and /signin.</summary>
public sealed record AuthUserInfo(int Id, string Email, string Name, string Role);

/// <summary>Body returned by /signup and /signin: an access token plus who it belongs to.</summary>
public sealed record AuthResponse(string Token, DateTime ExpiresAt, AuthUserInfo User);