using RoomBooking.Api.Models.DTOs;
using RoomBooking.Api.Repositories.Booking;

namespace RoomBooking.Api.Services.Booking;

public class BookingService : IBookingService
{
    private readonly IBookingRepository _repository;

    public BookingService(IBookingRepository repository)
    {
        _repository = repository;
    }

    public Task<IEnumerable<BookingResponse>> GetAllBookingsAsync()
    {
        return _repository.GetAllAsync();
    }

    public Task<BookingResponse?> GetBookingByIdAsync(int id)
    {
        return _repository.GetByIdAsync(id);
    }

    public Task<BookingResponse> CreateBookingAsync(BookingRequest booking)
    {
        return _repository.AddAsync(booking);
    }

    public async Task<bool> UpdateBookingAsync(int id, BookingRequest booking, uint? expectedVersion = null)
    {
        if (!await _repository.ExistsAsync(id))
        {
            return false;
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
        var removed = await _repository.RemoveAsync(id, expectedVersion);
        if (!removed)
        {
            return false;
        }

        await _repository.SaveChangesAsync();
        return true;
    }
}
