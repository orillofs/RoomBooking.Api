using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using RoomBooking.Api.Data;
using RoomBooking.Api.Models.DTOs;

namespace RoomBooking.Api.Tests;

[Collection("PostgreSql database")]
public class BookingEndpointConcurrencyTests : IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly string _connectionString;
    private readonly List<int> _bookingIds = [];

    public BookingEndpointConcurrencyTests()
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets<BookingEndpointConcurrencyTests>()
            .Build();

        _connectionString = configuration.GetConnectionString("TestDatabase")
            ?? throw new InvalidOperationException(
                "ConnectionStrings:TestDatabase must be configured with dotnet user-secrets.");

        _factory = new TestAppFactory(_connectionString);
        _client = _factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        if (_bookingIds.Count > 0)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(
                "DELETE FROM \"Bookings\" WHERE \"Id\" = ANY (@bookingIds);",
                connection);
            command.Parameters.AddWithValue("bookingIds", _bookingIds.ToArray());
            await command.ExecuteNonQueryAsync();
        }

        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task Put_WithStaleETag_Returns409Conflict()
    {
        // Step 1: Create a booking and capture its ETag
        var (created, originalETag) = await CreateBookingAsync(
            new DateTime(2027, 5, 1, 9, 0, 0, DateTimeKind.Utc),
            new DateTime(2027, 5, 1, 10, 0, 0, DateTimeKind.Utc));

        // Step 2: Update the booking with the correct ETag (succeeds, increments xmin)
        var firstUpdate = MakePutRequest(created.Id,
            new DateTime(2027, 5, 1, 9, 0, 0, DateTimeKind.Utc),
            new DateTime(2027, 5, 1, 11, 0, 0, DateTimeKind.Utc),
            originalETag);

        var firstUpdateResponse = await _client.SendAsync(firstUpdate);
        Assert.Equal(HttpStatusCode.NoContent, firstUpdateResponse.StatusCode);

        // Step 3: Try to update again with the original (stale) ETag — should fail
        var staleUpdate = MakePutRequest(created.Id,
            new DateTime(2027, 5, 1, 9, 0, 0, DateTimeKind.Utc),
            new DateTime(2027, 5, 1, 12, 0, 0, DateTimeKind.Utc),
            originalETag);

        var staleResponse = await _client.SendAsync(staleUpdate);
        Assert.Equal(HttpStatusCode.Conflict, staleResponse.StatusCode);

        var problem = await staleResponse.Content.ReadFromJsonAsync<ProblemDetailsJson>();
        Assert.NotNull(problem);
        Assert.Equal(409, problem.Status);
        Assert.Contains("changed or removed", problem.Detail);
    }

    [Fact]
    public async Task Delete_WithStaleETag_Returns409Conflict()
    {
        // Step 1: Create a booking and capture its ETag
        var (created, originalETag) = await CreateBookingAsync(
            new DateTime(2027, 6, 1, 9, 0, 0, DateTimeKind.Utc),
            new DateTime(2027, 6, 1, 10, 0, 0, DateTimeKind.Utc));

        // Step 2: Update the booking to increment its xmin
        var updateRequest = MakePutRequest(created.Id,
            new DateTime(2027, 6, 1, 9, 0, 0, DateTimeKind.Utc),
            new DateTime(2027, 6, 1, 11, 0, 0, DateTimeKind.Utc),
            originalETag);

        var updateResponse = await _client.SendAsync(updateRequest);
        Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);

        // Step 3: Try to delete with the original (stale) ETag — should fail
        var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, $"/api/booking/{created.Id}");
        deleteRequest.Headers.IfMatch.Add(new EntityTagHeaderValue(originalETag));

        var deleteResponse = await _client.SendAsync(deleteRequest);
        Assert.Equal(HttpStatusCode.Conflict, deleteResponse.StatusCode);

        var problem = await deleteResponse.Content.ReadFromJsonAsync<ProblemDetailsJson>();
        Assert.NotNull(problem);
        Assert.Equal(409, problem.Status);
        Assert.Contains("changed or removed", problem.Detail);
    }

    [Fact]
    public async Task Put_WithoutIfMatch_Returns412PreconditionFailed()
    {
        // Create a booking
        var (created, _) = await CreateBookingAsync(
            new DateTime(2027, 7, 1, 9, 0, 0, DateTimeKind.Utc),
            new DateTime(2027, 7, 1, 10, 0, 0, DateTimeKind.Utc));

        // Try to update without If-Match header
        var updateRequest = new HttpRequestMessage(HttpMethod.Put, $"/api/booking/{created.Id}")
        {
            Content = JsonContent.Create(new
            {
                UserId = 2,
                RoomId = 2,
                StartDate = new DateTime(2027, 7, 1, 9, 0, 0, DateTimeKind.Utc),
                EndDate = new DateTime(2027, 7, 1, 11, 0, 0, DateTimeKind.Utc)
            })
        };

        var response = await _client.SendAsync(updateRequest);
        Assert.Equal(HttpStatusCode.PreconditionFailed, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsJson>();
        Assert.NotNull(problem);
        Assert.Equal(412, problem.Status);
    }

    private async Task<(BookingResponse Booking, string ETag)> CreateBookingAsync(
        DateTime start, DateTime end)
    {
        var payload = JsonContent.Create(new
        {
            UserId = 2,
            RoomId = 2,
            StartDate = start,
            EndDate = end
        });

        var response = await _client.PostAsync("/api/booking", payload);
        response.EnsureSuccessStatusCode();

        var etag = response.Headers.ETag?.Tag
            ?? throw new InvalidOperationException("POST response missing ETag header");
        var booking = await response.Content.ReadFromJsonAsync<BookingResponse>()
            ?? throw new InvalidOperationException("POST response body is empty");

        _bookingIds.Add(booking.Id);
        return (booking, etag);
    }

    private static HttpRequestMessage MakePutRequest(int bookingId,
        DateTime start, DateTime end, string etag)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/booking/{bookingId}")
        {
            Content = JsonContent.Create(new
            {
                UserId = 2,
                RoomId = 2,
                StartDate = start,
                EndDate = end
            })
        };
        request.Headers.IfMatch.Add(new EntityTagHeaderValue(etag));
        return request;
    }

    private sealed record ProblemDetailsJson(int Status, string Title, string Detail);
}

internal sealed class TestAppFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;

    public TestAppFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor is not null)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(_connectionString));
        });

        return base.CreateHost(builder);
    }
}
