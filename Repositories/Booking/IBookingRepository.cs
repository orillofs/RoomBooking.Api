using RoomBooking.Api.Models.DTOs;

namespace RoomBooking.Api.Repositories.Booking;

public interface IBookingRepository
{
    Task<IEnumerable<BookingResponse>> GetAllAsync();
    Task<BookingResponse?> GetByIdAsync(int id);
    Task<BookingResponse> AddAsync(BookingRequest booking);
    Task<bool> UpdateAsync(int id, BookingRequest booking);
    Task<bool> RemoveAsync(int id);
    Task<bool> ExistsAsync(int id);
    Task SaveChangesAsync();
}
