using System;

namespace RoomBooking.Api.Models.Entities;

public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = "";
    public string Name { get; set; } = "";
    public int RoleId { get; set; }
    public UserRole UserRole { get; set; } = null!;

}
