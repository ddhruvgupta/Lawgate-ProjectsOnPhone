# Script to view project status and organization
# Shows breakdown of issues by epic, phase, and labels

Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "          Project Status - Quick Overview" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# Get repository info
$repoInfo = gh repo view --json owner,name,url | ConvertFrom-Json
$owner = $repoInfo.owner.login
$repo = $repoInfo.name
$repoUrl = $repoInfo.url

Write-Host "📂 Repository: $owner/$repo" -ForegroundColor White
Write-Host "🔗 URL: $repoUrl" -ForegroundColor White
Write-Host ""

# Get all issues
$allIssues = gh issue list --limit 100 --json number,title,labels,state --state all | ConvertFrom-Json
$openIssues = $allIssues | Where-Object { $_.state -eq "OPEN" }
$closedIssues = $allIssues | Where-Object { $_.state -eq "CLOSED" }

Write-Host "📊 Issue Statistics:" -ForegroundColor Yellow
Write-Host "   Total Issues: $($allIssues.Count)" -ForegroundColor White
Write-Host "   Open:         $($openIssues.Count)" -ForegroundColor Green
Write-Host "   Closed:       $($closedIssues.Count)" -ForegroundColor Gray
Write-Host ""

# Count by phase
$phase1 = $openIssues | Where-Object { ($_.labels | ForEach-Object { $_.name }) -contains "phase-1" }
$phase2 = $openIssues | Where-Object { ($_.labels | ForEach-Object { $_.name }) -contains "phase-2" }

Write-Host "🎯 By Phase:" -ForegroundColor Yellow
Write-Host "   Phase 1 (MVP):      $($phase1.Count) issues" -ForegroundColor Green
Write-Host "   Phase 2 (Advanced): $($phase2.Count) issues" -ForegroundColor Cyan
Write-Host ""

# Count by category
$categories = @{
    "infrastructure" = "🏗️  Infrastructure"
    "domain" = "📦 Domain Entities"
    "security" = "🔐 Security"
    "feature" = "📄 Features"
    "frontend" = "🎨 Frontend"
    "testing" = "✅ Testing"
    "devops" = "☁️  DevOps"
    "monitoring" = "📊 Monitoring"
}

Write-Host "📋 By Category:" -ForegroundColor Yellow
foreach ($label in $categories.Keys | Sort-Object) {
    $count = ($openIssues | Where-Object { 
        ($_.labels | ForEach-Object { $_.name }) -contains $label 
    }).Count
    
    if ($count -gt 0) {
        $icon = $categories[$label]
        Write-Host "   $icon`: $count issues" -ForegroundColor White
    }
}
Write-Host ""

# Show recent activity
Write-Host "📅 Recent Activity:" -ForegroundColor Yellow
$recentIssues = $openIssues | Select-Object -First 5
foreach ($issue in $recentIssues) {
    $labels = ($issue.labels | ForEach-Object { $_.name }) -join ", "
    Write-Host "   #$($issue.number) - $($issue.title)" -ForegroundColor White
    Write-Host "        [$labels]" -ForegroundColor Gray
}
Write-Host ""

# Get project info if exists
Write-Host "📊 Projects:" -ForegroundColor Yellow
$projects = gh project list --owner $owner --format json 2>&1 | ConvertFrom-Json
if ($projects -and $projects.Count -gt 0) {
    foreach ($proj in $projects) {
        Write-Host "   📋 $($proj.title) (#$($proj.number))" -ForegroundColor White
        Write-Host "      $($proj.url)" -ForegroundColor Gray
    }
} else {
    Write-Host "   No projects found. Run setup-kanban-board.ps1 to create one." -ForegroundColor Gray
}
Write-Host ""

Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

Write-Host "Quick Commands:" -ForegroundColor Cyan
Write-Host "  View Phase 1 issues:  gh issue list --label phase-1" -ForegroundColor White
Write-Host "  View Phase 2 issues:  gh issue list --label phase-2" -ForegroundColor White
Write-Host "  View by epic:         gh issue list --label infrastructure" -ForegroundColor White
Write-Host "  Create new issue:     gh issue create" -ForegroundColor White
Write-Host "  View issue details:   gh issue view <number>" -ForegroundColor White
Write-Host ""
