# Master Setup Script - Run All GitHub Setup Steps
# This script will:
# 1. Verify GitHub CLI authentication
# 2. Create all GitHub issues
# 3. Create project board
# 4. Add issues to board

Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "     Legal Document Management System - GitHub Setup" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# Step 1: Check GitHub CLI authentication
Write-Host "[Step 1/3] Checking GitHub CLI authentication..." -ForegroundColor Yellow
Write-Host ""

$authStatus = gh auth status 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Not authenticated with GitHub CLI" -ForegroundColor Red
    Write-Host ""
    Write-Host "Please authenticate first:" -ForegroundColor Yellow
    Write-Host "  gh auth login" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Then run this script again:" -ForegroundColor Yellow
    Write-Host "  .\setup-github-complete.ps1" -ForegroundColor Cyan
    exit 1
}

Write-Host "✓ Authenticated with GitHub CLI" -ForegroundColor Green
Write-Host ""

# Step 2: Create all issues
Write-Host "[Step 2/3] Creating GitHub issues..." -ForegroundColor Yellow
Write-Host ""

& ".\create-github-issues.ps1"

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "❌ Failed to create issues" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "✓ Issues created successfully" -ForegroundColor Green
Write-Host ""
Write-Host "Waiting 3 seconds before setting up project board..." -ForegroundColor White
Start-Sleep -Seconds 3

# Step 3: Setup project board
Write-Host "[Step 3/3] Setting up Kanban board..." -ForegroundColor Yellow
Write-Host ""

& ".\setup-kanban-board.ps1"

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "❌ Failed to setup project board" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "                  SETUP COMPLETE!" -ForegroundColor Green
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host "Your Legal Document Management System project is now set up on GitHub with:" -ForegroundColor White
Write-Host "  ✓ 30 detailed issues organized into 10 epics" -ForegroundColor Green
Write-Host "  ✓ Complete Kanban project board" -ForegroundColor Green
Write-Host "  ✓ All issues linked to the board" -ForegroundColor Green
Write-Host ""
Write-Host "Start developing by visiting your GitHub repository!" -ForegroundColor Cyan
Write-Host ""
