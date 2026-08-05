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

    public async Task<bool> UpdateBookingAsync(int id, BookingRequest booking)
    {
        if (!await _repository.ExistsAsync(id))
        {
            return false;
        }

        var updated = await _repository.UpdateAsync(id, booking);
        if (!updated)
        {
            return false;
        }

        await _repository.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteBookingAsync(int id)
    {
        var removed = await _repository.RemoveAsync(id);
        if (!removed)
        {
            return false;
        }

        await _repository.SaveChangesAsync();
        return true;
    }
}
