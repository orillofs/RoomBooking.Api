using System.ComponentModel.DataAnnotations;

namespace RoomBooking.Api.Models.DTOs;

public sealed class SignUpRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = "";

    [Required, MaxLength(100)]
    public string Name { get; set; } = "";

    [Required, MinLength(8)]
    public string Password { get; set; } = "";
}