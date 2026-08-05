using Microsoft.AspNetCore.Mvc;
using RoomBooking.Api.Models.DTOs;
using RoomBooking.Api.Services.Booking;

namespace RoomBooking.Api.Controllers;

[ApiController]
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
        var booking = await _bookingService.GetBookingByIdAsync(id);
        if (booking is null)
        {
            return NotFound();
        }

        return Ok(booking);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] BookingRequest booking)
    {
        var created = await _bookingService.CreateBookingAsync(booking);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] BookingRequest booking)
    {
        if (booking is null)
        {
            return BadRequest();
        }

        var updated = await _bookingService.UpdateBookingAsync(id, booking);
        if (!updated)
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _bookingService.DeleteBookingAsync(id);
        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}
