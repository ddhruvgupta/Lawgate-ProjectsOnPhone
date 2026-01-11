# Script to create all GitHub issues for Legal Document Management System
# Run this after: gh auth login

Write-Host "Creating GitHub Issues for Legal Document Management System..." -ForegroundColor Cyan
Write-Host ""

# Check if gh is authenticated
$authStatus = gh auth status 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Not authenticated with GitHub CLI" -ForegroundColor Red
    Write-Host "Please run: gh auth login" -ForegroundColor Yellow
    exit 1
}

Write-Host "✓ Authenticated with GitHub" -ForegroundColor Green
Write-Host ""

# Epic 1: Foundation & Infrastructure
Write-Host "Creating Epic 1: Foundation & Infrastructure..." -ForegroundColor Yellow

gh issue create --title "Setup Clean Architecture Project Structure" `
    --body @"
Implement Clean Architecture (Onion Architecture) for the .NET backend:

## Tasks
- [ ] Create LegalDocSystem.Domain project (Entities, Value Objects, Enums)
- [ ] Create LegalDocSystem.Application project (Services, DTOs, Interfaces)
- [ ] Create LegalDocSystem.Infrastructure project (Data, Storage, External Services)
- [ ] Create LegalDocSystem.API project (Controllers, Middleware)
- [ ] Setup project dependencies (Domain → Application → Infrastructure → API)
- [ ] Configure dependency injection in Program.cs

## Reference
- backend/docs/README.md
- initialDesign.md sections 4-5

## Epic
Foundation & Infrastructure
"@ `
    --label "enhancement,infrastructure,phase-1"

gh issue create --title "Implement Multi-Tenant Database Architecture" `
    --body @"
Design and implement multi-tenant database with strict data isolation:

## Tasks
- [ ] Create all entity models (Company, User, Project, Document, etc.)
- [ ] Implement Row-Level Security (RLS) using EF Core query filters
- [ ] Add CompanyId to all tenant-specific entities
- [ ] Create database indexes for multi-tenant queries
- [ ] Setup TenantMiddleware to extract CompanyId from JWT
- [ ] Test data isolation between tenants

## Security Critical
Absolute separation between companies required.

## Reference
- initialDesign.md section 5 (Data Model)

## Epic
Foundation & Infrastructure
"@ `
    --label "database,security,phase-1"

gh issue create --title "Setup Azure Blob Storage Integration" `
    --body @"
Integrate Azure Blob Storage for document storage:

## Tasks
- [ ] Install Azure.Storage.Blobs NuGet package
- [ ] Create IAzureBlobStorageService interface
- [ ] Implement AzureBlobStorageService with SAS token generation
- [ ] Configure hierarchical namespace: {company-id}/{project-id}/{document-id}/
- [ ] Implement chunked upload for large files (up to 500MB)
- [ ] Add blob lifecycle management configuration
- [ ] Setup connection in appsettings with Key Vault reference

## Reference
- initialDesign.md section 4

## Epic
Foundation & Infrastructure
"@ `
    --label "infrastructure,azure,phase-1"

# Epic 2: Core Domain Entities
Write-Host "Creating Epic 2: Core Domain Entities..." -ForegroundColor Yellow

gh issue create --title "Implement Company Entity & Management" `
    --body @"
Create Company entity with complete business logic:

## Tasks
- [ ] Create Company entity (Id, Name, GSTNumber, Address, IsActive)
- [ ] Add Address value object (Street, City, State, Pincode)
- [ ] Implement soft delete functionality
- [ ] Create CompanyRepository with multi-tenant filters
- [ ] Add CompaniesController with CRUD endpoints
- [ ] Add company registration validation (GST format)
- [ ] Create CompanyDto for API responses

## Acceptance Criteria
- GST number validation for India
- Audit logging for all operations
- Cannot view other company's data

## Reference
- initialDesign.md section 5 (Companies table)

## Epic
Core Domain Entities
"@ `
    --label "domain,phase-1"

