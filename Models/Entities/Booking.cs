using System;

namespace RoomBooking.Api.Models.Entities;

public class Booking
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int RoomId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public uint Version { get; private set; }
    public Room Room { get; set; } = null!;
    public User User { get; set; } = null!;
}
