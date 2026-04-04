using System.Net.Http.Json;
using System.Text.Json;
using DotNet.Testcontainers.Builders;
using LegalDocSystem.Domain.Entities;
using LegalDocSystem.Domain.Enums;
using LegalDocSystem.Infrastructure.BackgroundServices;
using LegalDocSystem.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Xunit;
using BCrypt.Net;

namespace LegalDocSystem.IntegrationTests.Infrastructure;

public class TestWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("lawgate_test")
        .WithUsername("testuser")
        .WithPassword("testpassword")
        .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(5432))
        .Build();

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();
    }

    public new async Task DisposeAsync()
    {
        await _dbContainer.StopAsync();
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Remove the existing DbContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (descriptor != null)
                services.Remove(descriptor);

            // Add DbContext pointing to the test container
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(_dbContainer.GetConnectionString()));

            // Remove background service — it queries DB before migrations run in tests
            var cleanupDescriptor = services.SingleOrDefault(
                d => d.ImplementationType == typeof(DocumentCleanupService));
            if (cleanupDescriptor != null)
                services.Remove(cleanupDescriptor);

        });

        // Override the connection string via WebHostBuilder settings
        builder.UseSetting("ConnectionStrings:DefaultConnection", _dbContainer.GetConnectionString());

        // Inject required JWT settings so the app starts in test mode
        builder.UseSetting("Jwt:SecretKey", "integration-test-secret-key-minimum-32-chars!");
        builder.UseSetting("Jwt:Issuer", "LegalDocSystem");
        builder.UseSetting("Jwt:Audience", "LegalDocSystemUsers");
        builder.UseSetting("Jwt:ExpiryMinutes", "60");
    }

    /// <summary>
    /// Runs EF migrations and seeds test data. Call this at the start of each test class.
    /// </summary>
    public async Task InitializeDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();

        // Seed only if empty
        if (await db.Companies.AnyAsync())
            return;

        // Test company
        var company = new Company
        {
            Name = "Test Law Firm",
            Email = "company@test.com",
            Phone = "",
            Address = "",
            City = "",
            State = "",
            Country = "",
            PostalCode = "",
            SubscriptionTier = SubscriptionTier.Trial,
            SubscriptionStartDate = DateTime.UtcNow,
            IsActive = true,
            StorageUsedBytes = 0,
            StorageQuotaBytes = 10L * 1024 * 1024 * 1024,
            CreatedBy = "TestSetup"
        };
        db.Companies.Add(company);
        await db.SaveChangesAsync();

        // Platform company (required by PlatformAdminService)
        var platformCompany = new Company
        {
            Name = "Lawgate Platform",
            Email = "platform@lawgate.io",
            Phone = "",
            Address = "",
            City = "",
            State = "",
            Country = "",
            PostalCode = "",
            SubscriptionTier = SubscriptionTier.Enterprise,
            SubscriptionStartDate = DateTime.UtcNow,
            IsActive = true,
            StorageUsedBytes = 0,
            StorageQuotaBytes = 0,
            CreatedBy = "TestSetup"
        };
        db.Companies.Add(platformCompany);
        await db.SaveChangesAsync();

        // CompanyOwner user
        db.Users.Add(new User
        {
            CompanyId = company.Id,
            FirstName = "Owner",
            LastName = "Test",
            Email = "owner@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test@1234"),
            Phone = "",
            Role = UserRole.CompanyOwner,
            IsActive = true,
            IsEmailVerified = true,
            CreatedBy = "TestSetup"
        });

        // Regular User (no delete permission)
        db.Users.Add(new User
        {
            CompanyId = company.Id,
            FirstName = "Regular",
            LastName = "Member",
            Email = "member@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test@1234"),
            Phone = "",
            Role = UserRole.User,
            IsActive = true,
            IsEmailVerified = true,
            CreatedBy = "TestSetup"
        });

        // PlatformSuperAdmin user
        db.Users.Add(new User
        {
            CompanyId = platformCompany.Id,
            FirstName = "Super",
            LastName = "Admin",
            Email = "superadmin@lawgate.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@1234"),
            Phone = "",
            Role = UserRole.PlatformSuperAdmin,
            IsActive = true,
            IsEmailVerified = true,
            CreatedBy = "TestSetup"
        });

        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Calls POST /api/auth/login and returns the JWT token string.
    /// </summary>
    public async Task<string> GetAuthTokenAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var token = doc.RootElement
            .GetProperty("data")
            .GetProperty("token")
            .GetString();

        return token ?? throw new InvalidOperationException("Token not found in login response");
    }
}
