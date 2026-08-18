using Microsoft.AspNetCore.Mvc;
using RoomBooking.Api.Middlewares;
using RoomBooking.Api.Models.DTOs;
using RoomBooking.Api.Services.Auth;

namespace RoomBooking.Api.Controllers;

/// <summary>
/// Token issuance endpoints. Sign-up and sign-in both return an access token a
/// client can present as <c>Authorization: Bearer &lt;token&gt;</c>.
/// </summary>
[ApiController]
[Route("api")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("signup")]
    public async Task<IActionResult> SignUp([FromBody] SignUpRequest request)
    {
        if (request is null)
        {
            return BadRequest(new { Message = "Sign-up payload is required." });
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        try
        {
            var response = await _authService.SignUpAsync(request, HttpContext.RequestAborted);
            return StatusCode(StatusCodes.Status201Created, response);
        }
        catch (DuplicateEmailException ex)
        {
            return Conflict(ErrorHandler.Conflict(ex.Message));
        }
    }

    [HttpPost("signin")]
    public async Task<IActionResult> SignIn([FromBody] SignInRequest request)
    {
        if (request is null)
        {
            return BadRequest(new { Message = "Sign-in payload is required." });
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var response = await _authService.SignInAsync(request, HttpContext.RequestAborted);
        if (response is null)
        {
            return Unauthorized(ErrorHandler.Unauthorized("Invalid email or password."));
        }

        return Ok(response);
    }
}