using Microsoft.EntityFrameworkCore;
using RoomBooking.Api.Models.Entities;

namespace RoomBooking.Api.Data;

public class AppDbContext : DbContext
{
    // PBKDF2 hashes (PasswordHasher<User>) for the two seeded accounts so they can
    // sign in through /signin. Generated once and pinned here so seeding is stable.
    private const string AdminSeedPasswordHash =
        "AQAAAAIAAYagAAAAEGtDOdHg1qiI7jD9VMy4Ubji6vXeFHl0DjVX9liQOSdseYZwy5iHPPnRFy2nFrTUfQ==";
    private const string UserSeedPasswordHash =
        "AQAAAAIAAYagAAAAECQ6uyby1jPI8UzYFmAaBADeHLNMQ4r+E8s43j7nyd2nMee7Y0b1q3jhh3ptxXRXkg==";

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Booking> Bookings => Set<Booking>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<Booking>()
            .Property(b => b.Version)
            .IsRowVersion();

        builder.Entity<Booking>()
            .HasOne(b => b.Room)
            .WithMany()
            .HasForeignKey(b => b.RoomId);

        builder.Entity<Booking>()
            .HasOne(b => b.User)
            .WithMany()
            .HasForeignKey(b => b.UserId);

        builder.Entity<User>()
            .HasOne(u => u.UserRole)
            .WithMany()
            .HasForeignKey(u => u.RoleId);

        builder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        builder.Entity<UserRole>()
            .HasData(
                new UserRole { Id = Roles.Admin, Name = Roles.AdminName },
                new UserRole { Id = Roles.User, Name = Roles.UserName }
            );

        builder.Entity<User>()
            .HasData(
                new User
                {
                    Id = 1,
                    Email = "admin@gmail.com",
                    Name = "Admin",
                    RoleId = Roles.Admin,
                    PasswordHash = AdminSeedPasswordHash
                },
                new User
                {
                    Id = 2,
                    Email = "user@gmail.com",
                    Name = "User",
                    RoleId = Roles.User,
                    PasswordHash = UserSeedPasswordHash
                }
            );

        builder.Entity<Room>()
            .HasData(
                new Room { Id = 1, Name = "Standard" },
                new Room { Id = 2, Name = "Deluxe" }
            );

        builder.Entity<Booking>()
            .HasData(
                new Booking
                {
                    Id = 1,
                    UserId = 2,
                    RoomId = 1,
                    StartDate = new DateTime(2026, 8, 10, 0, 0, 0, DateTimeKind.Utc),
                    EndDate = new DateTime(2026, 8, 15, 0, 0, 0, DateTimeKind.Utc)
                }
            );
    }
}