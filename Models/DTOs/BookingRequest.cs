using System;
using System.ComponentModel.DataAnnotations;

namespace RoomBooking.Api.Models.DTOs;

public class BookingRequest
{
    // Ownership is never client-supplied: the authenticated caller's id is
    // assigned in the service layer (see BookingService.CreateBookingAsync).

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "RoomId must be a valid positive id.")]
    public int RoomId { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }
}
