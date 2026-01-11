# Database Recreation Script
# This script completely recreates the database from scratch
# Run this when returning to the project after a long break!

param(
    [string]$DbHost = "localhost",
    [string]$DbName = "lawgate_db",
    [string]$DbUser = "lawgate_user",
    [string]$DbPassword = "lawgate_dev_password_change_in_production",
    [switch]$SkipConfirmation
)

Write-Host "================================================" -ForegroundColor Cyan
Write-Host "   Lawgate Database Recreation Script" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""

# Check if PostgreSQL is running
Write-Host "Checking PostgreSQL connection..." -ForegroundColor Yellow
$env:PGPASSWORD = $DbPassword
$pgTest = & pg_isready -h $DbHost -U $DbUser 2>&1

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ PostgreSQL is not running or not accessible!" -ForegroundColor Red
    Write-Host "   Starting Docker container..." -ForegroundColor Yellow
    
    Set-Location ..
    docker-compose up -d postgres
    
    Write-Host "   Waiting for PostgreSQL to be ready..." -ForegroundColor Yellow
    Start-Sleep -Seconds 10
    
    $pgTest = & pg_isready -h $DbHost -U $DbUser 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Failed to start PostgreSQL. Please check Docker." -ForegroundColor Red
        exit 1
    }
}

Write-Host "✅ PostgreSQL is running" -ForegroundColor Green
Write-Host ""

# Confirmation
if (-not $SkipConfirmation) {
    Write-Host "⚠️  WARNING: This will DELETE all data in '$DbName' database!" -ForegroundColor Red
    $confirmation = Read-Host "Are you sure you want to continue? (yes/no)"
    if ($confirmation -ne "yes") {
        Write-Host "Operation cancelled." -ForegroundColor Yellow
        exit 0
    }
}

Write-Host ""
Write-Host "Step 1: Dropping existing database..." -ForegroundColor Yellow
$env:PGPASSWORD = $DbPassword
& psql -h $DbHost -U $DbUser -d postgres -c "DROP DATABASE IF EXISTS $DbName;" 2>&1 | Out-Null
Write-Host "✅ Database dropped" -ForegroundColor Green

Write-Host ""
Write-Host "Step 2: Creating fresh database..." -ForegroundColor Yellow
& psql -h $DbHost -U $DbUser -d postgres -c "CREATE DATABASE $DbName;" 2>&1 | Out-Null
Write-Host "✅ Database created" -ForegroundColor Green

Write-Host ""
Write-Host "Step 3: Applying Entity Framework migrations..." -ForegroundColor Yellow
Set-Location ../backend
$migrationOutput = & dotnet ef database update 2>&1

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Migrations applied successfully" -ForegroundColor Green
} else {
    Write-Host "❌ Migration failed!" -ForegroundColor Red
    Write-Host $migrationOutput
    exit 1
}

Write-Host ""
Write-Host "Step 4: Seeding initial data..." -ForegroundColor Yellow
$seedOutput = & dotnet run --seed-data 2>&1

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Data seeded successfully" -ForegroundColor Green
} else {
    Write-Host "⚠️  Seeding warning (this may be normal if app runs)" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "================================================" -ForegroundColor Cyan
Write-Host "   ✅ Database Recreation Complete!" -ForegroundColor Green
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Connection Details:" -ForegroundColor Cyan
Write-Host "  Host: $DbHost" -ForegroundColor White
Write-Host "  Database: $DbName" -ForegroundColor White
Write-Host "  Username: $DbUser" -ForegroundColor White
Write-Host "  Password: $DbPassword" -ForegroundColor White
Write-Host ""
Write-Host "Default Users (Development):" -ForegroundColor Cyan
Write-Host "  Admin: admin@lawgate.com / Admin@123" -ForegroundColor White
Write-Host "  User: user@lawgate.com / User@123" -ForegroundColor White
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Cyan
Write-Host "  1. cd ../backend && dotnet run" -ForegroundColor White
Write-Host "  2. cd ../frontend && npm run dev" -ForegroundColor White
Write-Host ""
