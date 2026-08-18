namespace RoomBooking.Api.Authentication;

/// <summary>Provides the current request's caller identity to the service layer.</summary>
public interface ICurrentUser
{
    CurrentUser Value { get; }
}