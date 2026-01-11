# Simple GitHub Issues Creator
# Creates all 30 issues for the Legal Document Management System

Write-Host "Creating GitHub Issues..." -ForegroundColor Cyan
Write-Host ""

$issuesCreated = 0

# Issue 1
Write-Host "Creating issue 1/30..." -ForegroundColor Yellow
gh issue create `
  --title "Setup Clean Architecture Project Structure" `
  --body "Implement Clean Architecture (Onion Architecture) for the .NET backend`n`nTasks:`n- Create LegalDocSystem.Domain project`n- Create LegalDocSystem.Application project`n- Create LegalDocSystem.Infrastructure project`n- Create LegalDocSystem.API project`n- Setup project dependencies`n- Configure dependency injection`n`nReference: backend/docs/README.md, initialDesign.md sections 4-5" `
  --label "enhancement,infrastructure,phase-1"
if ($LASTEXITCODE -eq 0) { $issuesCreated++ }

# Issue 2
Write-Host "Creating issue 2/30..." -ForegroundColor Yellow
gh issue create `
  --title "Implement Multi-Tenant Database Architecture" `
  --body "Design and implement multi-tenant database with strict data isolation`n`nTasks:`n- Create all entity models`n- Implement Row-Level Security using EF Core query filters`n- Add CompanyId to all tenant-specific entities`n- Create database indexes`n- Setup TenantMiddleware`n- Test data isolation between tenants`n`nReference: initialDesign.md section 5" `
  --label "database,security,phase-1"
if ($LASTEXITCODE -eq 0) { $issuesCreated++ }

# Issue 3
Write-Host "Creating issue 3/30..." -ForegroundColor Yellow
gh issue create `
  --title "Setup Azure Blob Storage Integration" `
  --body "Integrate Azure Blob Storage for document storage`n`nTasks:`n- Install Azure.Storage.Blobs NuGet package`n- Create IAzureBlobStorageService interface`n- Implement AzureBlobStorageService with SAS token generation`n- Configure hierarchical namespace`n- Implement chunked upload for large files`n- Add blob lifecycle management configuration`n`nReference: initialDesign.md section 4" `
  --label "infrastructure,azure,phase-1"
if ($LASTEXITCODE -eq 0) { $issuesCreated++ }

# Issue 4
Write-Host "Creating issue 4/30..." -ForegroundColor Yellow
gh issue create `
  --title "Implement Company Entity and Management" `
  --body "Create Company entity with complete business logic`n`nTasks:`n- Create Company entity with GST number validation`n- Add Address value object`n- Implement soft delete functionality`n- Create CompanyRepository with multi-tenant filters`n- Add CompaniesController with CRUD endpoints`n- Create CompanyDto for API responses`n`nReference: initialDesign.md section 5" `
  --label "domain,phase-1"
if ($LASTEXITCODE -eq 0) { $issuesCreated++ }

# Issue 5
Write-Host "Creating issue 5/30..." -ForegroundColor Yellow
gh issue create `
  --title "Implement User Management with Company Association" `
  --body "Create User entity with company association and authentication`n`nTasks:`n- Create User entity with company association`n- Implement BCrypt password hashing`n- Create UserRepository with tenant filtering`n- Add UsersController with user management endpoints`n- Implement user invitation flow`n- Add email uniqueness validation`n`nReference: initialDesign.md section 5" `
  --label "domain,auth,phase-1"
if ($LASTEXITCODE -eq 0) { $issuesCreated++ }

# Issue 6
Write-Host "Creating issue 6/30..." -ForegroundColor Yellow
gh issue create `
  --title "Implement Project (Contract/Case) Entity" `
  --body "Create Project entity representing legal cases/contracts`n`nTasks:`n- Create Project entity with status tracking`n- Add ProjectStatus enum`n- Create ProjectRepository with tenant filtering`n- Implement project closure logic`n- Add ProjectsController with CRUD endpoints`n- Support multiple companies as co-parties`n`nReference: initialDesign.md section 5" `
  --label "domain,phase-1"
