using RoomBooking.Api.Authentication;
using RoomBooking.Api.Models.DTOs;
using RoomBooking.Api.Repositories.Booking;
using RoomBooking.Api.Services.Booking;

namespace RoomBooking.Api.Tests;

public class BookingOwnershipServiceTests
{
    private static BookingService CreateService(
        FakeBookingRepository repository,
        CurrentUser caller) =>
        new(repository, new FakeCurrentUser(caller));

    private static BookingRequest SampleRequest() => new()
    {
        RoomId = 2,
        StartDate = new DateTime(2027, 8, 1, 9, 0, 0, DateTimeKind.Utc),
        EndDate = new DateTime(2027, 8, 1, 10, 0, 0, DateTimeKind.Utc)
    };

    [Fact]
    public async Task GetAll_DelegatesToRepository_WithCaller()
    {
        var repository = new FakeBookingRepository(new Dictionary<int, int>());
        var service = CreateService(repository, new CurrentUser(2, IsAdmin: false));

        await service.GetAllBookingsAsync();

        Assert.Equal(new CurrentUser(2, false), repository.ReceivedCaller);
    }

    [Fact]
    public async Task GetById_OwnBooking_ReturnsBooking()
    {
        var repository = new FakeBookingRepository(new Dictionary<int, int> { [5] = 2 });
        var service = CreateService(repository, new CurrentUser(2, IsAdmin: false));

        var booking = await service.GetBookingByIdAsync(5);

        Assert.NotNull(booking);
        Assert.Equal(5, booking!.Id);
    }

