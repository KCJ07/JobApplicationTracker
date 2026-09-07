using BlazorBootstrap;
using JobApplicationTracker.Data;
using JobApplicationTracker.Models;
using JobApplicationTracker.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace JobApplicationTracker.Tests.Integration;

// Uses a real Sqlite connection instead of the EF Core InMemory provider because
// DeleteApplicationAsync relies on ExecuteDelete, which InMemory doesn't support.
public class ApplicationServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ApplicationDbContext _context;
    private readonly ApplicationService _service;

    public ApplicationServiceTests()
    {
        // connection has to stay open for the lifetime of the test, closing it drops the in-memory db
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated();

        _service = new ApplicationService(new TestDbContextFactory(options));
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    private class TestDbContextFactory : IDbContextFactory<ApplicationDbContext>
    {
        private readonly DbContextOptions<ApplicationDbContext> _options;
        public TestDbContextFactory(DbContextOptions<ApplicationDbContext> options) => _options = options;
        public ApplicationDbContext CreateDbContext() => new(_options);
        public Task<ApplicationDbContext> CreateDbContextAsync(CancellationToken ct = default) => Task.FromResult(CreateDbContext());
    }

    // Application.ApplicationUserId is a required FK, so every test needs a real user row first
    private async Task<string> SeedUserAsync(string email)
    {
        var user = new ApplicationUser { UserName = email, Email = email };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user.Id;
    }

    [Fact]
    public async Task CreateApplicationAsync_ThenGetApplicationsAsync_RoundTripsTheData()
    {
        var userId = await SeedUserAsync("alice@example.com");

        await _service.CreateApplicationAsync(
            userId, ApplicationStatus.Applied, heardBack: false,
            reachOutDate: new DateOnly(2026, 1, 1), dateApplied: new DateOnly(2026, 1, 1),
            notes: "first try", jobTitle: "Backend Engineer", company: "Acme",
            website: "https://acme.example", appType: ApplicationType.FullTime,
            state: "CA", description: "desc", linkedlnRecruiter: "recruiter@acme.example");

        var (data, totalCount) = await _service.GetApplicationsAsync(
            pageNumber: 1, pageSize: 10, sortString: "", sortDirection: SortDirection.Ascending, userId);

        Assert.Equal(1, totalCount);
        Assert.Single(data);
        Assert.Equal("Backend Engineer", data[0].Job.JobTitle);
        Assert.Equal("Acme", data[0].Job.Company);
    }

    [Fact]
    public async Task GetApplicationsAsync_OnlyReturnsRowsBelongingToTheRequestedUser()
    {
        var aliceId = await SeedUserAsync("alice@example.com");
        var bobId = await SeedUserAsync("bob@example.com");

        await _service.CreateApplicationAsync(aliceId, ApplicationStatus.Applied, false,
            DateOnly.FromDateTime(DateTime.Today), DateOnly.FromDateTime(DateTime.Today),
            "", "Alice's Job", "A Co", "", ApplicationType.FullTime, "CA", "", "");

        await _service.CreateApplicationAsync(bobId, ApplicationStatus.Applied, false,
            DateOnly.FromDateTime(DateTime.Today), DateOnly.FromDateTime(DateTime.Today),
            "", "Bob's Job", "B Co", "", ApplicationType.FullTime, "NY", "", "");

        var (data, totalCount) = await _service.GetApplicationsAsync(1, 10, "", SortDirection.Ascending, aliceId);

        Assert.Equal(1, totalCount);
        Assert.Equal("Alice's Job", data[0].Job.JobTitle);
    }

    [Fact]
    public async Task GetApplicationsAsync_SortsByCompanyNameDescendingWhenRequested()
    {
        var userId = await SeedUserAsync("alice@example.com");

        await _service.CreateApplicationAsync(userId, ApplicationStatus.Applied, false,
            DateOnly.FromDateTime(DateTime.Today), DateOnly.FromDateTime(DateTime.Today),
            "", "Job A", "Acme", "", ApplicationType.FullTime, "CA", "", "");

        await _service.CreateApplicationAsync(userId, ApplicationStatus.Applied, false,
            DateOnly.FromDateTime(DateTime.Today), DateOnly.FromDateTime(DateTime.Today),
            "", "Job B", "Zenith", "", ApplicationType.FullTime, "NY", "", "");

        var (data, _) = await _service.GetApplicationsAsync(1, 10, "CompanyName", SortDirection.Descending, userId);

        Assert.Equal("Zenith", data[0].Job.Company);
        Assert.Equal("Acme", data[1].Job.Company);
    }

    [Fact]
    public async Task DeleteApplicationAsync_RemovesTheApplicationForThatUser()
    {
        var userId = await SeedUserAsync("alice@example.com");

        await _service.CreateApplicationAsync(userId, ApplicationStatus.Applied, false,
            DateOnly.FromDateTime(DateTime.Today), DateOnly.FromDateTime(DateTime.Today),
            "", "Job A", "Acme", "", ApplicationType.FullTime, "CA", "", "");

        var created = await _context.Applications.FirstAsync();

        await _service.DeleteApplicationAsync(created.Id, userId);

        var (_, totalCount) = await _service.GetApplicationsAsync(1, 10, "", SortDirection.Ascending, userId);
        Assert.Equal(0, totalCount);
    }

    [Fact]
    public async Task DeleteApplicationAsync_DoesNothingWhenUserIdDoesNotMatch()
    {
        // makes sure ownership is enforced in the service, not just hidden in the UI
        var ownerId = await SeedUserAsync("alice@example.com");
        var attackerId = await SeedUserAsync("mallory@example.com");

        await _service.CreateApplicationAsync(ownerId, ApplicationStatus.Applied, false,
            DateOnly.FromDateTime(DateTime.Today), DateOnly.FromDateTime(DateTime.Today),
            "", "Job A", "Acme", "", ApplicationType.FullTime, "CA", "", "");

        var created = await _context.Applications.FirstAsync();

        await _service.DeleteApplicationAsync(created.Id, attackerId);

        var (_, totalCount) = await _service.GetApplicationsAsync(1, 10, "", SortDirection.Ascending, ownerId);
        Assert.Equal(1, totalCount);
    }
}