gh issue create --title "Implement User Management with Company Association" `
    --body @"
Create User entity with company association and authentication:

## Tasks
- [ ] Create User entity (CompanyId, Email, PasswordHash, Name, Phone)
- [ ] Implement BCrypt password hashing
- [ ] Create UserRepository with tenant filtering
- [ ] Add UsersController with user management endpoints
- [ ] Implement user invitation flow
- [ ] Add email uniqueness validation
- [ ] Create user profile endpoints

## Reference
- initialDesign.md section 5 (Users table)

## Epic
Core Domain Entities
"@ `
    --label "domain,auth,phase-1"

gh issue create --title "Implement Project (Contract/Case) Entity" `
    --body @"
Create Project entity representing legal cases/contracts:

## Tasks
- [ ] Create Project entity (Name, Description, Status, ContractDocumentId)
- [ ] Add ProjectStatus enum (Active, Closed, Archived)
- [ ] Create ProjectRepository with tenant filtering
- [ ] Implement project closure logic (sets ClosedAt timestamp)
- [ ] Add ProjectsController with CRUD endpoints
- [ ] Support multiple companies as co-parties (ProjectParties table)
- [ ] Add original contract document attachment

## Reference
- initialDesign.md section 5 (Projects table)

## Epic
Core Domain Entities
"@ `
    --label "domain,phase-1"

gh issue create --title "Implement Document Entity with Versioning" `
    --body @"
Create Document entity with version control:

## Tasks
- [ ] Create Document entity (FileName, BlobPath, Version, ParentDocumentId)
- [ ] Implement document versioning (parent-child relationship)
- [ ] Create DocumentRepository with tenant filtering
- [ ] Add DocumentsController with upload/download endpoints
- [ ] Implement soft delete (never hard delete)
- [ ] Add document metadata (description, file size, content type)
- [ ] Generate SAS URLs for secure downloads (15 min expiry)

## Reference
- initialDesign.md section 5 (Documents table)

## Epic
Core Domain Entities
"@ `
    --label "domain,storage,phase-1"

# Epic 3: Access Control & Security
Write-Host "Creating Epic 3: Access Control & Security..." -ForegroundColor Yellow

gh issue create --title "Implement JWT Authentication System" `
    --body @"
Setup JWT-based authentication with refresh tokens:

## Tasks
- [ ] Configure JWT settings in appsettings (key, issuer, audience)
- [ ] Create IAuthService interface
- [ ] Implement AuthService with login/register methods
- [ ] Generate access tokens (15 min) and refresh tokens (7 days)
- [ ] Add JWT middleware to validate tokens
- [ ] Create AuthController (login, register, refresh, logout)
- [ ] Include CompanyId and UserId in JWT claims
- [ ] Secure password reset flow

## Reference
- initialDesign.md section 7 (Authentication)

## Epic
Access Control & Security
"@ `
    --label "security,auth,phase-1"

gh issue create --title "Implement Project-Level RBAC" `
    --body @"
Implement Role-Based Access Control at project level:

## Tasks
- [ ] Create ProjectPermission entity (ProjectId, UserId, Role)
- [ ] Add ProjectRole enum (Admin, Editor, Viewer)
- [ ] Create IPermissionService interface
- [ ] Implement permission checking logic
- [ ] Add PermissionMiddleware for API endpoints
- [ ] Create endpoints to grant/revoke permissions
- [ ] Add project member invitation flow
- [ ] Test permission inheritance and restrictions

## Roles
- **Admin**: Full control (manage members, delete project)
- **Editor**: Upload/edit/delete documents, manage tags
- **Viewer**: Read-only access

## Reference
- initialDesign.md section 7 (Authorization)

## Epic
Access Control & Security
"@ `
    --label "security,rbac,phase-1"

gh issue create --title "Implement Comprehensive Audit Logging" `
    --body @"
Create audit trail for all critical operations:

## Tasks
- [ ] Create AuditLog entity (Action, EntityType, EntityId, UserId, Timestamp)
- [ ] Add AuditAction enum (all document/user/project actions)
- [ ] Create IAuditService interface
- [ ] Implement automatic audit logging middleware
- [ ] Log: document upload/download/view/share/delete
- [ ] Log: authentication events and permission changes
- [ ] Include IP address and User-Agent
- [ ] Create audit trail query endpoints
- [ ] Ensure 3-year retention compliance

## Reference
- initialDesign.md sections 5 & 7 (AuditLogs table)

## Epic
Access Control & Security
"@ `
    --label "security,compliance,phase-1"

