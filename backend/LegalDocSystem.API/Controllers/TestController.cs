using LegalDocSystem.Domain.Entities;
using LegalDocSystem.Domain.Enums;
using LegalDocSystem.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LegalDocSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<TestController> _logger;

    public TestController(ApplicationDbContext context, ILogger<TestController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Test database connection
    /// </summary>
    [HttpGet("database")]
    public async Task<IActionResult> TestDatabase()
    {
        try
        {
            var canConnect = await _context.Database.CanConnectAsync();
            
            if (!canConnect)
            {
                return StatusCode(500, new { 
                    success = false, 
                    message = "Cannot connect to database" 
                });
            }

            var companyCount = await _context.Companies.CountAsync();
            var userCount = await _context.Users.CountAsync();
            var projectCount = await _context.Projects.CountAsync();
            var documentCount = await _context.Documents.CountAsync();

            return Ok(new
            {
                success = true,
                message = "Database connection successful",
                statistics = new
                {
                    companies = companyCount,
                    users = userCount,
                    projects = projectCount,
                    documents = documentCount
                },
                databaseProvider = _context.Database.ProviderName,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database test failed");
            return StatusCode(500, new { 
                success = false, 
                message = ex.Message 
            });
        }
    }

    /// <summary>
    /// Create a test company (for development only)
    /// </summary>
    [HttpPost("seed-company")]
    public async Task<IActionResult> SeedTestCompany()
    {
        try
        {
            // Check if company already exists
            var existingCompany = await _context.Companies
                .FirstOrDefaultAsync(c => c.Email == "test@lawfirm.com");

            if (existingCompany != null)
            {
                return Ok(new
                {
                    success = true,
                    message = "Test company already exists",
                    companyId = existingCompany.Id
                });
            }

            // Create test company
            var company = new Company
            {
                Name = "Test Law Firm",
                Email = "test@lawfirm.com",
                Phone = "+1-555-0123",
                Address = "123 Legal Street",
                City = "New York",
                State = "NY",
                Country = "USA",
                PostalCode = "10001",
                SubscriptionTier = SubscriptionTier.Professional,
                SubscriptionStartDate = DateTime.UtcNow,
                SubscriptionEndDate = DateTime.UtcNow.AddYears(1),
                IsActive = true,
                StorageUsedBytes = 0,
                StorageQuotaBytes = 500L * 1024 * 1024 * 1024, // 500 GB
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "System"
            };

            _context.Companies.Add(company);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Test company created with ID: {CompanyId}", company.Id);

            return Ok(new
            {
                success = true,
                message = "Test company created successfully",
                company = new
                {
                    id = company.Id,
                    name = company.Name,
                    email = company.Email,
                    subscriptionTier = company.SubscriptionTier.ToString()
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to seed test company");
            return StatusCode(500, new { 
                success = false, 
                message = ex.Message 
            });
        }
    }

    /// <summary>
    /// Create a test user (for development only)
    /// </summary>
    [HttpPost("seed-user")]
    public async Task<IActionResult> SeedTestUser()
    {
        try
        {
            // Get or create company first
            var company = await _context.Companies
                .FirstOrDefaultAsync(c => c.Email == "test@lawfirm.com");

            if (company == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Test company not found. Please create it first using /seed-company"
                });
            }

            // Check if user already exists
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == "admin@lawfirm.com");

            if (existingUser != null)
            {
                return Ok(new
                {
                    success = true,
                    message = "Test user already exists",
                    userId = existingUser.Id
                });
            }

            // Create test user (password: Admin123!)
            var passwordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!");

            var user = new User
            {
                CompanyId = company.Id,
                FirstName = "Admin",
                LastName = "User",
                Email = "admin@lawfirm.com",
                PasswordHash = passwordHash,
                Phone = "+1-555-0124",
                Role = UserRole.CompanyOwner,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "System"
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Test user created with ID: {UserId}", user.Id);

            return Ok(new
            {
                success = true,
                message = "Test user created successfully",
                user = new
                {
                    id = user.Id,
                    email = user.Email,
                    name = $"{user.FirstName} {user.LastName}",
                    role = user.Role.ToString(),
                    password = "Admin123!"
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to seed test user");
            return StatusCode(500, new { 
                success = false, 
                message = ex.Message 
            });
        }
    }

    /// <summary>
    /// List all companies
    /// </summary>
    [HttpGet("companies")]
    public async Task<IActionResult> ListCompanies()
    {
        try
        {
            var companies = await _context.Companies
                .Where(c => !c.IsDeleted)
                .Select(c => new
                {
                    id = c.Id,
                    name = c.Name,
                    email = c.Email,
                    subscriptionTier = c.SubscriptionTier.ToString(),
                    isActive = c.IsActive,
                    userCount = c.Users.Count,
                    projectCount = c.Projects.Count
                })
                .ToListAsync();

            return Ok(new
            {
                success = true,
                count = companies.Count,
                companies
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list companies");
            return StatusCode(500, new { 
                success = false, 
                message = ex.Message 
            });
        }
    }

    /// <summary>
    /// List all users
    /// </summary>
    [HttpGet("users")]
    public async Task<IActionResult> ListUsers()
    {
        try
        {
            var users = await _context.Users
                .Include(u => u.Company)
                .Where(u => !u.IsDeleted)
                .Select(u => new
                {
                    id = u.Id,
                    email = u.Email,
                    name = $"{u.FirstName} {u.LastName}",
                    role = u.Role.ToString(),
                    company = u.Company.Name,
                    isActive = u.IsActive,
                    createdAt = u.CreatedAt
                })
                .ToListAsync();

            return Ok(new
            {
                success = true,
                count = users.Count,
                users
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list users");
            return StatusCode(500, new { 
                success = false, 
                message = ex.Message 
            });
        }
    }

    /// <summary>
    /// Clear all test data
    /// </summary>
    [HttpDelete("clear-data")]
    public async Task<IActionResult> ClearTestData()
    {
        try
        {
            var documentsDeleted = await _context.Documents.ExecuteDeleteAsync();
            var projectPermissionsDeleted = await _context.ProjectPermissions.ExecuteDeleteAsync();
            var projectsDeleted = await _context.Projects.ExecuteDeleteAsync();
            var auditLogsDeleted = await _context.AuditLogs.ExecuteDeleteAsync();
            var usersDeleted = await _context.Users.ExecuteDeleteAsync();
            var companiesDeleted = await _context.Companies.ExecuteDeleteAsync();

            return Ok(new
            {
                success = true,
                message = "All test data cleared",
                deleted = new
                {
                    documents = documentsDeleted,
                    projectPermissions = projectPermissionsDeleted,
                    projects = projectsDeleted,
                    auditLogs = auditLogsDeleted,
                    users = usersDeleted,
                    companies = companiesDeleted
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear test data");
            return StatusCode(500, new { 
                success = false, 
                message = ex.Message 
            });
        }
    }
}
