using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace RoomBooking.Api.Authentication;

public class CurrentUserService : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public CurrentUser Value
    {
        get
        {
            var principal = _httpContextAccessor.HttpContext?.User
                ?? throw new InvalidOperationException("No HttpContext was available for the request.");

            var idClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(idClaim, out var id))
            {
                throw new InvalidOperationException(
                    "The authenticated principal is missing a valid NameIdentifier claim.");
            }

            return new CurrentUser(id, principal.IsInRole("Admin"));
        }
    }
}