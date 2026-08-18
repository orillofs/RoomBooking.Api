using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;
using Npgsql;
using RoomBooking.Api.Authentication;
using RoomBooking.Api.Middlewares;
using RoomBooking.Api.Models.DTOs;
using RoomBooking.Api.Services.Booking;

namespace RoomBooking.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class BookingController : ControllerBase
{
    private readonly IBookingService _bookingService;

    public BookingController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var bookings = await _bookingService.GetAllBookingsAsync();
        return Ok(bookings);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var booking = await _bookingService.GetBookingByIdAsync(id);
            if (booking is null)
            {
                return NotFound(ErrorHandler.NotFound(id, "Booking"));
            }

            AddETag(booking.Version);
            return Ok(booking);
        }
        catch (ForbiddenAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, ErrorHandler.Forbidden(ex.Message));
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] BookingRequest booking)
    {
        if (booking is null)
        {
            return BadRequest(new { Message = "Booking request payload is required." });
        }

        ValidateDates(booking);

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        try
        {
            var created = await _bookingService.CreateBookingAsync(booking);
            AddETag(created.Version);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (DbUpdateException exception) when (IsBookingOverlapConflict(exception))
        {
            return Conflict(ErrorHandler.RoomUnavailable());
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] BookingRequest booking)
    {
        if (booking is null)
        {
            return BadRequest(new { Message = "Booking update payload is required." });
        }

        var expectedVersion = ParseIfMatch();
        if (expectedVersion is null)
        {
            return StatusCode(StatusCodes.Status412PreconditionFailed, ErrorHandler.PreconditionRequired());
        }

        ValidateDates(booking);

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        try
        {
            var updated = await _bookingService.UpdateBookingAsync(id, booking, expectedVersion);
            if (!updated)
            {
                return NotFound(ErrorHandler.NotFound(id, "Booking"));
            }

            return NoContent();
        }
        catch (ForbiddenAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, ErrorHandler.Forbidden(ex.Message));
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(ErrorHandler.BookingChanged());
        }
        catch (DbUpdateException exception) when (IsBookingOverlapConflict(exception))
        {
            return Conflict(ErrorHandler.RoomUnavailable());
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var expectedVersion = ParseIfMatch();

        try
        {
            var deleted = await _bookingService.DeleteBookingAsync(id, expectedVersion);
            if (!deleted)
            {
                return NotFound(ErrorHandler.NotFound(id, "Booking"));
            }

            return NoContent();
        }
        catch (ForbiddenAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, ErrorHandler.Forbidden(ex.Message));
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(ErrorHandler.BookingChanged());
        }
    }

    private void ValidateDates(BookingRequest booking)
    {
        if (booking.StartDate >= booking.EndDate)
        {
            ModelState.AddModelError(
                nameof(BookingRequest.EndDate),
                "EndDate must be after StartDate.");
        }
    }

    private uint? ParseIfMatch()
    {
        if (!Request.Headers.TryGetValue(HeaderNames.IfMatch, out var values) || values.Count == 0)
        {
            return null;
        }

        var raw = values.ToString().Trim().Trim('"');
        return uint.TryParse(raw, out var version) ? version : null;
    }

    private void AddETag(uint version)
    {
        Response.Headers.ETag = $"\"{version}\"";
    }

    private static bool IsBookingOverlapConflict(DbUpdateException exception) =>
        exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.ExclusionViolation,
            ConstraintName: "EX_Bookings_RoomId_DateRange"
        };
}
