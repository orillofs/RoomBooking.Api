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

/// <summary>
/// End-to-end ownership tests through the real HTTP pipeline: the DevAuth scheme maps
/// Bearer tokens to seeded users, the service enforces ownership, and the controller
/// renders 403 problem-details. Requires the ConnectionStrings:TestDatabase user secret.
/// </summary>
[Collection("PostgreSql database")]
public class BookingEndpointAuthorizationTests : IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _userClient;   // bearer user-token  => UserId 2
    private readonly HttpClient _adminClient;  // bearer admin-token => UserId 1 (Admin)
    private readonly HttpClient _anonClient;   // no token
    private readonly string _connectionString;
    private readonly List<int> _bookingIds = [];

    public BookingEndpointAuthorizationTests()
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets<BookingEndpointAuthorizationTests>()
            .Build();

        _connectionString = configuration.GetConnectionString("TestDatabase")
            ?? throw new InvalidOperationException(
                "ConnectionStrings:TestDatabase must be configured with dotnet user-secrets.");

        _factory = new TestAppFactory(_connectionString);
        _userClient = CreateAuthorizedClient("user-token");
        _adminClient = CreateAuthorizedClient("admin-token");
        _anonClient = _factory.CreateClient();
    }

    private HttpClient CreateAuthorizedClient(string token)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
        return client;
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
    public async Task Get_WithoutToken_Returns401()
    {
        var response = await _anonClient.GetAsync("/api/booking");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Post_WithoutToken_Returns401()
    {
        var payload = JsonContent.Create(BookingPayload());
        var response = await _anonClient.PostAsync("/api/booking", payload);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetList_AsUser_ReturnsOnlyOwnedBookings()
    {
        var response = await _userClient.GetAsync("/api/booking");

        response.EnsureSuccessStatusCode();
        var bookings = await response.Content.ReadFromJsonAsync<List<BookingResponse>>();
        Assert.NotNull(bookings);
        Assert.NotEmpty(bookings);
        Assert.All(bookings, b => Assert.Equal(2, b.UserId));
        Assert.DoesNotContain(bookings, b => b.Id == 1 && b.UserId != 2);
    }

    [Fact]
    public async Task GetById_AsUser_OwnedSeedBooking_Returns200()
    {
        // Seed booking id 1 is owned by UserId 2 (the user-token caller)
        var response = await _userClient.GetAsync("/api/booking/1");

        response.EnsureSuccessStatusCode();
        var booking = await response.Content.ReadFromJsonAsync<BookingResponse>();
        Assert.NotNull(booking);
        Assert.Equal(1, booking.Id);
        Assert.Equal(2, booking.UserId);
        Assert.NotNull(response.Headers.ETag);
    }

    [Fact]
    public async Task GetById_AsUser_TryReadAdminsBooking_Returns403()
    {
        var adminBooking = await CreateBookingAsAsync(_adminClient);

        var response = await _userClient.GetAsync($"/api/booking/{adminBooking.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsJson>();
        Assert.NotNull(problem);
        Assert.Equal(403, problem.Status);
        Assert.Equal("Forbidden", problem.Title);
    }

    [Fact]
    public async Task Delete_AsUser_TryDeleteAdminsBooking_Returns403()
    {
        var adminBooking = await CreateBookingAsAsync(_adminClient);
        var etag = await GetETagAsync(_adminClient, adminBooking.Id);

        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/booking/{adminBooking.Id}");
        request.Headers.IfMatch.Add(new EntityTagHeaderValue(etag));

        var response = await _userClient.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsJson>();
        Assert.NotNull(problem);
        Assert.Equal(403, problem.Status);
    }

    [Fact]
    public async Task GetById_AsUser_NonexistentBooking_Returns404()
    {
        var response = await _userClient.GetAsync("/api/booking/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetList_AsAdmin_ContainsSeedBooking()
    {
        var response = await _adminClient.GetAsync("/api/booking");

        response.EnsureSuccessStatusCode();
        var bookings = await response.Content.ReadFromJsonAsync<List<BookingResponse>>();
        Assert.NotNull(bookings);
        Assert.Contains(bookings, b => b.Id == 1 && b.UserId == 2);
    }

    [Fact]
    public async Task Post_AsUser_AssertsCallerOwnership()
    {
        var response = await _userClient.PostAsync("/api/booking", JsonContent.Create(BookingPayload()));

        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<BookingResponse>();
        Assert.NotNull(created);
        Assert.Equal(2, created.UserId); // caller identity, not a client-supplied field

        _bookingIds.Add(created.Id);
    }

    [Fact]
    public async Task Delete_AsAdmin_CanDeleteUsersBooking_Returns204()
    {
        var userBooking = await CreateBookingAsAsync(_userClient);
        var etag = await GetETagAsync(_adminClient, userBooking.Id);

        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/booking/{userBooking.Id}");
        request.Headers.IfMatch.Add(new EntityTagHeaderValue(etag));

        var response = await _adminClient.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    // Each created row needs a unique room-2 slot so the GiST exclusion constraint
    // does not reject a later test's insert. A static counter guarantees uniqueness
    // even across test-class instances (one instance per test method).
    private static int _slot = 0;

    private static object BookingPayload() => BookingSlot(Interlocked.Increment(ref _slot));

    private static object BookingSlot(int slot) => new
    {
        // No UserId field: ownership comes from the authenticated caller
        RoomId = 2,
        StartDate = new DateTime(2028, 4, 1, 0, 0, 0, DateTimeKind.Utc).AddHours(slot),
        EndDate = new DateTime(2028, 4, 1, 0, 0, 0, DateTimeKind.Utc).AddHours(slot + 1)
    };

    private async Task<BookingResponse> CreateBookingAsAsync(HttpClient client)
    {
        var response = await client.PostAsync("/api/booking", JsonContent.Create(BookingPayload()));
        response.EnsureSuccessStatusCode();

        var created = await response.Content.ReadFromJsonAsync<BookingResponse>()
            ?? throw new InvalidOperationException("POST response body is empty");

        _bookingIds.Add(created.Id);
        return created;
    }

    private static async Task<string> GetETagAsync(HttpClient client, int bookingId)
    {
        var response = await client.GetAsync($"/api/booking/{bookingId}");
        response.EnsureSuccessStatusCode();

        return response.Headers.ETag?.Tag
            ?? throw new InvalidOperationException("GET response missing ETag header");
    }

    private sealed record ProblemDetailsJson(int Status, string Title, string Detail);
}