# Epic 4: Document Management Features
Write-Host "Creating Epic 4: Document Management Features..." -ForegroundColor Yellow

gh issue create --title "Implement Document Tagging System" `
    --body @"
Create flexible tagging system for document organization:

## Tasks
- [ ] Create Tag entity (Name, Type, CompanyId)
- [ ] Create DocumentTag entity (many-to-many relationship)
- [ ] Add standard tags (Contract, Evidence, Court Filing, Correspondence)
- [ ] Allow custom tags per company
- [ ] Create TagsController with CRUD endpoints
- [ ] Add tag filtering to document queries
- [ ] Implement tag autocomplete for UI

## Reference
- initialDesign.md section 2.6 (Tagging)

## Epic
Document Management Features
"@ `
    --label "feature,phase-1"

gh issue create --title "Implement Secure Document Sharing" `
    --body @"
Create secure document sharing with external parties:

## Tasks
- [ ] Create DocumentShare entity (ShareLink, ExpiresAt, Password)
- [ ] Generate unique share links with UUID
- [ ] Implement link expiration logic
- [ ] Add optional password protection (hashed)
- [ ] Track access count and max access limits
- [ ] Log all external accesses in audit trail
- [ ] Create share revocation endpoint
- [ ] Add public share access endpoint (no auth required)

## Reference
- initialDesign.md sections 2.4 & 5 (DocumentShares table)

## Epic
Document Management Features
"@ `
    --label "feature,security,phase-1"

gh issue create --title "Implement Document Version Control" `
    --body @"
Enable users to upload new versions of documents:

## Tasks
- [ ] Link new versions to parent document (ParentDocumentId)
- [ ] Increment version number automatically
- [ ] Create endpoint to list all versions of a document
- [ ] Allow downloading specific versions
- [ ] Display version history in UI
- [ ] Maintain all versions (never delete old versions)
- [ ] Add version comparison metadata

## Reference
- initialDesign.md section 2.3 (Version control)

## Epic
Document Management Features
"@ `
    --label "feature,phase-1"

# Epic 5: Subscription & License Management
Write-Host "Creating Epic 5: Subscription & License Management..." -ForegroundColor Yellow

gh issue create --title "Implement License/Subscription Entity" `
    --body @"
Create subscription management system:

## Tasks
- [ ] Create License entity (CompanyId, Tier, MaxContracts, MaxStorageGB)
- [ ] Add subscription tiers (Basic, Professional, Enterprise, Custom)
- [ ] Implement license validation logic
- [ ] Check storage limits before document upload
- [ ] Check contract limits before project creation
- [ ] Create license upgrade/downgrade flow
- [ ] Add license expiration checking
- [ ] Send alerts before expiration

## Tiers
- **Basic**: 10 contracts, 50GB - ₹1,000/month
- **Professional**: 50 contracts, 250GB - ₹5,000/month
- **Enterprise**: 200 contracts, 1TB - ₹15,000/month

## Reference
- initialDesign.md section 5 (Licenses table)

## Epic
Subscription & License Management
"@ `
    --label "business,phase-1"

gh issue create --title "Integrate Razorpay Payment Gateway" `
    --body @"
Integrate Razorpay for subscription payments:

## Tasks
- [ ] Install Razorpay SDK
- [ ] Create IRazorpayService interface
- [ ] Implement subscription creation flow
- [ ] Handle payment webhooks (success, failure, refund)
- [ ] Create invoice generation logic
- [ ] Add payment history endpoints
- [ ] Handle subscription renewal
- [ ] Implement grace period logic

## Reference
- initialDesign.md section 4 (Payment)

## Epic
Subscription & License Management
"@ `
    --label "integration,payment,phase-1"

# Epic 6: Advanced Features (Phase 2)
Write-Host "Creating Epic 6: Advanced Features (Phase 2)..." -ForegroundColor Yellow

gh issue create --title "Integrate Azure Cognitive Search" `
    --body @"
Implement full-text search across documents:

## Tasks
- [ ] Setup Azure Cognitive Search resource
- [ ] Create search indexes with tenant isolation
- [ ] Implement document indexing on upload
- [ ] Add OCR for scanned PDF documents
- [ ] Create SearchController with query endpoints
- [ ] Support filters (date range, tags, document type)
- [ ] Add custom analyzers for legal terminology
- [ ] Ensure strict tenant isolation in search results

## Reference
- initialDesign.md section 2.7 (Full-Text Search)

## Epic
Advanced Features (Phase 2)
"@ `
    --label "feature,phase-2,search"

