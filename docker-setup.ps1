# Docker Setup and Test Script
# This script sets up and tests the complete Docker environment

Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "     Legal Document Management System - Docker Setup" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# Function to check if Docker is running
function Test-DockerRunning {
    try {
        $null = docker info 2>&1
        return $LASTEXITCODE -eq 0
    } catch {
        return $false
    }
}

# Function to check if a container is running
function Test-ContainerRunning {
    param([string]$containerName)
    $status = docker ps --filter "name=$containerName" --format "{{.Status}}" 2>&1
    return $status -and $status -notlike "*error*"
}

# Step 1: Check Docker
Write-Host "[Step 1/6] Checking Docker Desktop..." -ForegroundColor Yellow
if (-not (Test-DockerRunning)) {
    Write-Host "❌ Docker is not running!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Please start Docker Desktop and run this script again." -ForegroundColor Yellow
    Write-Host "Download Docker Desktop: https://www.docker.com/products/docker-desktop" -ForegroundColor Cyan
    exit 1
}
Write-Host "✅ Docker is running" -ForegroundColor Green
Write-Host ""

# Step 2: Clean up existing containers (optional)
Write-Host "[Step 2/6] Cleaning up existing containers..." -ForegroundColor Yellow
$cleanup = Read-Host "Do you want to remove existing containers? (y/N)"
if ($cleanup -eq "y" -or $cleanup -eq "Y") {
    Write-Host "Stopping and removing containers..." -ForegroundColor White
    docker-compose down -v
    Write-Host "✅ Cleanup complete" -ForegroundColor Green
} else {
    Write-Host "⏩ Skipping cleanup" -ForegroundColor Gray
}
Write-Host ""

# Step 3: Build and start PostgreSQL only
Write-Host "[Step 3/6] Starting PostgreSQL database..." -ForegroundColor Yellow
Write-Host "Building and starting postgres container..." -ForegroundColor White
docker-compose up -d postgres

# Wait for PostgreSQL to be healthy
Write-Host "Waiting for PostgreSQL to be ready..." -ForegroundColor White
$maxAttempts = 30
$attempt = 0
$isHealthy = $false

while ($attempt -lt $maxAttempts -and -not $isHealthy) {
    $attempt++
    Start-Sleep -Seconds 2
    
    $health = docker inspect --format='{{.State.Health.Status}}' lawgate-postgres 2>&1
    if ($health -eq "healthy") {
        $isHealthy = $true
    } else {
        Write-Host "  Attempt $attempt/$maxAttempts - Status: $health" -ForegroundColor Gray
    }
}

if ($isHealthy) {
    Write-Host "✅ PostgreSQL is healthy and ready!" -ForegroundColor Green
} else {
    Write-Host "❌ PostgreSQL failed to start properly" -ForegroundColor Red
    Write-Host "Check logs with: docker logs lawgate-postgres" -ForegroundColor Yellow
    exit 1
}
Write-Host ""

# Step 4: Test database connection
Write-Host "[Step 4/6] Testing database connection..." -ForegroundColor Yellow
$dbTest = docker exec lawgate-postgres psql -U lawgate_user -d lawgate_db -c "SELECT version();" 2>&1
if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Database connection successful!" -ForegroundColor Green
    Write-Host "Database version:" -ForegroundColor Gray
    Write-Host $dbTest -ForegroundColor Gray
} else {
    Write-Host "❌ Database connection failed" -ForegroundColor Red
    Write-Host $dbTest -ForegroundColor Red
    exit 1
}
Write-Host ""

