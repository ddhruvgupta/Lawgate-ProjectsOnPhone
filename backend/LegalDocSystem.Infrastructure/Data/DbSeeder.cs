using LegalDocSystem.Domain.Entities;
using LegalDocSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LegalDocSystem.Infrastructure.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context, ILogger logger)
    {
        // Only seed if the database is empty
        if (await context.Companies.AnyAsync())
        {
            logger.LogInformation("Database already seeded. Skipping.");
            return;
        }

        logger.LogInformation("Seeding database with initial data...");

        // Create demo company
        var company = new Company
        {
            Name = "Demo Law Firm",
            Email = "admin@demolawfirm.com",
            Phone = "+1-555-0100",
            Address = "123 Legal Street",
            City = "New York",
            State = "NY",
            Country = "USA",
            PostalCode = "10001",
            SubscriptionTier = SubscriptionTier.Trial,
            SubscriptionStartDate = DateTime.UtcNow,
            SubscriptionEndDate = DateTime.UtcNow.AddDays(14),
            IsActive = true,
            StorageUsedBytes = 0,
            StorageQuotaBytes = 1L * 1024 * 1024 * 1024, // 1 GB (Trial)
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "Seeder"
        };

        context.Companies.Add(company);
        await context.SaveChangesAsync();

        // Create owner user (password: Admin@123)
        var ownerPasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123");
        var owner = new User
        {
            CompanyId = company.Id,
            FirstName = "Admin",
            LastName = "User",
            Email = "admin@demolawfirm.com",
            PasswordHash = ownerPasswordHash,
            Phone = "+1-555-0101",
            Role = UserRole.CompanyOwner,
            IsActive = true,
            IsEmailVerified = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "Seeder"
        };

        // Create standard user (password: User@123)
        var userPasswordHash = BCrypt.Net.BCrypt.HashPassword("User@123");
        var standardUser = new User
        {
            CompanyId = company.Id,
            FirstName = "Jane",
            LastName = "Doe",
            Email = "jane.doe@demolawfirm.com",
            PasswordHash = userPasswordHash,
            Phone = "+1-555-0102",
            Role = UserRole.User,
            IsActive = true,
            IsEmailVerified = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "Seeder"
        };

        context.Users.AddRange(owner, standardUser);
        await context.SaveChangesAsync();

        // Create a sample project
        var project = new Project
        {
            CompanyId = company.Id,
            Name = "Sample Contract Review",
            Description = "Demo project for testing the system",
            ClientName = "Acme Corporation",
            CaseNumber = "CASE-2026-001",
            Status = ProjectStatus.Active,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            CreatedAt = DateTime.UtcNow,
            CreatedBy = owner.Email
        };

        context.Projects.Add(project);
        await context.SaveChangesAsync();

        logger.LogInformation("Database seeded successfully. Demo admin user email: admin@demolawfirm.com");
    }

    /// <summary>
    /// Seeds Lawgate platform admin users. Runs independently of the demo company seed —
    /// safe to call on an already-seeded DB (idempotent by email check).
    /// </summary>
    public static async Task SeedPlatformAdminsAsync(ApplicationDbContext context, ILogger logger)
    {
        const string platformEmail = "platform@lawgate.io";

        if (await context.Companies.AnyAsync(c => c.Email == platformEmail))
        {
            logger.LogInformation("Platform admin company already seeded. Skipping.");
            return;
        }

        logger.LogInformation("Seeding Lawgate platform admin accounts...");

        var platformCompany = new Company
        {
            Name = "Lawgate Platform",
            Email = platformEmail,
            Phone = string.Empty,
            Address = string.Empty,
            City = string.Empty,
            State = string.Empty,
            Country = string.Empty,
            PostalCode = string.Empty,
            SubscriptionTier = SubscriptionTier.Enterprise,
            SubscriptionStartDate = DateTime.UtcNow,
            IsActive = true,
            StorageUsedBytes = 0,
            StorageQuotaBytes = 0,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "Seeder"
        };

        context.Companies.Add(platformCompany);
        await context.SaveChangesAsync();

        context.Users.AddRange(
            new User
            {
                CompanyId = platformCompany.Id,
                FirstName = "Platform",
                LastName = "Admin",
                Email = "admin@lawgate.io",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("LawgatePlatform@1"),
                Phone = string.Empty,
                Role = UserRole.PlatformAdmin,
                IsActive = true,
                IsEmailVerified = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "Seeder"
            },
            new User
            {
                CompanyId = platformCompany.Id,
                FirstName = "Super",
                LastName = "Admin",
                Email = "superadmin@lawgate.io",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("LawgateSuperAdmin@1"),
                Phone = string.Empty,
                Role = UserRole.PlatformSuperAdmin,
                IsActive = true,
                IsEmailVerified = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "Seeder"
            }
        );

        await context.SaveChangesAsync();
        logger.LogInformation("Platform admins seeded: admin@lawgate.io / LawgatePlatform@1, superadmin@lawgate.io / LawgateSuperAdmin@1");
    }
}
