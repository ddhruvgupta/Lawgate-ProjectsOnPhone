# Backend Documentation

## Overview
.NET 8 Web API with Entity Framework Core, JWT authentication, and comprehensive API documentation via Swagger.

## Project Structure
```
backend/
├── Controllers/        # API endpoints
├── Models/            # Domain models
├── Data/              # DbContext, configurations, seeding
├── Services/          # Business logic
├── Middleware/        # Custom middleware
├── DTOs/              # Data Transfer Objects
├── Migrations/        # EF Core migrations
├── Configurations/    # App configuration
├── Program.cs         # Application entry point
├── appsettings.json   # Configuration (template)
└── docs/              # Documentation
```

## Getting Started

### Prerequisites
- .NET 8 SDK
- PostgreSQL (or Docker)

### First Time Setup
```powershell
# Restore packages
dotnet restore

# Create initial migration (if not exists)
dotnet ef migrations add InitialCreate

# Update database
dotnet ef database update

# Run application
dotnet run
```

### Development
```powershell
# Run with hot reload
dotnet watch run

# Run with seed data
dotnet run --seed-data
```

## API Documentation

### Swagger UI
- Development: http://localhost:5000/swagger
- Provides interactive API documentation
- Test endpoints directly from browser

### API Endpoints

#### Authentication
- `POST /api/auth/register` - Register new user
- `POST /api/auth/login` - Login and get JWT token
- `POST /api/auth/refresh` - Refresh access token
- `POST /api/auth/logout` - Logout user

#### Users
- `GET /api/users` - Get all users (Admin only)
- `GET /api/users/{id}` - Get user by ID
- `PUT /api/users/{id}` - Update user
- `DELETE /api/users/{id}` - Delete user (Admin only)

#### Health
- `GET /api/health` - Health check endpoint

## Configuration

### Connection Strings
Defined in `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=lawgate_db;Username=lawgate_user;Password=your_password"
  }
}
```

### JWT Settings
```json
{
  "Jwt": {
    "Key": "your-secret-key-min-32-characters-long",
    "Issuer": "lawgate-api",
    "Audience": "lawgate-client",
    "ExpiryMinutes": 60
  }
}
```

### CORS
Configure allowed origins in `Program.cs`:
```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy => policy.WithOrigins("http://localhost:3000")
                       .AllowAnyMethod()
                       .AllowAnyHeader());
});
```

## Database Migrations

### Create Migration
```powershell
dotnet ef migrations add <MigrationName>
```

### Update Database
```powershell
dotnet ef database update
```

### Rollback
```powershell
dotnet ef database update <PreviousMigrationName>
```

### Generate SQL Script
```powershell
dotnet ef migrations script -o migration.sql
```

### Remove Last Migration (if not applied)
```powershell
dotnet ef migrations remove
```

## Entity Framework Core

### DbContext
- Located in `Data/ApplicationDbContext.cs`
- Configures all entities and relationships
- Includes audit trail functionality

### Entity Configurations
- Fluent API configurations in `Data/Configurations/`
- Separate file per entity for clean organization
- Defines indexes, constraints, relationships

### Seeding Data
- Initial data in `Data/DbSeeder.cs`
- Run with: `dotnet run --seed-data`
- Creates default roles and admin user

## Authentication & Authorization

### JWT Bearer Authentication
- Tokens expire after configured time (default: 60 minutes)
- Refresh tokens for extended sessions
- Tokens include user ID and roles

### Authorization Policies
- `[Authorize]` - Requires authentication
- `[Authorize(Roles = "Admin")]` - Requires specific role
- `[AllowAnonymous]` - Public endpoint

### Password Security
- BCrypt hashing with salt
- Minimum password requirements enforced
- No plain-text password storage

## Error Handling

### Global Exception Handler
- Catches all unhandled exceptions
- Returns consistent error format
- Logs errors for debugging

### Validation
- Data annotations on DTOs
- FluentValidation for complex rules
- Returns 400 Bad Request with details

## Logging

### Structured Logging
- Uses Serilog for structured logging
- Logs to Console and File
- Configured log levels per environment

### Log Levels
- **Development**: Debug and above
- **Production**: Information and above

## Testing

### Unit Tests
```powershell
cd Backend.Tests
dotnet test
```

### Integration Tests
```powershell
cd Backend.IntegrationTests
dotnet test
```

### Test Database
- Uses Testcontainers for isolated database
- Each test run gets fresh database
- No impact on development database

## Performance

### Caching
- In-memory caching for frequently accessed data
- Redis support ready (commented out)

### Async/Await
- All I/O operations are asynchronous
- Better scalability and resource usage

### Query Optimization
- Includes/ThenIncludes for eager loading
- AsNoTracking for read-only queries
- Compiled queries for hot paths

## Deployment

### Azure App Service
```powershell
# Publish
dotnet publish -c Release -o ./publish

# Deploy (using Azure CLI)
az webapp deploy --resource-group lawgate-rg --name lawgate-api --src-path ./publish.zip
```

### Environment Variables (Azure)
Set in Azure Portal > Configuration:
- `ConnectionStrings__DefaultConnection`
- `Jwt__Key`
- `Jwt__Issuer`
- `Jwt__Audience`

### Health Checks
- Endpoint: `/health`
- Checks database connectivity
- Used by Azure App Service health monitoring

## Security Best Practices

1. **Never commit secrets** - Use User Secrets locally, Key Vault in Azure
2. **HTTPS only** in production
3. **Validate all input** - Data annotations + FluentValidation
4. **Sanitize output** - Prevent XSS attacks
5. **Rate limiting** - Protect against abuse
6. **SQL injection** - Prevented by EF Core parameterization
7. **CORS** - Restrict allowed origins

## Troubleshooting

### "Migration already applied" Error
```powershell
# Check migration status
dotnet ef migrations list

# Remove last migration (if not in production)
dotnet ef migrations remove
```

### "Connection refused" Error
- Check PostgreSQL is running: `docker ps`
- Verify connection string in appsettings
- Check firewall rules

### "Assembly not found" Error
```powershell
# Clean and rebuild
dotnet clean
dotnet restore
dotnet build
```

## Package Management

### Key Packages
- `Microsoft.EntityFrameworkCore` - ORM
- `Npgsql.EntityFrameworkCore.PostgreSQL` - PostgreSQL provider
- `Microsoft.AspNetCore.Authentication.JwtBearer` - JWT auth
- `BCrypt.Net-Next` - Password hashing
- `Swashbuckle.AspNetCore` - Swagger/OpenAPI
- `Serilog.AspNetCore` - Logging

### Update Packages
```powershell
dotnet list package --outdated
dotnet add package <PackageName>
```

## Documentation Updates
When adding new features:
1. Update this README
2. Update `api-reference.md`
3. Add XML comments for Swagger
4. Update `claude.md` for context