# Step 5: Display connection info
Write-Host "[Step 5/6] Database connection information:" -ForegroundColor Yellow
Write-Host ""
Write-Host "  Host:     localhost" -ForegroundColor White
Write-Host "  Port:     5432" -ForegroundColor White
Write-Host "  Database: lawgate_db" -ForegroundColor White
Write-Host "  Username: lawgate_user" -ForegroundColor White
Write-Host "  Password: lawgate_dev_password_change_in_production" -ForegroundColor White
Write-Host ""
Write-Host "Connection String for .NET:" -ForegroundColor Cyan
Write-Host "Host=localhost;Database=lawgate_db;Username=lawgate_user;Password=lawgate_dev_password_change_in_production" -ForegroundColor Gray
Write-Host ""

# Step 6: Ask about backend/frontend
Write-Host "[Step 6/6] Additional services:" -ForegroundColor Yellow
Write-Host ""
Write-Host "PostgreSQL is now running!" -ForegroundColor Green
Write-Host ""
$startAll = Read-Host "Do you want to start backend and frontend services too? (y/N)"

if ($startAll -eq "y" -or $startAll -eq "Y") {
    Write-Host ""
    Write-Host "⚠️  NOTE: Backend and frontend projects need to be initialized first!" -ForegroundColor Yellow
    Write-Host "   Run these commands to initialize:" -ForegroundColor White
    Write-Host "   1. cd frontend && npm create vite@latest . -- --template react-ts" -ForegroundColor Cyan
    Write-Host "   2. cd backend && dotnet new webapi" -ForegroundColor Cyan
    Write-Host ""
    $proceed = Read-Host "Have you initialized the projects? (y/N)"
    
    if ($proceed -eq "y" -or $proceed -eq "Y") {
        Write-Host ""
        Write-Host "Starting all services..." -ForegroundColor Yellow
        docker-compose up -d
        
        Write-Host ""
        Write-Host "✅ All services are starting!" -ForegroundColor Green
        Write-Host ""
        Write-Host "Services:" -ForegroundColor Cyan
        Write-Host "  🗄️  Database:  http://localhost:5432" -ForegroundColor White
        Write-Host "  🔙 Backend:   http://localhost:5000" -ForegroundColor White
        Write-Host "  🎨 Frontend:  http://localhost:3000" -ForegroundColor White
        Write-Host ""
        Write-Host "View logs: docker-compose logs -f" -ForegroundColor Gray
    } else {
        Write-Host ""
        Write-Host "ℹ️  Only PostgreSQL is running." -ForegroundColor Cyan
        Write-Host "   Start all services later with: docker-compose up -d" -ForegroundColor Gray
    }
} else {
    Write-Host ""
    Write-Host "ℹ️  Only PostgreSQL is running." -ForegroundColor Cyan
    Write-Host "   Start all services later with: docker-compose up -d" -ForegroundColor Gray
}

Write-Host ""
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "                  DOCKER SETUP COMPLETE!" -ForegroundColor Green
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

Write-Host "Useful Docker Commands:" -ForegroundColor Cyan
Write-Host "  View running containers:  docker ps" -ForegroundColor White
Write-Host "  View all containers:      docker ps -a" -ForegroundColor White
Write-Host "  View logs:                docker-compose logs -f" -ForegroundColor White
Write-Host "  Stop all services:        docker-compose stop" -ForegroundColor White
Write-Host "  Start all services:       docker-compose start" -ForegroundColor White
Write-Host "  Remove all services:      docker-compose down" -ForegroundColor White
Write-Host "  Remove with volumes:      docker-compose down -v" -ForegroundColor White
Write-Host ""
Write-Host "Database Commands:" -ForegroundColor Cyan
Write-Host "  Connect to database:      docker exec -it lawgate-postgres psql -U lawgate_user -d lawgate_db" -ForegroundColor White
Write-Host "  View database logs:       docker logs lawgate-postgres" -ForegroundColor White
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "  1. Initialize backend: cd backend && dotnet new webapi" -ForegroundColor White
Write-Host "  2. Initialize frontend: cd frontend && npm create vite@latest . -- --template react-ts" -ForegroundColor White
Write-Host "  3. Run database setup: .\database\recreate-database.ps1" -ForegroundColor White
Write-Host ""
