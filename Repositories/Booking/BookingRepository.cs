using Microsoft.EntityFrameworkCore;
using RoomBooking.Api.Models.DTOs;
using RoomBooking.Api.Models.Entities;

namespace RoomBooking.Api.Repositories.Booking;

public class BookingRepository : IBookingRepository
{
    private readonly AppDbContext _context;

    public BookingRepository(AppDbContext context)
    {
        _context = context;
    }

    private static BookingResponse MapToResponse(Booking booking) => new()
    {
        Id = booking.Id,
        UserId = booking.UserId,
        RoomId = booking.RoomId,
        StartDate = booking.StartDate,
        EndDate = booking.EndDate
    };

    private static Booking MapToEntity(BookingRequest dto) => new()
    {
        UserId = dto.UserId,
        RoomId = dto.RoomId,
        StartDate = dto.StartDate,
        EndDate = dto.EndDate
    };

    public async Task<IEnumerable<BookingResponse>> GetAllAsync()
    {
        var bookings = await _context.Bookings
            .Include(b => b.User)
            .Include(b => b.Room)
            .AsNoTracking()
            .ToListAsync();

        return bookings.Select(MapToResponse);
    }

    public async Task<BookingResponse?> GetByIdAsync(int id)
    {
        var booking = await _context.Bookings
            .Include(b => b.User)
            .Include(b => b.Room)
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == id);

        return booking is null ? null : MapToResponse(booking);
    }

    public async Task<BookingResponse> AddAsync(BookingRequest bookingRequest)
    {
        var booking = MapToEntity(bookingRequest);
        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync();

        return MapToResponse(booking);
    }

    public async Task<bool> UpdateAsync(int id, BookingRequest bookingRequest)
    {
        var booking = await _context.Bookings.FindAsync(id);
        if (booking is null)
        {
            return false;
        }

        booking.UserId = bookingRequest.UserId;
        booking.RoomId = bookingRequest.RoomId;
        booking.StartDate = bookingRequest.StartDate;
        booking.EndDate = bookingRequest.EndDate;

        _context.Bookings.Update(booking);
        return true;
    }

    public async Task<bool> RemoveAsync(int id)
    {
        var booking = await _context.Bookings.FindAsync(id);
        if (booking is null)
        {
            return false;
        }

        _context.Bookings.Remove(booking);
        return true;
    }

    public Task<bool> ExistsAsync(int id)
    {
        return _context.Bookings.AnyAsync(b => b.Id == id);
    }

    public Task SaveChangesAsync()
    {
        return _context.SaveChangesAsync();
    }
}