gh issue create --title "Implement AI-Powered Document Processing" `
    --body @"
Use Azure OpenAI/Document Intelligence for automation:

## Tasks
- [ ] Integrate Azure Document Intelligence
- [ ] Implement PDF bundle auto-splitting
- [ ] Create AI-based document classification
- [ ] Auto-generate tags based on content
- [ ] Extract key metadata (dates, parties, case numbers)
- [ ] Generate document summaries
- [ ] Add confidence scores for AI predictions
- [ ] Allow manual override of AI decisions

## Reference
- initialDesign.md section 2.8 (AI-Powered Processing)

## Epic
Advanced Features (Phase 2)
"@ `
    --label "feature,phase-2,ai"

gh issue create --title "Implement Storage Lifecycle Management" `
    --body @"
Automate storage tier management for cost optimization:

## Tasks
- [ ] Create Azure Blob lifecycle policies
- [ ] Move closed projects to Cool tier after 3 months
- [ ] Implement project archival logic
- [ ] Add auto-deletion after subscription ends + retention
- [ ] Create storage usage reporting
- [ ] Alert when approaching storage limits
- [ ] Implement data export for cancelled subscriptions

## Reference
- initialDesign.md section 2.10 (Storage Lifecycle)

## Epic
Advanced Features (Phase 2)
"@ `
    --label "feature,phase-2,optimization"

# Epic 7: Frontend Development
Write-Host "Creating Epic 7: Frontend Development..." -ForegroundColor Yellow

gh issue create --title "Create Company & User Management UI" `
    --body @"
Build company and user management interfaces:

## Tasks
- [ ] Create company registration form
- [ ] Build user invitation interface
- [ ] Add user list/edit/deactivate pages
- [ ] Implement company profile page (GST, address)
- [ ] Create user profile settings
- [ ] Add password change functionality

## Reference
- frontend/docs/README.md

## Epic
Frontend Development
"@ `
    --label "frontend,phase-1"

gh issue create --title "Create Project Management UI" `
    --body @"
Build project/contract management interfaces:

## Tasks
- [ ] Create project list view with filters
- [ ] Build project creation form
- [ ] Add project detail page
- [ ] Implement project status updates
- [ ] Create co-party management interface
- [ ] Add project member management
- [ ] Build permission assignment UI

## Reference
- initialDesign.md section 2

## Epic
Frontend Development
"@ `
    --label "frontend,phase-1"

gh issue create --title "Create Document Management UI" `
    --body @"
Build document upload, viewing, and management interfaces:

## Tasks
- [ ] Create document upload with drag-and-drop
- [ ] Build document list with thumbnails
- [ ] Add document viewer (PDF preview)
- [ ] Implement document download
- [ ] Create version history view
- [ ] Add tag management UI
- [ ] Build document sharing interface
- [ ] Implement search and filtering

## Reference
- initialDesign.md section 2.3

## Epic
Frontend Development
"@ `
    --label "frontend,phase-1"

gh issue create --title "Create Subscription & Billing UI" `
    --body @"
Build subscription management and payment interfaces:

## Tasks
- [ ] Create subscription plan selection page
- [ ] Build Razorpay payment integration
- [ ] Add payment history view
- [ ] Display current plan and usage
- [ ] Implement upgrade/downgrade flow
- [ ] Add invoice download
- [ ] Show storage and contract usage meters

## Reference
- initialDesign.md section 5.1

## Epic
Frontend Development
"@ `
    --label "frontend,phase-1"

# Epic 8: Testing & Quality Assurance
Write-Host "Creating Epic 8: Testing & Quality Assurance..." -ForegroundColor Yellow

gh issue create --title "Setup Unit Test Framework" `
    --body @"
Implement comprehensive unit testing:

## Tasks
- [ ] Setup xUnit for backend tests
- [ ] Create test fixtures and mocks
- [ ] Write unit tests for domain entities
- [ ] Test service layer business logic
- [ ] Test permission checking logic
- [ ] Achieve 80%+ code coverage
- [ ] Add tests to CI/CD pipeline

## Reference
- IMPLEMENTATION-CHECKLIST.md Phase 5

## Epic
Testing & Quality Assurance
"@ `
    --label "testing,quality"

