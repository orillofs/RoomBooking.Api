using System;

namespace RoomBooking.Api.Models.DTOs;

public class BookingResponse
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int RoomId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public uint Version { get; set; }
}
