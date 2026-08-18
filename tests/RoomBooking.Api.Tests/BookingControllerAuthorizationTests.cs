using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RoomBooking.Api.Auth;
using RoomBooking.Api.Controllers;
using RoomBooking.Api.Models.DTOs;
using RoomBooking.Api.Services.Booking;

namespace RoomBooking.Api.Tests;

/// <summary>
/// Verifies the controller maps ownership violations to RFC 7807 403 problem-details
/// responses for every endpoint that can be denied. Service behavior is faked so no
/// database is required.
/// </summary>
public class BookingControllerAuthorizationTests
{
    private static BookingController CreateController(
        IBookingService service, bool setIfMatch = true)
    {
        var controller = new BookingController(service)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        if (setIfMatch)
        {
            controller.HttpContext.Request.Headers["If-Match"] = "\"1\"";
        }

        return controller;
    }

    private static ProblemDetails AssertForbidden(IActionResult result)
    {
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
        var problem = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal(403, problem.Status);
        Assert.Equal("Forbidden", problem.Title);
        return problem;
    }

    [Fact]
    public async Task GetById_WhenCallerForbidden_Returns403ProblemDetails()
    {
        var controller = CreateController(new ForbiddenService());

        var result = await controller.GetById(42);

        var problem = AssertForbidden(result);
        Assert.Equal("You can only access your own bookings.", problem.Detail);
    }

    [Fact]
    public async Task Update_WhenCallerForbidden_Returns403ProblemDetails()
    {
        var controller = CreateController(new ForbiddenService());

        var result = await controller.Update(42, SampleRequest());

        var problem = AssertForbidden(result);
        Assert.Equal("You can only modify your own bookings.", problem.Detail);
    }

    [Fact]
    public async Task Delete_WhenCallerForbidden_Returns403ProblemDetails()
    {
        var controller = CreateController(new ForbiddenService());

        var result = await controller.Delete(42);

        var problem = AssertForbidden(result);
        Assert.Equal("You can only cancel your own bookings.", problem.Detail);
    }

    [Fact]
    public async Task GetById_WhenAllowed_ReturnsOkWithETag()
    {
        var controller = CreateController(new SuccessService());

        var result = await controller.GetById(1);

        var ok = Assert.IsType<OkObjectResult>(result);
        var booking = Assert.IsType<BookingResponse>(ok.Value);
        Assert.Equal(1, booking.Id);
        Assert.Equal("\"1\"", controller.HttpContext.Response.Headers.ETag);
    }

    [Fact]
    public async Task Update_WhenAllowed_ReturnsNoContent()
    {
        var controller = CreateController(new SuccessService());

        var result = await controller.Update(1, SampleRequest());

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_WhenAllowed_ReturnsNoContent()
    {
        var controller = CreateController(new SuccessService());

        var result = await controller.Delete(1);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task GetAll_DelegatesToService_ReturnsOk()
    {
        var controller = CreateController(new SuccessService());

        var result = await controller.GetAll();

        var ok = Assert.IsType<OkObjectResult>(result);
        var bookings = Assert.IsAssignableFrom<IEnumerable<BookingResponse>>(ok.Value);
        Assert.Single(bookings);
    }

    private static BookingRequest SampleRequest() => new()
    {
        RoomId = 2,
        StartDate = new DateTime(2028, 4, 1, 9, 0, 0, DateTimeKind.Utc),
        EndDate = new DateTime(2028, 4, 1, 10, 0, 0, DateTimeKind.Utc)
    };

    private sealed class SuccessService : IBookingService
    {
        private static readonly BookingResponse Owned = new()
        {
            Id = 1,
            UserId = 2,
            RoomId = 1,
            StartDate = new DateTime(2026, 8, 10, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2026, 8, 15, 0, 0, 0, DateTimeKind.Utc),
            Version = 1
        };

        public Task<IEnumerable<BookingResponse>> GetAllBookingsAsync() =>
            Task.FromResult<IEnumerable<BookingResponse>>([Owned]);

        public Task<BookingResponse?> GetBookingByIdAsync(int id) =>
            Task.FromResult<BookingResponse?>(Owned);

        public Task<BookingResponse> CreateBookingAsync(BookingRequest booking) =>
            throw new NotSupportedException();

        public Task<bool> UpdateBookingAsync(int id, BookingRequest booking, uint? expectedVersion = null) =>
            Task.FromResult(true);

        public Task<bool> DeleteBookingAsync(int id, uint? expectedVersion = null) =>
            Task.FromResult(true);
    }

    private sealed class ForbiddenService : IBookingService
    {
        public Task<IEnumerable<BookingResponse>> GetAllBookingsAsync() =>
            throw new NotSupportedException();

        public Task<BookingResponse?> GetBookingByIdAsync(int id) =>
            throw new ForbiddenAccessException("You can only access your own bookings.");

        public Task<BookingResponse> CreateBookingAsync(BookingRequest booking) =>
            throw new NotSupportedException();

        public Task<bool> UpdateBookingAsync(int id, BookingRequest booking, uint? expectedVersion = null) =>
            throw new ForbiddenAccessException("You can only modify your own bookings.");

        public Task<bool> DeleteBookingAsync(int id, uint? expectedVersion = null) =>
            throw new ForbiddenAccessException("You can only cancel your own bookings.");
    }
}