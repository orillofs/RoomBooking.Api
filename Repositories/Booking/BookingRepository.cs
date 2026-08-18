using Microsoft.EntityFrameworkCore;
using RoomBooking.Api.Data;
using RoomBooking.Api.Models.DTOs;
using BookingEntity = RoomBooking.Api.Models.Entities.Booking;

namespace RoomBooking.Api.Repositories.Booking;

public class BookingRepository : IBookingRepository
{
    private readonly AppDbContext _context;

    public BookingRepository(AppDbContext context)
    {
        _context = context;
    }

    private static BookingResponse MapToResponse(BookingEntity booking) => new()
    {
        Id = booking.Id,
        UserId = booking.UserId,
        RoomId = booking.RoomId,
        StartDate = booking.StartDate,
        EndDate = booking.EndDate,
        Version = booking.Version
    };

    private static BookingEntity MapToEntity(BookingRequest dto) => new()
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

    public async Task<bool> UpdateAsync(int id, BookingRequest bookingRequest, uint? expectedVersion = null)
    {
        var booking = await _context.Bookings.FindAsync(id);
        if (booking is null)
        {
            return false;
        }

        if (expectedVersion.HasValue)
        {
            _context.Entry(booking).Property("Version").OriginalValue = expectedVersion.Value;
        }

        booking.UserId = bookingRequest.UserId;
        booking.RoomId = bookingRequest.RoomId;
        booking.StartDate = bookingRequest.StartDate;
        booking.EndDate = bookingRequest.EndDate;

        _context.Bookings.Update(booking);
        return true;
    }

    public async Task<bool> RemoveAsync(int id, uint? expectedVersion = null)
    {
        var booking = await _context.Bookings.FindAsync(id);
        if (booking is null)
        {
            return false;
        }

        if (expectedVersion.HasValue)
        {
            _context.Entry(booking).Property("Version").OriginalValue = expectedVersion.Value;
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