if ($LASTEXITCODE -eq 0) { $issuesCreated++ }

# Issue 7
Write-Host "Creating issue 7/30..." -ForegroundColor Yellow
gh issue create `
  --title "Implement Document Entity with Versioning" `
  --body "Create Document entity with version control`n`nTasks:`n- Create Document entity with versioning support`n- Implement document versioning (parent-child relationship)`n- Create DocumentRepository with tenant filtering`n- Add DocumentsController with upload/download endpoints`n- Implement soft delete`n- Generate SAS URLs for secure downloads`n`nReference: initialDesign.md section 5" `
  --label "domain,storage,phase-1"
if ($LASTEXITCODE -eq 0) { $issuesCreated++ }

# Issue 8
Write-Host "Creating issue 8/30..." -ForegroundColor Yellow
gh issue create `
  --title "Implement JWT Authentication System" `
  --body "Setup JWT-based authentication with refresh tokens`n`nTasks:`n- Configure JWT settings in appsettings`n- Create IAuthService interface`n- Implement AuthService with login/register methods`n- Generate access and refresh tokens`n- Add JWT middleware`n- Create AuthController`n- Include CompanyId and UserId in JWT claims`n`nReference: initialDesign.md section 7" `
  --label "security,auth,phase-1"
if ($LASTEXITCODE -eq 0) { $issuesCreated++ }

# Issue 9
Write-Host "Creating issue 9/30..." -ForegroundColor Yellow
gh issue create `
  --title "Implement Project-Level RBAC" `
  --body "Implement Role-Based Access Control at project level`n`nTasks:`n- Create ProjectPermission entity`n- Add ProjectRole enum (Admin, Editor, Viewer)`n- Create IPermissionService interface`n- Implement permission checking logic`n- Add PermissionMiddleware for API endpoints`n- Create endpoints to grant/revoke permissions`n- Add project member invitation flow`n`nReference: initialDesign.md section 7" `
  --label "security,rbac,phase-1"
if ($LASTEXITCODE -eq 0) { $issuesCreated++ }

# Issue 10
Write-Host "Creating issue 10/30..." -ForegroundColor Yellow
gh issue create `
  --title "Implement Comprehensive Audit Logging" `
  --body "Create audit trail for all critical operations`n`nTasks:`n- Create AuditLog entity`n- Add AuditAction enum`n- Create IAuditService interface`n- Implement automatic audit logging middleware`n- Log all document and permission operations`n- Include IP address and User-Agent`n- Create audit trail query endpoints`n- Ensure 3-year retention compliance`n`nReference: initialDesign.md sections 5 and 7" `
  --label "security,compliance,phase-1"
if ($LASTEXITCODE -eq 0) { $issuesCreated++ }

# Issue 11
Write-Host "Creating issue 11/30..." -ForegroundColor Yellow
gh issue create `
  --title "Implement Document Tagging System" `
  --body "Create flexible tagging system for document organization`n`nTasks:`n- Create Tag entity`n- Create DocumentTag entity (many-to-many relationship)`n- Add standard tags (Contract, Evidence, Court Filing, Correspondence)`n- Allow custom tags per company`n- Create TagsController with CRUD endpoints`n- Add tag filtering to document queries`n- Implement tag autocomplete`n`nReference: initialDesign.md section 2.6" `
  --label "feature,phase-1"
if ($LASTEXITCODE -eq 0) { $issuesCreated++ }

# Issue 12
Write-Host "Creating issue 12/30..." -ForegroundColor Yellow
gh issue create `
  --title "Implement Secure Document Sharing" `
  --body "Create secure document sharing with external parties`n`nTasks:`n- Create DocumentShare entity`n- Generate unique share links with UUID`n- Implement link expiration logic`n- Add optional password protection`n- Track access count and limits`n- Log all external accesses in audit trail`n- Create share revocation endpoint`n`nReference: initialDesign.md sections 2.4 and 5" `
  --label "feature,security,phase-1"
