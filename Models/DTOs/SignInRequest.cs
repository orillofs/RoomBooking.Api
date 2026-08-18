using System.ComponentModel.DataAnnotations;

namespace RoomBooking.Api.Models.DTOs;

public sealed class SignInRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = "";

    [Required]
    public string Password { get; set; } = "";
}