using System;
using Microsoft.EntityFrameworkCore;
using RoomBooking.Api.Models.Entities;

namespace RoomBooking.Api.Data;

public class AppDbContext : DbContext
{
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


        builder.Entity<UserRole>()
            .HasData(
            new UserRole()
            {
                Id = 1,
                Name = "Admin"
            },
            new UserRole()
            {
                Id = 2,
                Name = "User"
            }
        );

        builder.Entity<User>()
            .HasData(
                new User()
                {
                    Id = 1,
                    Email = "admin@gmail.com",
                    Name = "Admin",
                    RoleId = 1
                },
                new User()
                {
                    Id = 2,
                    Email = "user@gmail.com",
                    Name = "User",
                    RoleId = 2
                }
            );

        builder.Entity<Room>()
            .HasData(
                new Room()
                {
                    Id = 1,
                    Name = "Standard"
                },
                new Room()
                {
                    Id = 2,
                    Name = "Deluxe"
                }
            );
        builder.Entity<Booking>()
            .HasData(
                new Booking()
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
