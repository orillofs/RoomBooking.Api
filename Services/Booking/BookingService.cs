using RoomBooking.Api.Authentication;
using RoomBooking.Api.Models.DTOs;
using RoomBooking.Api.Repositories.Booking;

namespace RoomBooking.Api.Services.Booking;

public class BookingService : IBookingService
{
    private readonly IBookingRepository _repository;
    private readonly ICurrentUser _currentUser;

    public BookingService(IBookingRepository repository, ICurrentUser currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public Task<IEnumerable<BookingResponse>> GetAllBookingsAsync()
    {
        return _repository.GetAllAsync(_currentUser.Value);
    }

    public async Task<BookingResponse?> GetBookingByIdAsync(int id)
    {
        var booking = await _repository.GetByIdAsync(id);
        if (booking is null)
        {
            return null;
        }

        var caller = _currentUser.Value;
        if (booking.UserId != caller.Id && !caller.IsAdmin)
        {
            throw new ForbiddenAccessException("You can only access your own bookings.");
        }

        return booking;
    }

    public Task<BookingResponse> CreateBookingAsync(BookingRequest booking)
    {
        return _repository.AddAsync(booking, _currentUser.Value.Id);
    }

    public async Task<bool> UpdateBookingAsync(int id, BookingRequest booking, uint? expectedVersion = null)
    {
        var ownerId = await _repository.GetOwnerIdAsync(id);
        if (ownerId is null)
        {
            return false;
        }

        var caller = _currentUser.Value;
        if (ownerId != caller.Id && !caller.IsAdmin)
        {
            throw new ForbiddenAccessException("You can only modify your own bookings.");
        }

        var updated = await _repository.UpdateAsync(id, booking, expectedVersion);
        if (!updated)
        {
            return false;
        }

        await _repository.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteBookingAsync(int id, uint? expectedVersion = null)
    {
        var ownerId = await _repository.GetOwnerIdAsync(id);
        if (ownerId is null)
        {
            return false;
        }

        var caller = _currentUser.Value;
        if (ownerId != caller.Id && !caller.IsAdmin)
        {
            throw new ForbiddenAccessException("You can only cancel your own bookings.");
        }

        var removed = await _repository.RemoveAsync(id, expectedVersion);
        if (!removed)
        {
            return false;
        }

        await _repository.SaveChangesAsync();
        return true;
    }
}