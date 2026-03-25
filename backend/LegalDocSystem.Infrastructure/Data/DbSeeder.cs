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
            StorageQuotaBytes = 10L * 1024 * 1024 * 1024, // 10 GB
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
            StartDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = owner.Email
        };

        context.Projects.Add(project);
        await context.SaveChangesAsync();

        logger.LogInformation("Database seeded successfully. Demo credentials: admin@demolawfirm.com / Admin@123");
    }
}