if ($LASTEXITCODE -eq 0) { $issuesCreated++ }

# Issue 13
Write-Host "Creating issue 13/30..." -ForegroundColor Yellow
gh issue create `
  --title "Implement Document Version Control" `
  --body "Enable users to upload new versions of documents`n`nTasks:`n- Link new versions to parent document`n- Increment version number automatically`n- Create endpoint to list all versions`n- Allow downloading specific versions`n- Display version history in UI`n- Maintain all versions (never delete old versions)`n`nReference: initialDesign.md section 2.3" `
  --label "feature,phase-1"
if ($LASTEXITCODE -eq 0) { $issuesCreated++ }

# Issue 14
Write-Host "Creating issue 14/30..." -ForegroundColor Yellow
gh issue create `
  --title "Implement License/Subscription Entity" `
  --body "Create subscription management system`n`nTasks:`n- Create License entity with tier support`n- Add subscription tiers (Basic, Professional, Enterprise, Custom)`n- Implement license validation logic`n- Check storage limits before document upload`n- Check contract limits before project creation`n- Create license upgrade/downgrade flow`n- Add license expiration checking`n`nReference: initialDesign.md section 5" `
  --label "business,phase-1"
if ($LASTEXITCODE -eq 0) { $issuesCreated++ }

# Issue 15
Write-Host "Creating issue 15/30..." -ForegroundColor Yellow
gh issue create `
  --title "Integrate Razorpay Payment Gateway" `
  --body "Integrate Razorpay for subscription payments`n`nTasks:`n- Install Razorpay SDK`n- Create IRazorpayService interface`n- Implement subscription creation flow`n- Handle payment webhooks`n- Create invoice generation logic`n- Add payment history endpoints`n- Handle subscription renewal`n`nReference: initialDesign.md section 4" `
  --label "integration,payment,phase-1"
if ($LASTEXITCODE -eq 0) { $issuesCreated++ }

# Issue 16
Write-Host "Creating issue 16/30..." -ForegroundColor Yellow
gh issue create `
  --title "Integrate Azure Cognitive Search" `
  --body "Implement full-text search across documents`n`nTasks:`n- Setup Azure Cognitive Search resource`n- Create search indexes with tenant isolation`n- Implement document indexing on upload`n- Add OCR for scanned PDF documents`n- Create SearchController with query endpoints`n- Support filters (date range, tags, document type)`n- Ensure strict tenant isolation in search results`n`nReference: initialDesign.md section 2.7" `
  --label "feature,phase-2,search"
if ($LASTEXITCODE -eq 0) { $issuesCreated++ }

# Issue 17
Write-Host "Creating issue 17/30..." -ForegroundColor Yellow
gh issue create `
  --title "Implement AI-Powered Document Processing" `
  --body "Use Azure OpenAI/Document Intelligence for automation`n`nTasks:`n- Integrate Azure Document Intelligence`n- Implement PDF bundle auto-splitting`n- Create AI-based document classification`n- Auto-generate tags based on content`n- Extract key metadata (dates, parties, case numbers)`n- Generate document summaries`n- Add confidence scores for AI predictions`n`nReference: initialDesign.md section 2.8" `
  --label "feature,phase-2,ai"
if ($LASTEXITCODE -eq 0) { $issuesCreated++ }

# Issue 18
Write-Host "Creating issue 18/30..." -ForegroundColor Yellow
gh issue create `
  --title "Implement Storage Lifecycle Management" `
  --body "Automate storage tier management for cost optimization`n`nTasks:`n- Create Azure Blob lifecycle policies`n- Move closed projects to Cool tier after 3 months`n- Implement project archival logic`n- Add auto-deletion after subscription ends`n- Create storage usage reporting`n- Alert when approaching storage limits`n`nReference: initialDesign.md section 2.10" `
  --label "feature,phase-2,optimization"
