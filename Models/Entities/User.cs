namespace RoomBooking.Api.Models.Entities;

public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = "";
    public string Name { get; set; } = "";
    public int RoleId { get; set; }
    public UserRole UserRole { get; set; } = null!;

    /// <summary>PBKDF2 hash produced by PasswordHasher&lt;User&gt; at signup/signin.</summary>
    public string PasswordHash { get; set; } = "";
}