    [Fact]
    public async Task GetById_OtherUsersBooking_NonAdmin_ThrowsForbidden()
    {
        var repository = new FakeBookingRepository(new Dictionary<int, int> { [5] = 1 });
        var service = CreateService(repository, new CurrentUser(2, IsAdmin: false));

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => service.GetBookingByIdAsync(5));
    }

    [Fact]
    public async Task GetById_OtherUsersBooking_Admin_ReturnsBooking()
    {
        var repository = new FakeBookingRepository(new Dictionary<int, int> { [5] = 1 });
        var service = CreateService(repository, new CurrentUser(1, IsAdmin: true));

        var booking = await service.GetBookingByIdAsync(5);

        Assert.NotNull(booking);
    }

    [Fact]
    public async Task GetById_MissingBooking_ReturnsNull()
    {
        var repository = new FakeBookingRepository(new Dictionary<int, int>());
        var service = CreateService(repository, new CurrentUser(2, IsAdmin: false));

        var booking = await service.GetBookingByIdAsync(5);

        Assert.Null(booking);
    }

    [Fact]
    public async Task Update_OwnBooking_ProceedsToRepository()
    {
        var repository = new FakeBookingRepository(new Dictionary<int, int> { [5] = 2 });
        var service = CreateService(repository, new CurrentUser(2, IsAdmin: false));

        var result = await service.UpdateBookingAsync(5, SampleRequest());

        Assert.True(result);
        Assert.Equal(1, repository.UpdateCalls);
        Assert.Equal(1, repository.SaveChangesCalls);
    }

    [Fact]
    public async Task Update_OtherUsersBooking_NonAdmin_ThrowsForbidden()
    {
        var repository = new FakeBookingRepository(new Dictionary<int, int> { [5] = 1 });
        var service = CreateService(repository, new CurrentUser(2, IsAdmin: false));

        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => service.UpdateBookingAsync(5, SampleRequest()));

        Assert.Equal(0, repository.UpdateCalls);
    }

    [Fact]
    public async Task Update_OtherUsersBooking_Admin_ProceedsToRepository()
    {
        var repository = new FakeBookingRepository(new Dictionary<int, int> { [5] = 1 });
        var service = CreateService(repository, new CurrentUser(1, IsAdmin: true));

        var result = await service.UpdateBookingAsync(5, SampleRequest());

        Assert.True(result);
        Assert.Equal(1, repository.UpdateCalls);
    }

    [Fact]
    public async Task Update_MissingBooking_ReturnsFalse()
    {
        var repository = new FakeBookingRepository(new Dictionary<int, int>());
        var service = CreateService(repository, new CurrentUser(2, IsAdmin: false));

        var result = await service.UpdateBookingAsync(5, SampleRequest());

        Assert.False(result);
        Assert.Equal(0, repository.UpdateCalls);
    }

    [Fact]
    public async Task Delete_OwnBooking_ProceedsToRepository()
    {
        var repository = new FakeBookingRepository(new Dictionary<int, int> { [5] = 2 });
        var service = CreateService(repository, new CurrentUser(2, IsAdmin: false));

        var result = await service.DeleteBookingAsync(5);

        Assert.True(result);
        Assert.Equal(1, repository.RemoveCalls);
        Assert.Equal(1, repository.SaveChangesCalls);
    }

    [Fact]
    public async Task Delete_OtherUsersBooking_NonAdmin_ThrowsForbidden()
    {
        var repository = new FakeBookingRepository(new Dictionary<int, int> { [5] = 1 });
        var service = CreateService(repository, new CurrentUser(2, IsAdmin: false));

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => service.DeleteBookingAsync(5));

        Assert.Equal(0, repository.RemoveCalls);
    }

    [Fact]
    public async Task Delete_OtherUsersBooking_Admin_ProceedsToRepository()
    {
        var repository = new FakeBookingRepository(new Dictionary<int, int> { [5] = 2 });
        var service = CreateService(repository, new CurrentUser(1, IsAdmin: true));

        var result = await service.DeleteBookingAsync(5);

        Assert.True(result);
        Assert.Equal(1, repository.RemoveCalls);
    }

    [Fact]
    public async Task Delete_MissingBooking_ReturnsFalse()
    {
        var repository = new FakeBookingRepository(new Dictionary<int, int>());
        var service = CreateService(repository, new CurrentUser(2, IsAdmin: false));

        var result = await service.DeleteBookingAsync(5);

        Assert.False(result);
        Assert.Equal(0, repository.RemoveCalls);
    }

    [Fact]
    public async Task Create_AssignsOwnershipToCaller()
    {
        var repository = new FakeBookingRepository(new Dictionary<int, int>());
        var service = CreateService(repository, new CurrentUser(2, IsAdmin: false));

        var created = await service.CreateBookingAsync(SampleRequest());

        Assert.Equal(2, repository.ReceivedCallerId);
        Assert.Equal(2, created.UserId);
    }

    private sealed class FakeCurrentUser(CurrentUser value) : ICurrentUser
    {
        public CurrentUser Value { get; } = value;
    }

    private sealed class FakeBookingRepository : IBookingRepository
    {
        private readonly Dictionary<int, int> _owners;

        public FakeBookingRepository(Dictionary<int, int> owners)
        {
            _owners = owners;
        }

        public CurrentUser? ReceivedCaller { get; private set; }
        public int? ReceivedCallerId { get; private set; }
        public int UpdateCalls { get; private set; }
        public int RemoveCalls { get; private set; }
        public int SaveChangesCalls { get; private set; }

        public Task<IEnumerable<BookingResponse>> GetAllAsync(CurrentUser caller)
        {
            ReceivedCaller = caller;
            return Task.FromResult(Enumerable.Empty<BookingResponse>());
        }

        public Task<BookingResponse?> GetByIdAsync(int id)
        {
            if (!_owners.TryGetValue(id, out var owner))
            {
                return Task.FromResult<BookingResponse?>(null);
            }

            return Task.FromResult<BookingResponse?>(new BookingResponse
            {
                Id = id,
                UserId = owner,
                RoomId = 2
            });
        }

        public Task<BookingResponse> AddAsync(BookingRequest booking, int callerId)
        {
            ReceivedCallerId = callerId;
            return Task.FromResult(new BookingResponse
            {
                Id = 99,
                UserId = callerId,
                RoomId = booking.RoomId,
                StartDate = booking.StartDate,
                EndDate = booking.EndDate
            });
        }

        public Task<bool> UpdateAsync(int id, BookingRequest booking, uint? expectedVersion = null)
        {
            UpdateCalls++;
            return Task.FromResult(true);
        }

        public Task<bool> RemoveAsync(int id, uint? expectedVersion = null)
        {
            RemoveCalls++;
            return Task.FromResult(true);
        }

        public Task<int?> GetOwnerIdAsync(int id)
        {
            return Task.FromResult(_owners.TryGetValue(id, out var owner) ? owner : (int?)null);
        }

        public Task SaveChangesAsync()
        {
            SaveChangesCalls++;
            return Task.CompletedTask;
        }
    }
}