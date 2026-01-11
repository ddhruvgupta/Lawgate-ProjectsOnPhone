# Script to create GitHub Project Board and organize issues
# Run this after creating issues with create-github-issues.ps1

Write-Host "Setting up GitHub Project Kanban Board..." -ForegroundColor Cyan
Write-Host ""

# Check if gh is authenticated
$authStatus = gh auth status 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Not authenticated with GitHub CLI" -ForegroundColor Red
    Write-Host "Please run: gh auth login" -ForegroundColor Yellow
    exit 1
}

Write-Host "✓ Authenticated with GitHub" -ForegroundColor Green

# Get the repository owner and name
$repoInfo = gh repo view --json owner,name | ConvertFrom-Json
$owner = $repoInfo.owner.login
$repo = $repoInfo.name

Write-Host "✓ Repository: $owner/$repo" -ForegroundColor Green
Write-Host ""

# Create the project
Write-Host "Creating Project Board..." -ForegroundColor Yellow
$projectOutput = gh project create --owner $owner --title "Legal Doc System Development" --format json 2>&1

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Failed to create project" -ForegroundColor Red
    Write-Host $projectOutput -ForegroundColor Red
    Write-Host ""
    Write-Host "You may need to create the project manually:" -ForegroundColor Yellow
    Write-Host "1. Go to: https://github.com/$owner/$repo/projects" -ForegroundColor White
    Write-Host "2. Click 'New project' -> 'Board' template" -ForegroundColor White
    Write-Host "3. Name it: 'Legal Doc System Development'" -ForegroundColor White
    exit 1
}

$project = $projectOutput | ConvertFrom-Json
$projectNumber = $project.number
$projectUrl = $project.url

Write-Host "✓ Created Project #$projectNumber" -ForegroundColor Green
Write-Host "  URL: $projectUrl" -ForegroundColor White
Write-Host ""

# Get all issues
Write-Host "Fetching all issues..." -ForegroundColor Yellow
$issues = gh issue list --limit 100 --json number,title,labels --state open | ConvertFrom-Json

if ($issues.Count -eq 0) {
    Write-Host "WARNING: No issues found. Please run create-github-issues.ps1 first" -ForegroundColor Yellow
    exit 1
}

Write-Host "✓ Found $($issues.Count) issues" -ForegroundColor Green
Write-Host ""

# Add issues to project and organize by phase
Write-Host "Adding issues to project board..." -ForegroundColor Yellow

$phase1Count = 0
$phase2Count = 0

foreach ($issue in $issues) {
    $issueNumber = $issue.number
    $issueTitle = $issue.title
    $labels = $issue.labels | ForEach-Object { $_.name }
    
    # Add issue to project
    Write-Host "  Adding #$issueNumber`: $issueTitle" -ForegroundColor White
    
    $addResult = gh project item-add $projectNumber --owner $owner --url "https://github.com/$owner/$repo/issues/$issueNumber" 2>&1
    
    if ($LASTEXITCODE -eq 0) {
        # Categorize by phase
        if ($labels -contains "phase-1") {
            $phase1Count++
        } elseif ($labels -contains "phase-2") {
            $phase2Count++
        }
    } else {
        Write-Host "    ⚠ Failed to add issue #$issueNumber" -ForegroundColor Yellow
    }
    
    Start-Sleep -Milliseconds 200  # Rate limiting
}

Write-Host ""
Write-Host "✓ Successfully added issues to project board!" -ForegroundColor Green
Write-Host ""

# Summary
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "                    SUMMARY" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host "Project Board Created:" -ForegroundColor White
Write-Host "  📊 Name: Legal Doc System Development" -ForegroundColor White
Write-Host "  🔗 URL: $projectUrl" -ForegroundColor White
Write-Host ""
Write-Host "Issues Added:" -ForegroundColor White
Write-Host "  📋 Total Issues: $($issues.Count)" -ForegroundColor White
Write-Host "  🎯 Phase 1 (MVP): $phase1Count issues" -ForegroundColor Green
Write-Host "  🚀 Phase 2 (Advanced): $phase2Count issues" -ForegroundColor Yellow
Write-Host ""
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

Write-Host "Next Steps:" -ForegroundColor Cyan
Write-Host "1. Visit your project board: $projectUrl" -ForegroundColor White
Write-Host "2. Customize column names if needed (default columns are created)" -ForegroundColor White
Write-Host "3. Move Phase 1 issues to 'To Do' column" -ForegroundColor White
Write-Host "4. Move Phase 2 issues to 'Backlog' column" -ForegroundColor White
Write-Host "5. Start working on the first issue!" -ForegroundColor White
Write-Host ""

Write-Host "Quick Commands:" -ForegroundColor Cyan
Write-Host "  View all issues:  gh issue list" -ForegroundColor White
Write-Host "  View project:     gh project view $projectNumber --owner $owner" -ForegroundColor White
Write-Host "  Filter Phase 1:   gh issue list --label phase-1" -ForegroundColor White
Write-Host "  Filter Phase 2:   gh issue list --label phase-2" -ForegroundColor White
Write-Host ""

Write-Host "🎉 Your project board is ready! Happy coding!" -ForegroundColor Green
