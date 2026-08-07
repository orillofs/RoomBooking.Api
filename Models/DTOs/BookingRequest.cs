using System;
using System.ComponentModel.DataAnnotations;

namespace RoomBooking.Api.Models.DTOs;

public class BookingRequest
{
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "UserId must be a valid positive id.")]
    public int UserId { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "RoomId must be a valid positive id.")]
    public int RoomId { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }
}
