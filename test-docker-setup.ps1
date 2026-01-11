# Docker Setup and Test Script
# This script sets up and tests the complete Docker environment

Write-Host "================================================================" -ForegroundColor Cyan
Write-Host "  Legal Document Management System - Docker Setup" -ForegroundColor Cyan
Write-Host "================================================================" -ForegroundColor Cyan
Write-Host ""

# Function to check if Docker is running
function Test-DockerRunning {
    try {
        $null = docker info 2>&1
        return $LASTEXITCODE -eq 0
    }
    catch {
        return $false
    }
}

# Step 1: Check Docker
Write-Host "[Step 1/5] Checking Docker Desktop..." -ForegroundColor Yellow
if (-not (Test-DockerRunning)) {
    Write-Host "ERROR: Docker is not running!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Please start Docker Desktop and run this script again." -ForegroundColor Yellow
    Write-Host "Download: https://www.docker.com/products/docker-desktop" -ForegroundColor Cyan
    exit 1
}
Write-Host "SUCCESS: Docker is running" -ForegroundColor Green
Write-Host ""

# Step 2: Clean up existing containers
Write-Host "[Step 2/5] Cleaning up existing containers..." -ForegroundColor Yellow
Write-Host "Stopping and removing old containers..." -ForegroundColor White
docker-compose down 2>&1 | Out-Null
Write-Host "SUCCESS: Cleanup complete" -ForegroundColor Green
Write-Host ""

# Step 3: Start PostgreSQL
Write-Host "[Step 3/5] Starting PostgreSQL database..." -ForegroundColor Yellow
Write-Host "Building and starting postgres container..." -ForegroundColor White
docker-compose up -d postgres

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Failed to start PostgreSQL" -ForegroundColor Red
    exit 1
}

# Wait for PostgreSQL to be healthy
Write-Host "Waiting for PostgreSQL to be ready..." -ForegroundColor White
$maxAttempts = 30
$attempt = 0
$isHealthy = $false

while ($attempt -lt $maxAttempts) {
    $attempt++
    Start-Sleep -Seconds 2
    
    $health = docker inspect --format='{{.State.Health.Status}}' lawgate-postgres 2>&1
    if ($health -eq "healthy") {
        $isHealthy = $true
        break
    }
    Write-Host "  Attempt $attempt/$maxAttempts - Status: $health" -ForegroundColor Gray
}

if ($isHealthy) {
    Write-Host "SUCCESS: PostgreSQL is healthy and ready!" -ForegroundColor Green
}
else {
    Write-Host "ERROR: PostgreSQL failed to start" -ForegroundColor Red
    Write-Host "Check logs with: docker logs lawgate-postgres" -ForegroundColor Yellow
    exit 1
}
Write-Host ""

# Step 4: Test database connection
Write-Host "[Step 4/5] Testing database connection..." -ForegroundColor Yellow
$dbTest = docker exec lawgate-postgres psql -U lawgate_user -d lawgate_db -c "SELECT version();" 2>&1
if ($LASTEXITCODE -eq 0) {
    Write-Host "SUCCESS: Database connection works!" -ForegroundColor Green
}
else {
    Write-Host "ERROR: Database connection failed" -ForegroundColor Red
    Write-Host $dbTest -ForegroundColor Red
    exit 1
}
Write-Host ""

# Step 5: Display info
Write-Host "[Step 5/5] Setup complete!" -ForegroundColor Yellow
Write-Host ""
Write-Host "Database Connection Info:" -ForegroundColor Cyan
Write-Host "  Host:     localhost" -ForegroundColor White
Write-Host "  Port:     5432" -ForegroundColor White
Write-Host "  Database: lawgate_db" -ForegroundColor White
Write-Host "  Username: lawgate_user" -ForegroundColor White
Write-Host "  Password: lawgate_dev_password_change_in_production" -ForegroundColor White
Write-Host ""
Write-Host "Connection String:" -ForegroundColor Cyan
Write-Host "Host=localhost;Database=lawgate_db;Username=lawgate_user;Password=lawgate_dev_password_change_in_production" -ForegroundColor Gray
Write-Host ""

Write-Host "================================================================" -ForegroundColor Cyan
Write-Host "                 DOCKER SETUP COMPLETE!" -ForegroundColor Green
Write-Host "================================================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "Useful Commands:" -ForegroundColor Cyan
Write-Host "  View containers:     docker ps" -ForegroundColor White
Write-Host "  View logs:           docker logs lawgate-postgres" -ForegroundColor White
Write-Host "  Stop database:       docker-compose stop postgres" -ForegroundColor White
Write-Host "  Start database:      docker-compose start postgres" -ForegroundColor White
Write-Host "  Remove all:          docker-compose down -v" -ForegroundColor White
Write-Host ""
Write-Host "  Connect to DB:       docker exec -it lawgate-postgres psql -U lawgate_user -d lawgate_db" -ForegroundColor White
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "  1. Initialize backend:  cd backend && dotnet new webapi -n LegalDocSystem.API" -ForegroundColor White
Write-Host "  2. Initialize frontend: cd frontend && npm create vite@latest . -- --template react-ts" -ForegroundColor White
Write-Host "  3. Setup database:      .\database\recreate-database.ps1" -ForegroundColor White
Write-Host ""
