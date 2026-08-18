using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoomBooking.Api.Controllers;
using RoomBooking.Api.Models.DTOs;
using RoomBooking.Api.Services.Booking;

namespace RoomBooking.Api.Tests;

public class BookingControllerConcurrencyTests
{
    private static BookingController CreateController(IBookingService service, string? ifMatch = null)
    {
        var controller = new BookingController(service)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        if (ifMatch is not null)
        {
            controller.HttpContext.Request.Headers["If-Match"] = ifMatch;
        }

        return controller;
    }

    private static BookingRequest SampleRequest() => new()
    {
        UserId = 2,
        RoomId = 2,
        StartDate = new DateTime(2027, 4, 1, 9, 0, 0, DateTimeKind.Utc),
        EndDate = new DateTime(2027, 4, 1, 10, 0, 0, DateTimeKind.Utc)
    };

    [Fact]
    public async Task Update_WhenBookingChangesAfterRead_ReturnsConflictProblemDetails()
    {
        var controller = CreateController(new ConcurrencyConflictService(), "\"1\"");

        var result = await controller.Update(42, SampleRequest());

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        var problem = Assert.IsType<ProblemDetails>(conflict.Value);
        Assert.Equal(StatusCodes.Status409Conflict, problem.Status);
        Assert.Equal("Conflict", problem.Title);
        Assert.Equal(
            "This booking was changed or removed by another request. Refresh the data and try again.",
            problem.Detail);
    }

    [Fact]
    public async Task Update_WhenIfMatchMissing_Returns412PreconditionFailed()
    {
        var controller = CreateController(new ConcurrencyConflictService(), ifMatch: null);

        var result = await controller.Update(42, SampleRequest());

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status412PreconditionFailed, objectResult.StatusCode);
        var problem = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal("Precondition required", problem.Title);
    }

    [Fact]
    public async Task Delete_WhenBookingChangesAfterRead_ReturnsConflictProblemDetails()
    {
        var controller = CreateController(new ConcurrencyConflictDeleteService(), "\"1\"");

        var result = await controller.Delete(42);

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        var problem = Assert.IsType<ProblemDetails>(conflict.Value);
        Assert.Equal(StatusCodes.Status409Conflict, problem.Status);
        Assert.Equal(
            "This booking was changed or removed by another request. Refresh the data and try again.",
            problem.Detail);
    }

    private sealed class ConcurrencyConflictService : IBookingService
    {
        public Task<IEnumerable<BookingResponse>> GetAllBookingsAsync() =>
            throw new NotSupportedException();

        public Task<BookingResponse?> GetBookingByIdAsync(int id) =>
            throw new NotSupportedException();

        public Task<BookingResponse> CreateBookingAsync(BookingRequest booking) =>
            throw new NotSupportedException();

        public Task<bool> UpdateBookingAsync(int id, BookingRequest booking, uint? expectedVersion = null) =>
            throw new DbUpdateConcurrencyException();

        public Task<bool> DeleteBookingAsync(int id, uint? expectedVersion = null) =>
            throw new NotSupportedException();
    }

    private sealed class ConcurrencyConflictDeleteService : IBookingService
    {
        public Task<IEnumerable<BookingResponse>> GetAllBookingsAsync() =>
            throw new NotSupportedException();

        public Task<BookingResponse?> GetBookingByIdAsync(int id) =>
            throw new NotSupportedException();

        public Task<BookingResponse> CreateBookingAsync(BookingRequest booking) =>
            throw new NotSupportedException();

        public Task<bool> UpdateBookingAsync(int id, BookingRequest booking, uint? expectedVersion = null) =>
            throw new NotSupportedException();

        public Task<bool> DeleteBookingAsync(int id, uint? expectedVersion = null) =>
            throw new DbUpdateConcurrencyException();
    }
}
