using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RoomBooking.Api.Controllers;
using RoomBooking.Api.Models.DTOs;
using RoomBooking.Api.Services.Auth;

namespace RoomBooking.Api.Tests;

/// <summary>
/// Verifies the token endpoints map auth-service outcomes to the RFC 7807 error
/// contract: 201 + token on signup, 409 on duplicate email, 200 + token on signin,
/// 401 on bad credentials. Service behavior is faked so no database is required.
/// </summary>
public class AuthControllerTests
{
    private static AuthController CreateController(IAuthService service) => new(service)
    {
        ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
    };

    private static SignUpRequest SignUpPayload() => new()
    {
        Email = "sam@fullscale.ph",
        Name = "Sam",
        Password = "password123"
    };

    [Fact]
    public async Task SignUp_ValidRequest_Returns201WithToken()
    {
        var controller = CreateController(new FakeAuthService());

        var result = await controller.SignUp(SignUpPayload());

        var objectResult = Assert.IsAssignableFrom<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status201Created, objectResult.StatusCode);
        var response = Assert.IsType<AuthResponse>(objectResult.Value);
        Assert.Equal("token-abc", response.Token);
        Assert.Equal("sam@fullscale.ph", response.User.Email);
        Assert.Equal("User", response.User.Role);
    }

    [Fact]
    public async Task SignUp_DuplicateEmail_Returns409Conflict()
    {
        var controller = CreateController(new FakeAuthService(throwDuplicate: true));

        var result = await controller.SignUp(SignUpPayload());

        var objectResult = Assert.IsAssignableFrom<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status409Conflict, objectResult.StatusCode);
        var problem = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal(409, problem.Status);
        Assert.Contains("already exists", problem.Detail);
    }

    [Fact]
    public async Task SignUp_NullPayload_Returns400()
    {
        var controller = CreateController(new FakeAuthService());

        var result = await controller.SignUp(null!);

        var objectResult = Assert.IsAssignableFrom<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, objectResult.StatusCode);
    }

    [Fact]
    public async Task SignIn_ValidCredentials_Returns200WithToken()
    {
        var controller = CreateController(new FakeAuthService(signInResult: AuthResponse()));

        var result = await controller.SignIn(new SignInRequest
        {
            Email = "sam@fullscale.ph",
            Password = "password123"
        });

        var objectResult = Assert.IsAssignableFrom<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, objectResult.StatusCode);
        var response = Assert.IsType<AuthResponse>(objectResult.Value);
        Assert.Equal("token-abc", response.Token);
    }

    [Fact]
    public async Task SignIn_InvalidCredentials_Returns401ProblemDetails()
    {
        var controller = CreateController(new FakeAuthService(signInResult: null));

        var result = await controller.SignIn(new SignInRequest
        {
            Email = "sam@fullscale.ph",
            Password = "wrong-password"
        });

        var objectResult = Assert.IsAssignableFrom<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status401Unauthorized, objectResult.StatusCode);
        var problem = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal(401, problem.Status);
        Assert.Equal("Invalid email or password.", problem.Detail);
    }

    private static AuthResponse AuthResponse() => new(
        "token-abc",
        new DateTime(2026, 8, 18, 12, 0, 0, DateTimeKind.Utc),
        new AuthUserInfo(1, "sam@fullscale.ph", "Sam", "User"));

    private sealed class FakeAuthService : IAuthService
    {
        private readonly AuthResponse? _signInResult;
        private readonly bool _throwDuplicate;

        public FakeAuthService(AuthResponse? signInResult = null, bool throwDuplicate = false)
        {
            _signInResult = signInResult;
            _throwDuplicate = throwDuplicate;
        }

        public Task<AuthResponse> SignUpAsync(SignUpRequest request, CancellationToken cancellationToken)
        {
            if (_throwDuplicate)
            {
                throw new DuplicateEmailException(request.Email);
            }

            return Task.FromResult(AuthResponse());
        }

        public Task<AuthResponse?> SignInAsync(SignInRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_signInResult);
        }
    }
}