if ($LASTEXITCODE -eq 0) { $issuesCreated++ }

# Issue 19
Write-Host "Creating issue 19/30..." -ForegroundColor Yellow
gh issue create `
  --title "Create Company and User Management UI" `
  --body "Build company and user management interfaces`n`nTasks:`n- Create company registration form`n- Build user invitation interface`n- Add user list/edit/deactivate pages`n- Implement company profile page (GST, address)`n- Create user profile settings`n- Add password change functionality`n`nReference: frontend/docs/README.md" `
  --label "frontend,phase-1"
if ($LASTEXITCODE -eq 0) { $issuesCreated++ }

# Issue 20
Write-Host "Creating issue 20/30..." -ForegroundColor Yellow
gh issue create `
  --title "Create Project Management UI" `
  --body "Build project/contract management interfaces`n`nTasks:`n- Create project list view with filters`n- Build project creation form`n- Add project detail page`n- Implement project status updates`n- Create co-party management interface`n- Add project member management`n- Build permission assignment UI`n`nReference: initialDesign.md section 2" `
  --label "frontend,phase-1"
if ($LASTEXITCODE -eq 0) { $issuesCreated++ }

# Issue 21
Write-Host "Creating issue 21/30..." -ForegroundColor Yellow
gh issue create `
  --title "Create Document Management UI" `
  --body "Build document upload, viewing, and management interfaces`n`nTasks:`n- Create document upload with drag-and-drop`n- Build document list with thumbnails`n- Add document viewer (PDF preview)`n- Implement document download`n- Create version history view`n- Add tag management UI`n- Build document sharing interface`n`nReference: initialDesign.md section 2.3" `
  --label "frontend,phase-1"
if ($LASTEXITCODE -eq 0) { $issuesCreated++ }

# Issue 22
Write-Host "Creating issue 22/30..." -ForegroundColor Yellow
gh issue create `
  --title "Create Subscription and Billing UI" `
  --body "Build subscription management and payment interfaces`n`nTasks:`n- Create subscription plan selection page`n- Build Razorpay payment integration`n- Add payment history view`n- Display current plan and usage`n- Implement upgrade/downgrade flow`n- Add invoice download`n- Show storage and contract usage meters`n`nReference: initialDesign.md section 5.1" `
  --label "frontend,phase-1"
if ($LASTEXITCODE -eq 0) { $issuesCreated++ }

# Issue 23
Write-Host "Creating issue 23/30..." -ForegroundColor Yellow
gh issue create `
  --title "Setup Unit Test Framework" `
  --body "Implement comprehensive unit testing`n`nTasks:`n- Setup xUnit for backend tests`n- Create test fixtures and mocks`n- Write unit tests for domain entities`n- Test service layer business logic`n- Test permission checking logic`n- Achieve 80%+ code coverage`n- Add tests to CI/CD pipeline`n`nReference: IMPLEMENTATION-CHECKLIST.md Phase 5" `
  --label "testing,quality"
if ($LASTEXITCODE -eq 0) { $issuesCreated++ }

# Issue 24
Write-Host "Creating issue 24/30..." -ForegroundColor Yellow
gh issue create `
  --title "Implement Integration Tests" `
  --body "Create integration tests for critical flows`n`nTasks:`n- Setup Testcontainers for PostgreSQL`n- Test API endpoints end-to-end`n- Test multi-tenant data isolation`n- Test document upload/download flow`n- Test authentication and authorization`n- Test Azure Blob Storage integration`n- Verify audit logging`n`nReference: IMPLEMENTATION-CHECKLIST.md Phase 5" `
  --label "testing,quality"
if ($LASTEXITCODE -eq 0) { $issuesCreated++ }

