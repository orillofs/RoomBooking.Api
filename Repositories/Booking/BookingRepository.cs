using Microsoft.EntityFrameworkCore;
using RoomBooking.Api.Authentication;
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

    private static BookingEntity MapToEntity(BookingRequest dto, int callerId) => new()
    {
        UserId = callerId,
        RoomId = dto.RoomId,
        StartDate = dto.StartDate,
        EndDate = dto.EndDate
    };

    public async Task<IEnumerable<BookingResponse>> GetAllAsync(CurrentUser caller)
    {
        var query = _context.Bookings
            .Include(b => b.User)
            .Include(b => b.Room)
            .AsNoTracking();

        if (!caller.IsAdmin)
        {
            query = query.Where(b => b.UserId == caller.Id);
        }

        var bookings = await query.ToListAsync();

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

    public async Task<BookingResponse> AddAsync(BookingRequest bookingRequest, int callerId)
    {
        var booking = MapToEntity(bookingRequest, callerId);
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

        // The owner is never changed by an update; ownership comes from the
        // authenticated caller and was already checked by the service.
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

    public async Task<int?> GetOwnerIdAsync(int id)
    {
        var booking = await _context.Bookings
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == id);

        return booking?.UserId;
    }

    public Task SaveChangesAsync()
    {
        return _context.SaveChangesAsync();
    }
}
