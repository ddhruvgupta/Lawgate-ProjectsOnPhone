using LegalDocSystem.Infrastructure.Data;
using LegalDocSystem.Application.Interfaces;
using LegalDocSystem.Infrastructure.Services;
using LegalDocSystem.Infrastructure.BackgroundServices;
using LegalDocSystem.API.Middleware;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/legaldoc-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // Include XML comments from the API project
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        options.IncludeXmlComments(xmlPath);
});

// In-memory cache (company lookups, etc. — 5-minute sliding TTL)
builder.Services.AddMemoryCache();

// Configure Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// Register Services
builder.Services.Configure<UploadOptions>(builder.Configuration.GetSection(UploadOptions.SectionName));
builder.Services.Configure<TierStorageLimits>(builder.Configuration.GetSection(TierStorageLimits.SectionName));
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

// Use ACS (Azure Communication Services) in production; console stub in development
if (builder.Environment.IsProduction())
    builder.Services.AddScoped<IEmailService, AcsEmailService>();
else
    builder.Services.AddScoped<IEmailService, ConsoleEmailService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IBlobStorageService, AzureBlobStorageService>();
builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<ICompanyService, CompanyService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IPlatformAdminService, PlatformAdminService>();

// Register Background Services
builder.Services.AddHostedService<DocumentCleanupService>();

// Rate Limiting — auth endpoints: 10 req/min; global: 100 req/min
// Disabled in Testing environment to avoid 429s in integration tests
var isTesting = builder.Environment.IsEnvironment("Testing");
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("auth", limiterOptions =>
    {
        limiterOptions.PermitLimit = isTesting ? int.MaxValue : 10;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = isTesting ? int.MaxValue : 0;
    });

    options.AddFixedWindowLimiter("global", limiterOptions =>
    {
        limiterOptions.PermitLimit = isTesting ? int.MaxValue : 100;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = isTesting ? int.MaxValue : 5;
    });

    options.RejectionStatusCode = 429;
});

// Response Compression
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSettings["SecretKey"];
if (string.IsNullOrWhiteSpace(secretKey))
    throw new InvalidOperationException("Jwt:SecretKey is not configured. Set it via environment variable, user-secrets, or Key Vault.");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!))
    };
});

builder.Services.AddAuthorization();

// Configure CORS
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(allowedOrigins ?? new[] { "http://localhost:5173" })
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Apply pending EF Core migrations on startup in non-Development environments.
// Development uses the dedicated migrate-and-seed block further below.
// This runs inside the VNet (App Service has VNet integration), so the DB is reachable.
// db.Database.Migrate() is idempotent — safe to call on every startup.
// NOTE: This app runs as a single App Service instance; scale-out migrations
// would need an advisory lock or a dedicated migration job.
if (!app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();
    try
    {
        // Acquire a PostgreSQL session-level advisory lock before running migrations.
        // This serialises concurrent startup migrations during scale-out events —
        // only one instance migrates at a time; others block until it completes.
        // The lock is automatically released when this connection closes (scope disposal).
        const long MigrationLockId = 0x4C617767617465L; // "Lawgate" in ASCII as int64
        db.Database.OpenConnection();
        using (var lockCmd = db.Database.GetDbConnection().CreateCommand())
        {
            lockCmd.CommandText = $"SELECT pg_advisory_lock({MigrationLockId})";
            lockCmd.ExecuteNonQuery();
        }
        logger.LogInformation("Migration advisory lock acquired; applying pending migrations");
        db.Database.Migrate();
        logger.LogInformation("Database migrations applied successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to apply database migrations on startup");
        throw;
    }
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();

app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseMiddleware<InputSanitizationMiddleware>();

app.UseResponseCompression();

app.UseHttpsRedirection();

app.UseCors();

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers().RequireRateLimiting("global");

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { 
    status = "healthy", 
    timestamp = DateTime.UtcNow 
}))
.WithName("HealthCheck");

Log.Information("Legal Document System API starting...");

// Run database migrations and seeder on startup (development only)
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<LegalDocSystem.Infrastructure.Data.ApplicationDbContext>();
    var seederLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        seederLogger.LogInformation("Applying database migrations...");
        await dbContext.Database.MigrateAsync();
        seederLogger.LogInformation("Database migrations applied successfully. Seeding development data...");
        await LegalDocSystem.Infrastructure.Data.DbSeeder.SeedAsync(dbContext, seederLogger);
        await LegalDocSystem.Infrastructure.Data.DbSeeder.SeedPlatformAdminsAsync(dbContext, seederLogger);
        seederLogger.LogInformation("Development database seeding completed successfully.");
    }
    catch (Exception ex)
    {
        seederLogger.LogError(ex, "An error occurred while migrating or seeding the development database.");
        throw;
    }
}

app.Run();
