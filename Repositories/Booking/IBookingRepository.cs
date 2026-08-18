using RoomBooking.Api.Auth;
using RoomBooking.Api.Models.DTOs;

namespace RoomBooking.Api.Repositories.Booking;

public interface IBookingRepository
{
    Task<IEnumerable<BookingResponse>> GetAllAsync(CurrentUser caller);
    Task<BookingResponse?> GetByIdAsync(int id);
    Task<BookingResponse> AddAsync(BookingRequest booking, int callerId);
    Task<bool> UpdateAsync(int id, BookingRequest booking, uint? expectedVersion = null);
    Task<bool> RemoveAsync(int id, uint? expectedVersion = null);
    Task<int?> GetOwnerIdAsync(int id);
    Task SaveChangesAsync();
}
