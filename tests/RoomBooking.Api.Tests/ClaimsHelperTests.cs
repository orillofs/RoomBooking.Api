using System.Security.Claims;
using RoomBooking.Api.Authentication;
using RoomBooking.Api.Models.Entities;

namespace RoomBooking.Api.Tests;

public class ClaimsHelperTests
{
    [Fact]
    public void CreateClaims_ExposesIdentityOfTheUser()
    {
        var user = new User
        {
            Id = 7,
            Email = "sam@fullscale.ph",
            Name = "Sam",
            RoleId = Roles.User,
            UserRole = new UserRole { Id = Roles.User, Name = Roles.UserName }
        };

        var claims = ClaimsHelper.CreateClaims(user);

        Assert.Equal("7", claims.Single(c => c.Type == ClaimTypes.NameIdentifier).Value);
        Assert.Equal("sam@fullscale.ph", claims.Single(c => c.Type == ClaimTypes.Name).Value);
        Assert.Equal("User", claims.Single(c => c.Type == ClaimTypes.Role).Value);
    }

    [Fact]
    public void CreateClaims_AdminRole_IsExposedAsRoleClaim()
    {
        var user = new User
        {
            Id = 1,
            Email = "admin@gmail.com",
            Name = "Admin",
            RoleId = Roles.Admin,
            UserRole = new UserRole { Id = Roles.Admin, Name = Roles.AdminName }
        };

        var claims = ClaimsHelper.CreateClaims(user);

        Assert.Equal("Admin", claims.Single(c => c.Type == ClaimTypes.Role).Value);
    }
}