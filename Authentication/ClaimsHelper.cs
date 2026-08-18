using System.Security.Claims;
using RoomBooking.Api.Models.Entities;

namespace RoomBooking.Api.Authentication;

/// <summary>
/// Builds the claim set a signed-in user carries inside their JWT. The claims
/// mirror what the DevAuth handler produces (NameIdentifier, Name, Role) so both
/// schemes yield an identical principal for <see cref="ICurrentUser"/>.
/// </summary>
public static class ClaimsHelper
{
    /// <param name="user">Must have its <see cref="User.UserRole"/> loaded.</param>
    public static Claim[] CreateClaims(User user)
    {
        return
        [
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Email),
            new Claim(ClaimTypes.Role, user.UserRole.Name)
        ];
    }
}