gh issue create --title "Implement Integration Tests" `
    --body @"
Create integration tests for critical flows:

## Tasks
- [ ] Setup Testcontainers for PostgreSQL
- [ ] Test API endpoints end-to-end
- [ ] Test multi-tenant data isolation
- [ ] Test document upload/download flow
- [ ] Test authentication and authorization
- [ ] Test Azure Blob Storage integration
- [ ] Verify audit logging

## Reference
- IMPLEMENTATION-CHECKLIST.md Phase 5

## Epic
Testing & Quality Assurance
"@ `
    --label "testing,quality"

gh issue create --title "Security & Penetration Testing" `
    --body @"
Conduct security audits and penetration testing:

## Tasks
- [ ] Test tenant isolation (try accessing other company's data)
- [ ] Test authentication bypass attempts
- [ ] Verify CSRF protection
- [ ] Test rate limiting effectiveness
- [ ] Check for SQL injection vulnerabilities
- [ ] Test XSS protection
- [ ] Verify secure headers
- [ ] Run OWASP dependency check

## Reference
- initialDesign.md section 7

## Epic
Testing & Quality Assurance
"@ `
    --label "security,testing"

# Epic 9: Deployment & DevOps
Write-Host "Creating Epic 9: Deployment & DevOps..." -ForegroundColor Yellow

gh issue create --title "Setup Azure Infrastructure" `
    --body @"
Create and configure all Azure resources:

## Tasks
- [ ] Create resource group
- [ ] Setup Azure Database for PostgreSQL
- [ ] Configure Azure Blob Storage
- [ ] Setup Azure Cache for Redis
- [ ] Create App Service Plan and Web App
- [ ] Configure Azure Key Vault
- [ ] Setup Application Insights
- [ ] Configure managed identities

## Reference
- docs/azure-deployment.md

## Epic
Deployment & DevOps
"@ `
    --label "devops,azure"

gh issue create --title "Implement CI/CD Pipelines" `
    --body @"
Setup automated deployment pipelines:

## Tasks
- [ ] Create GitHub Actions workflows
- [ ] Setup backend build and test pipeline
- [ ] Setup frontend build pipeline
- [ ] Configure automated migrations
- [ ] Add staging environment
- [ ] Implement blue-green deployment
- [ ] Add smoke tests post-deployment
- [ ] Setup rollback procedures

## Reference
- docs/azure-deployment.md section 10

## Epic
Deployment & DevOps
"@ `
    --label "devops,automation"

# Epic 10: Monitoring & Operations
Write-Host "Creating Epic 10: Monitoring & Operations..." -ForegroundColor Yellow

gh issue create --title "Setup Application Monitoring" `
    --body @"
Implement comprehensive monitoring and alerting:

## Tasks
- [ ] Configure Application Insights
- [ ] Setup custom metrics and dashboards
- [ ] Create alerts for errors and performance
- [ ] Add health check endpoints
- [ ] Implement log aggregation
- [ ] Setup uptime monitoring
- [ ] Create performance baselines

## Reference
- initialDesign.md section 10

## Epic
Monitoring & Operations
"@ `
    --label "monitoring,operations"

gh issue create --title "Create Operational Runbooks" `
    --body @"
Document common operational procedures:

## Tasks
- [ ] Database backup and restore procedures
- [ ] Disaster recovery runbook
- [ ] Scaling procedures
- [ ] Incident response guide
- [ ] User onboarding checklist
- [ ] Common troubleshooting guide
- [ ] Security incident response

## Reference
- docs/azure-deployment.md

## Epic
Monitoring & Operations
"@ `
    --label "documentation,operations"

Write-Host ""
Write-Host "✓ Successfully created 30 GitHub issues!" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "1. Go to your GitHub repository" -ForegroundColor White
Write-Host "2. Click 'Projects' tab" -ForegroundColor White
Write-Host "3. Create a new project board (Kanban template)" -ForegroundColor White
Write-Host "4. Add these issues to your board" -ForegroundColor White
Write-Host ""
Write-Host "You can also view all issues with: gh issue list" -ForegroundColor Yellow
