using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;
using RoomBooking.Api.Data;
using RoomBooking.Api.Models.Entities;

namespace RoomBooking.Api.Tests;

[Collection("PostgreSql database")]
public class BookingConcurrencyTests : IAsyncLifetime
{
    private const int TestUserId = 2;
    private const int TestRoomId = 2;
    private readonly string _connectionString;
    private readonly DbContextOptions<AppDbContext> _options;
    private readonly List<int> _bookingIds = [];

    public BookingConcurrencyTests()
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets<BookingConcurrencyTests>()
            .Build();

        _connectionString = configuration.GetConnectionString("TestDatabase")
            ?? throw new InvalidOperationException(
                "ConnectionStrings:TestDatabase must be configured with dotnet user-secrets.");

        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_connectionString)
            .Options;
    }

    public async Task InitializeAsync()
    {
        await using var context = new AppDbContext(_options);
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        if (_bookingIds.Count == 0)
        {
            return;
        }

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(
            "DELETE FROM \"Bookings\" WHERE \"Id\" = ANY (@bookingIds);",
            connection);
        command.Parameters.AddWithValue("bookingIds", _bookingIds.ToArray());
        await command.ExecuteNonQueryAsync();
    }

    [Fact]
    public async Task SaveChangesAsync_WhenAnotherContextUpdatesSameBooking_ThrowsConcurrencyException()
    {
        var bookingId = await CreateBookingAsync(
            new DateTime(2027, 1, 1, 9, 0, 0, DateTimeKind.Utc),
            new DateTime(2027, 1, 1, 10, 0, 0, DateTimeKind.Utc));

        await using var firstContext = new AppDbContext(_options);
        await using var secondContext = new AppDbContext(_options);

        var firstBooking = await firstContext.Bookings.SingleAsync(b => b.Id == bookingId);
        var secondBooking = await secondContext.Bookings.SingleAsync(b => b.Id == bookingId);

        firstBooking.EndDate = firstBooking.EndDate.AddHours(1);
        secondBooking.EndDate = secondBooking.EndDate.AddHours(2);

        await firstContext.SaveChangesAsync();

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
            () => secondContext.SaveChangesAsync());
    }

    [Fact]
    public async Task SaveChangesAsync_WhenNewBookingsOverlap_ThrowsExpectedExclusionViolation()
    {
        var start = new DateTime(2027, 2, 1, 9, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2027, 2, 1, 10, 0, 0, DateTimeKind.Utc);

        await using var firstContext = new AppDbContext(_options);
        await using var secondContext = new AppDbContext(_options);

        firstContext.Bookings.Add(CreateBooking(start, end));
        secondContext.Bookings.Add(CreateBooking(start.AddMinutes(30), end.AddMinutes(30)));

        await firstContext.SaveChangesAsync();
        _bookingIds.Add(firstContext.Bookings.Local.Single().Id);

        var exception = await Assert.ThrowsAsync<DbUpdateException>(
            () => secondContext.SaveChangesAsync());

        var postgresException = Assert.IsType<PostgresException>(exception.InnerException);
        Assert.Equal(PostgresErrorCodes.ExclusionViolation, postgresException.SqlState);
        Assert.Equal("EX_Bookings_RoomId_DateRange", postgresException.ConstraintName);
    }

    [Fact]
    public async Task SaveChangesAsync_WhenNewBookingsTouchAtBoundary_AllowsBothBookings()
    {
        var start = new DateTime(2027, 3, 1, 9, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2027, 3, 1, 10, 0, 0, DateTimeKind.Utc);

        await using var firstContext = new AppDbContext(_options);
        await using var secondContext = new AppDbContext(_options);

        var firstBooking = CreateBooking(start, end);
        var secondBooking = CreateBooking(end, end.AddHours(1));
        firstContext.Bookings.Add(firstBooking);
        secondContext.Bookings.Add(secondBooking);

        await firstContext.SaveChangesAsync();
        await secondContext.SaveChangesAsync();

        _bookingIds.Add(firstBooking.Id);
        _bookingIds.Add(secondBooking.Id);
    }

    private async Task<int> CreateBookingAsync(DateTime start, DateTime end)
    {
        await using var context = new AppDbContext(_options);
        var booking = CreateBooking(start, end);
        context.Bookings.Add(booking);
        await context.SaveChangesAsync();
        _bookingIds.Add(booking.Id);
        return booking.Id;
    }

    private static Booking CreateBooking(DateTime start, DateTime end) => new()
    {
        UserId = TestUserId,
        RoomId = TestRoomId,
        StartDate = start,
        EndDate = end
    };
}

[CollectionDefinition("PostgreSql database", DisableParallelization = true)]
public class PostgreSqlDatabaseCollection;

