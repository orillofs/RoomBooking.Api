using RoomBooking.Api.Models.DTOs;

namespace RoomBooking.Api.Services.Booking;

public interface IBookingService
{
    Task<IEnumerable<BookingResponse>> GetAllBookingsAsync();
    Task<BookingResponse?> GetBookingByIdAsync(int id);
    Task<BookingResponse> CreateBookingAsync(BookingRequest booking);
    Task<bool> UpdateBookingAsync(int id, BookingRequest booking);
    Task<bool> DeleteBookingAsync(int id);
}
