using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RoomBooking.Api.Authentication;
using RoomBooking.Api.Data;
using RoomBooking.Api.Models.DTOs;
using RoomBooking.Api.Models.Entities;

namespace RoomBooking.Api.Services.Auth;

/// <summary>
/// Sign-up/sign-in flow. Owns the user data access directly (no repository yet —
/// auth is the only consumer of the Users table today); password hashing uses
/// PasswordHasher&lt;User&gt; and token issuance uses <see cref="TokenProvider"/> so
/// every token in the system is minted the same way.
/// </summary>
public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly TokenProvider _tokenProvider;
    private readonly IPasswordHasher<User> _passwordHasher;

    public AuthService(
        AppDbContext context,
        TokenProvider tokenProvider,
        IPasswordHasher<User> passwordHasher)
    {
        _context = context;
        _tokenProvider = tokenProvider;
        _passwordHasher = passwordHasher;
    }

    public async Task<AuthResponse> SignUpAsync(SignUpRequest request, CancellationToken cancellationToken)
    {
        if (await _context.Users.AnyAsync(u => u.Email == request.Email, cancellationToken))
        {
            throw new DuplicateEmailException(request.Email);
        }

        var user = new User
        {
            Email = request.Email.Trim().ToLowerInvariant(),
            Name = request.Name.Trim(),
            RoleId = Roles.User,
            PasswordHash = _passwordHasher.HashPassword(null!, request.Password)
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);

        // UserRole is needed for the role claim; load it now that the row exists.
        await _context.Entry(user).Reference(u => u.UserRole).LoadAsync(cancellationToken);

        return CreateAuthResponse(user);
    }

    public async Task<AuthResponse?> SignInAsync(SignInRequest request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .Include(u => u.UserRole)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

        // Same response for an unknown email and a wrong password: never reveal
        // which one failed.
        if (user is null)
        {
            return null;
        }

        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (result == PasswordVerificationResult.Failed)
        {
            return null;
        }

        return CreateAuthResponse(user);
    }

    private AuthResponse CreateAuthResponse(User user)
    {
        var issued = _tokenProvider.CreateAccessToken(user);
        return new AuthResponse(
            issued.Token,
            issued.ExpiresAt,
            new AuthUserInfo(user.Id, user.Email, user.Name, user.UserRole.Name));
    }
}