# Issue 25
Write-Host "Creating issue 25/30..." -ForegroundColor Yellow
gh issue create `
  --title "Security and Penetration Testing" `
  --body "Conduct security audits and penetration testing`n`nTasks:`n- Test tenant isolation`n- Test authentication bypass attempts`n- Verify CSRF protection`n- Test rate limiting effectiveness`n- Check for SQL injection vulnerabilities`n- Test XSS protection`n- Verify secure headers`n- Run OWASP dependency check`n`nReference: initialDesign.md section 7" `
  --label "security,testing"
if ($LASTEXITCODE -eq 0) { $issuesCreated++ }

# Issue 26
Write-Host "Creating issue 26/30..." -ForegroundColor Yellow
gh issue create `
  --title "Setup Azure Infrastructure" `
  --body "Create and configure all Azure resources`n`nTasks:`n- Create resource group`n- Setup Azure Database for PostgreSQL`n- Configure Azure Blob Storage`n- Setup Azure Cache for Redis`n- Create App Service Plan and Web App`n- Configure Azure Key Vault`n- Setup Application Insights`n- Configure managed identities`n`nReference: docs/azure-deployment.md" `
  --label "devops,azure"
if ($LASTEXITCODE -eq 0) { $issuesCreated++ }

# Issue 27
Write-Host "Creating issue 27/30..." -ForegroundColor Yellow
gh issue create `
  --title "Implement CI/CD Pipelines" `
  --body "Setup automated deployment pipelines`n`nTasks:`n- Create GitHub Actions workflows`n- Setup backend build and test pipeline`n- Setup frontend build pipeline`n- Configure automated migrations`n- Add staging environment`n- Implement blue-green deployment`n- Add smoke tests post-deployment`n`nReference: docs/azure-deployment.md section 10" `
  --label "devops,automation"
if ($LASTEXITCODE -eq 0) { $issuesCreated++ }

# Issue 28
Write-Host "Creating issue 28/30..." -ForegroundColor Yellow
gh issue create `
  --title "Setup Application Monitoring" `
  --body "Implement comprehensive monitoring and alerting`n`nTasks:`n- Configure Application Insights`n- Setup custom metrics and dashboards`n- Create alerts for errors and performance`n- Add health check endpoints`n- Implement log aggregation`n- Setup uptime monitoring`n- Create performance baselines`n`nReference: initialDesign.md section 10" `
  --label "monitoring,operations"
if ($LASTEXITCODE -eq 0) { $issuesCreated++ }

# Issue 29
Write-Host "Creating issue 29/30..." -ForegroundColor Yellow
gh issue create `
  --title "Create Operational Runbooks" `
  --body "Document common operational procedures`n`nTasks:`n- Database backup and restore procedures`n- Disaster recovery runbook`n- Scaling procedures`n- Incident response guide`n- User onboarding checklist`n- Common troubleshooting guide`n- Security incident response`n`nReference: docs/azure-deployment.md" `
  --label "documentation,operations"
if ($LASTEXITCODE -eq 0) { $issuesCreated++ }

# Issue 30
Write-Host "Creating issue 30/30..." -ForegroundColor Yellow
gh issue create `
  --title "Initialize Frontend and Backend Projects" `
  --body "Initialize the actual React and .NET projects`n`nTasks:`n- Run npm create vite in frontend folder`n- Choose React + TypeScript template`n- Install frontend dependencies`n- Run dotnet new webapi in backend folder`n- Install backend NuGet packages`n- Configure both projects according to documentation`n`nReference: IMPLEMENTATION-CHECKLIST.md Phase 2-3" `
  --label "infrastructure,phase-1"
if ($LASTEXITCODE -eq 0) { $issuesCreated++ }

Write-Host ""
Write-Host "Successfully created $issuesCreated out of 30 issues!" -ForegroundColor Green
Write-Host ""
Write-Host "View all issues: gh issue list" -ForegroundColor Cyan
