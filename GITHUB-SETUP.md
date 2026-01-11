# GitHub Setup Guide

## Step 1: Create Repository on GitHub

1. Go to https://github.com/new
2. Fill in the details:
   - **Repository name**: `Lawgate-ProjectsOnPhone` (or your preferred name)
   - **Description**: `Legal Document Management System - Multi-tenant SaaS for Indian law firms`
   - **Visibility**: Private (recommended for business projects)
   - **DO NOT** initialize with README, .gitignore, or license (we already have these)

3. Click "Create repository"

## Step 2: Push Local Repository

After creating the repository, run these commands:

```powershell
# Add remote (replace YOUR_USERNAME with your GitHub username)
git remote add origin https://github.com/YOUR_USERNAME/Lawgate-ProjectsOnPhone.git

# Push to GitHub
git branch -M main
git push -u origin main
```

## Step 3: Create GitHub Project Board (Manual Setup)

Since GitHub CLI is not installed, follow these steps manually:

### Create Project Board

1. Go to your repository on GitHub
2. Click on "Projects" tab
3. Click "New project"
4. Choose "Board" template
5. Name it: "Legal Doc System Development"
6. Click "Create"

### Add Columns

The board should have these columns (drag items between them):
- **📋 Backlog** - All pending work
- **📝 To Do** - Ready to start
- **🚧 In Progress** - Currently working on
- **👀 In Review** - Code review/testing
- **✅ Done** - Completed

## Step 4: Create Backlog Items

I've prepared a detailed backlog below. Add these as issues to your repository:

---

## BACKLOG ITEMS TO CREATE

### Epic 1: Foundation & Infrastructure 🏗️

#### Issue 1.1: Setup Clean Architecture Project Structure
**Labels**: `enhancement`, `infrastructure`, `phase-1`
**Description**:
```
Implement Clean Architecture (Onion Architecture) for the .NET backend:

Tasks:
- [ ] Create LegalDocSystem.Domain project (Entities, Value Objects, Enums)
- [ ] Create LegalDocSystem.Application project (Services, DTOs, Interfaces)
- [ ] Create LegalDocSystem.Infrastructure project (Data, Storage, External Services)
- [ ] Create LegalDocSystem.API project (Controllers, Middleware)
- [ ] Setup project dependencies (Domain → Application → Infrastructure → API)
- [ ] Configure dependency injection in Program.cs

Reference: backend/docs/README.md, initialDesign.md sections 4-5
```

#### Issue 1.2: Implement Multi-Tenant Database Architecture
**Labels**: `database`, `security`, `phase-1`
**Description**:
```
Design and implement multi-tenant database with strict data isolation:

Tasks:
- [ ] Create all entity models (Company, User, Project, Document, etc.)
- [ ] Implement Row-Level Security (RLS) using EF Core query filters
- [ ] Add CompanyId to all tenant-specific entities
- [ ] Create database indexes for multi-tenant queries
- [ ] Setup TenantMiddleware to extract CompanyId from JWT
- [ ] Test data isolation between tenants

Security Critical: Absolute separation between companies required.

Reference: initialDesign.md section 5 (Data Model)
```

#### Issue 1.3: Setup Azure Blob Storage Integration
**Labels**: `infrastructure`, `azure`, `phase-1`
**Description**:
```
Integrate Azure Blob Storage for document storage:

Tasks:
- [ ] Install Azure.Storage.Blobs NuGet package
- [ ] Create IAzureBlobStorageService interface
- [ ] Implement AzureBlobStorageService with SAS token generation
- [ ] Configure hierarchical namespace: {company-id}/{project-id}/{document-id}/
- [ ] Implement chunked upload for large files (up to 500MB)
- [ ] Add blob lifecycle management configuration
- [ ] Setup connection in appsettings with Key Vault reference

Reference: initialDesign.md section 4
```

---

### Epic 2: Core Domain Entities 📦

#### Issue 2.1: Implement Company Entity & Management
**Labels**: `domain`, `phase-1`
**Description**:
```
Create Company entity with complete business logic:

Tasks:
- [ ] Create Company entity (Id, Name, GSTNumber, Address, IsActive)
- [ ] Add Address value object (Street, City, State, Pincode)
- [ ] Implement soft delete functionality
- [ ] Create CompanyRepository with multi-tenant filters
- [ ] Add CompaniesController with CRUD endpoints
- [ ] Add company registration validation (GST format)
- [ ] Create CompanyDto for API responses

Acceptance Criteria:
- GST number validation for India
- Audit logging for all operations
- Cannot view other company's data

Reference: initialDesign.md section 5 (Companies table)
```

#### Issue 2.2: Implement User Management with Company Association
**Labels**: `domain`, `auth`, `phase-1`
**Description**:
```
Create User entity with company association and authentication:

Tasks:
- [ ] Create User entity (CompanyId, Email, PasswordHash, Name, Phone)
- [ ] Implement BCrypt password hashing
- [ ] Create UserRepository with tenant filtering
- [ ] Add UsersController with user management endpoints
- [ ] Implement user invitation flow
- [ ] Add email uniqueness validation
- [ ] Create user profile endpoints

Reference: initialDesign.md section 5 (Users table)
```

#### Issue 2.3: Implement Project (Contract/Case) Entity
**Labels**: `domain`, `phase-1`
**Description**:
```
Create Project entity representing legal cases/contracts:

Tasks:
- [ ] Create Project entity (Name, Description, Status, ContractDocumentId)
- [ ] Add ProjectStatus enum (Active, Closed, Archived)
- [ ] Create ProjectRepository with tenant filtering
- [ ] Implement project closure logic (sets ClosedAt timestamp)
- [ ] Add ProjectsController with CRUD endpoints
- [ ] Support multiple companies as co-parties (ProjectParties table)
- [ ] Add original contract document attachment

Reference: initialDesign.md section 5 (Projects table)
```

#### Issue 2.4: Implement Document Entity with Versioning
**Labels**: `domain`, `storage`, `phase-1`
**Description**:
```
Create Document entity with version control:

Tasks:
- [ ] Create Document entity (FileName, BlobPath, Version, ParentDocumentId)
- [ ] Implement document versioning (parent-child relationship)
- [ ] Create DocumentRepository with tenant filtering
- [ ] Add DocumentsController with upload/download endpoints
- [ ] Implement soft delete (never hard delete)
- [ ] Add document metadata (description, file size, content type)
- [ ] Generate SAS URLs for secure downloads (15 min expiry)

Reference: initialDesign.md section 5 (Documents table)
```

---

### Epic 3: Access Control & Security 🔐

#### Issue 3.1: Implement JWT Authentication System
**Labels**: `security`, `auth`, `phase-1`
**Description**:
```
Setup JWT-based authentication with refresh tokens:

Tasks:
- [ ] Configure JWT settings in appsettings (key, issuer, audience)
- [ ] Create IAuthService interface
- [ ] Implement AuthService with login/register methods
- [ ] Generate access tokens (15 min) and refresh tokens (7 days)
- [ ] Add JWT middleware to validate tokens
- [ ] Create AuthController (login, register, refresh, logout)
- [ ] Include CompanyId and UserId in JWT claims
- [ ] Secure password reset flow

Reference: initialDesign.md section 7 (Authentication)
```

#### Issue 3.2: Implement Project-Level RBAC
**Labels**: `security`, `rbac`, `phase-1`
**Description**:
```
Implement Role-Based Access Control at project level:

Tasks:
- [ ] Create ProjectPermission entity (ProjectId, UserId, Role)
- [ ] Add ProjectRole enum (Admin, Editor, Viewer)
- [ ] Create IPermissionService interface
- [ ] Implement permission checking logic
- [ ] Add PermissionMiddleware for API endpoints
- [ ] Create endpoints to grant/revoke permissions
- [ ] Add project member invitation flow
- [ ] Test permission inheritance and restrictions

Roles:
- Admin: Full control (manage members, delete project)
- Editor: Upload/edit/delete documents, manage tags
- Viewer: Read-only access

Reference: initialDesign.md section 7 (Authorization)
```

#### Issue 3.3: Implement Comprehensive Audit Logging
**Labels**: `security`, `compliance`, `phase-1`
**Description**:
```
Create audit trail for all critical operations:

Tasks:
- [ ] Create AuditLog entity (Action, EntityType, EntityId, UserId, Timestamp)
- [ ] Add AuditAction enum (all document/user/project actions)
- [ ] Create IAuditService interface
- [ ] Implement automatic audit logging middleware
- [ ] Log: document upload/download/view/share/delete
- [ ] Log: authentication events and permission changes
- [ ] Include IP address and User-Agent
- [ ] Create audit trail query endpoints
- [ ] Ensure 3-year retention compliance

Reference: initialDesign.md sections 5 & 7 (AuditLogs table)
```

---

### Epic 4: Document Management Features 📄

#### Issue 4.1: Implement Document Tagging System
**Labels**: `feature`, `phase-1`
**Description**:
```
Create flexible tagging system for document organization:

Tasks:
- [ ] Create Tag entity (Name, Type, CompanyId)
- [ ] Create DocumentTag entity (many-to-many relationship)
- [ ] Add standard tags (Contract, Evidence, Court Filing, Correspondence)
- [ ] Allow custom tags per company
- [ ] Create TagsController with CRUD endpoints
- [ ] Add tag filtering to document queries
- [ ] Implement tag autocomplete for UI

Reference: initialDesign.md section 2.6 (Tagging)
```

#### Issue 4.2: Implement Secure Document Sharing
**Labels**: `feature`, `security`, `phase-1`
**Description**:
```
Create secure document sharing with external parties:

Tasks:
- [ ] Create DocumentShare entity (ShareLink, ExpiresAt, Password)
- [ ] Generate unique share links with UUID
- [ ] Implement link expiration logic
- [ ] Add optional password protection (hashed)
- [ ] Track access count and max access limits
- [ ] Log all external accesses in audit trail
- [ ] Create share revocation endpoint
- [ ] Add public share access endpoint (no auth required)

Reference: initialDesign.md sections 2.4 & 5 (DocumentShares table)
```

#### Issue 4.3: Implement Document Version Control
**Labels**: `feature`, `phase-1`
**Description**:
```
Enable users to upload new versions of documents:

Tasks:
- [ ] Link new versions to parent document (ParentDocumentId)
- [ ] Increment version number automatically
- [ ] Create endpoint to list all versions of a document
- [ ] Allow downloading specific versions
- [ ] Display version history in UI
- [ ] Maintain all versions (never delete old versions)
- [ ] Add version comparison metadata

Reference: initialDesign.md section 2.3 (Version control)
```

---

### Epic 5: Subscription & License Management 💳

#### Issue 5.1: Implement License/Subscription Entity
**Labels**: `business`, `phase-1`
**Description**:
```
Create subscription management system:

Tasks:
- [ ] Create License entity (CompanyId, Tier, MaxContracts, MaxStorageGB)
- [ ] Add subscription tiers (Basic, Professional, Enterprise, Custom)
- [ ] Implement license validation logic
- [ ] Check storage limits before document upload
- [ ] Check contract limits before project creation
- [ ] Create license upgrade/downgrade flow
- [ ] Add license expiration checking
- [ ] Send alerts before expiration

Tiers:
- Basic: 10 contracts, 50GB - ₹1,000/month
- Professional: 50 contracts, 250GB - ₹5,000/month
- Enterprise: 200 contracts, 1TB - ₹15,000/month

Reference: initialDesign.md section 5 (Licenses table)
```

#### Issue 5.2: Integrate Razorpay Payment Gateway
**Labels**: `integration`, `payment`, `phase-1`
**Description**:
```
Integrate Razorpay for subscription payments:

Tasks:
- [ ] Install Razorpay SDK
- [ ] Create IRazorpayService interface
- [ ] Implement subscription creation flow
- [ ] Handle payment webhooks (success, failure, refund)
- [ ] Create invoice generation logic
- [ ] Add payment history endpoints
- [ ] Handle subscription renewal
- [ ] Implement grace period logic

Reference: initialDesign.md section 4 (Payment)
```

---

### Epic 6: Advanced Features (Phase 2) 🚀

#### Issue 6.1: Integrate Azure Cognitive Search
**Labels**: `feature`, `phase-2`, `search`
**Description**:
```
Implement full-text search across documents:

Tasks:
- [ ] Setup Azure Cognitive Search resource
- [ ] Create search indexes with tenant isolation
- [ ] Implement document indexing on upload
- [ ] Add OCR for scanned PDF documents
- [ ] Create SearchController with query endpoints
- [ ] Support filters (date range, tags, document type)
- [ ] Add custom analyzers for legal terminology
- [ ] Ensure strict tenant isolation in search results

Reference: initialDesign.md section 2.7 (Full-Text Search)
```

#### Issue 6.2: Implement AI-Powered Document Processing
**Labels**: `feature`, `phase-2`, `ai`
**Description**:
```
Use Azure OpenAI/Document Intelligence for automation:

Tasks:
- [ ] Integrate Azure Document Intelligence
- [ ] Implement PDF bundle auto-splitting
- [ ] Create AI-based document classification
- [ ] Auto-generate tags based on content
- [ ] Extract key metadata (dates, parties, case numbers)
- [ ] Generate document summaries
- [ ] Add confidence scores for AI predictions
- [ ] Allow manual override of AI decisions

Reference: initialDesign.md section 2.8 (AI-Powered Processing)
```

#### Issue 6.3: Implement Storage Lifecycle Management
**Labels**: `feature`, `phase-2`, `optimization`
**Description**:
```
Automate storage tier management for cost optimization:

Tasks:
- [ ] Create Azure Blob lifecycle policies
- [ ] Move closed projects to Cool tier after 3 months
- [ ] Implement project archival logic
- [ ] Add auto-deletion after subscription ends + retention
- [ ] Create storage usage reporting
- [ ] Alert when approaching storage limits
- [ ] Implement data export for cancelled subscriptions

Reference: initialDesign.md section 2.10 (Storage Lifecycle)
```

---

### Epic 7: Frontend Development 🎨

#### Issue 7.1: Create Company & User Management UI
**Labels**: `frontend`, `phase-1`
**Description**:
```
Build company and user management interfaces:

Tasks:
- [ ] Create company registration form
- [ ] Build user invitation interface
- [ ] Add user list/edit/deactivate pages
- [ ] Implement company profile page (GST, address)
- [ ] Create user profile settings
- [ ] Add password change functionality

Reference: frontend/docs/README.md
```

#### Issue 7.2: Create Project Management UI
**Labels**: `frontend`, `phase-1`
**Description**:
```
Build project/contract management interfaces:

Tasks:
- [ ] Create project list view with filters
- [ ] Build project creation form
- [ ] Add project detail page
- [ ] Implement project status updates
- [ ] Create co-party management interface
- [ ] Add project member management
- [ ] Build permission assignment UI

Reference: initialDesign.md section 2
```

#### Issue 7.3: Create Document Management UI
**Labels**: `frontend`, `phase-1`
**Description**:
```
Build document upload, viewing, and management interfaces:

Tasks:
- [ ] Create document upload with drag-and-drop
- [ ] Build document list with thumbnails
- [ ] Add document viewer (PDF preview)
- [ ] Implement document download
- [ ] Create version history view
- [ ] Add tag management UI
- [ ] Build document sharing interface
- [ ] Implement search and filtering

Reference: initialDesign.md section 2.3
```

#### Issue 7.4: Create Subscription & Billing UI
**Labels**: `frontend`, `phase-1`
**Description**:
```
Build subscription management and payment interfaces:

Tasks:
- [ ] Create subscription plan selection page
- [ ] Build Razorpay payment integration
- [ ] Add payment history view
- [ ] Display current plan and usage
- [ ] Implement upgrade/downgrade flow
- [ ] Add invoice download
- [ ] Show storage and contract usage meters

Reference: initialDesign.md section 5.1
```

---

### Epic 8: Testing & Quality Assurance ✅

#### Issue 8.1: Setup Unit Test Framework
**Labels**: `testing`, `quality`
**Description**:
```
Implement comprehensive unit testing:

Tasks:
- [ ] Setup xUnit for backend tests
- [ ] Create test fixtures and mocks
- [ ] Write unit tests for domain entities
- [ ] Test service layer business logic
- [ ] Test permission checking logic
- [ ] Achieve 80%+ code coverage
- [ ] Add tests to CI/CD pipeline

Reference: IMPLEMENTATION-CHECKLIST.md Phase 5
```

#### Issue 8.2: Implement Integration Tests
**Labels**: `testing`, `quality`
**Description**:
```
Create integration tests for critical flows:

Tasks:
- [ ] Setup Testcontainers for PostgreSQL
- [ ] Test API endpoints end-to-end
- [ ] Test multi-tenant data isolation
- [ ] Test document upload/download flow
- [ ] Test authentication and authorization
- [ ] Test Azure Blob Storage integration
- [ ] Verify audit logging

Reference: IMPLEMENTATION-CHECKLIST.md Phase 5
```

#### Issue 8.3: Security & Penetration Testing
**Labels**: `security`, `testing`
**Description**:
```
Conduct security audits and penetration testing:

Tasks:
- [ ] Test tenant isolation (try accessing other company's data)
- [ ] Test authentication bypass attempts
- [ ] Verify CSRF protection
- [ ] Test rate limiting effectiveness
- [ ] Check for SQL injection vulnerabilities
- [ ] Test XSS protection
- [ ] Verify secure headers
- [ ] Run OWASP dependency check

Reference: initialDesign.md section 7
```

---

### Epic 9: Deployment & DevOps ☁️

#### Issue 9.1: Setup Azure Infrastructure
**Labels**: `devops`, `azure`
**Description**:
```
Create and configure all Azure resources:

Tasks:
- [ ] Create resource group
- [ ] Setup Azure Database for PostgreSQL
- [ ] Configure Azure Blob Storage
- [ ] Setup Azure Cache for Redis
- [ ] Create App Service Plan and Web App
- [ ] Configure Azure Key Vault
- [ ] Setup Application Insights
- [ ] Configure managed identities

Reference: docs/azure-deployment.md
```

#### Issue 9.2: Implement CI/CD Pipelines
**Labels**: `devops`, `automation`
**Description**:
```
Setup automated deployment pipelines:

Tasks:
- [ ] Create GitHub Actions workflows
- [ ] Setup backend build and test pipeline
- [ ] Setup frontend build pipeline
- [ ] Configure automated migrations
- [ ] Add staging environment
- [ ] Implement blue-green deployment
- [ ] Add smoke tests post-deployment
- [ ] Setup rollback procedures

Reference: docs/azure-deployment.md section 10
```

---

### Epic 10: Monitoring & Operations 📊

#### Issue 10.1: Setup Application Monitoring
**Labels**: `monitoring`, `operations`
**Description**:
```
Implement comprehensive monitoring and alerting:

Tasks:
- [ ] Configure Application Insights
- [ ] Setup custom metrics and dashboards
- [ ] Create alerts for errors and performance
- [ ] Add health check endpoints
- [ ] Implement log aggregation
- [ ] Setup uptime monitoring
- [ ] Create performance baselines

Reference: initialDesign.md section 10
```

#### Issue 10.2: Create Operational Runbooks
**Labels**: `documentation`, `operations`
**Description**:
```
Document common operational procedures:

Tasks:
- [ ] Database backup and restore procedures
- [ ] Disaster recovery runbook
- [ ] Scaling procedures
- [ ] Incident response guide
- [ ] User onboarding checklist
- [ ] Common troubleshooting guide
- [ ] Security incident response

Reference: docs/azure-deployment.md
```

---

## How to Add These Issues

For each issue above:

1. Go to your GitHub repository
2. Click "Issues" tab
3. Click "New issue"
4. Copy the title and description
5. Add the appropriate labels
6. Click "Create issue"
7. Drag the issue to your Project Board's "Backlog" column

## Priority Order

**MVP (Phase 1) - Focus on these first:**
1. Foundation & Infrastructure (Epic 1)
2. Core Domain Entities (Epic 2)
3. Access Control & Security (Epic 3)
4. Document Management Features (Epic 4)
5. Subscription Management (Epic 5)
6. Frontend Development (Epic 7)
7. Testing (Epic 8)
8. Deployment (Epic 9)

**Phase 2 - Advanced Features:**
- Advanced Features (Epic 6)
- Monitoring & Operations (Epic 10)

---

**Total Estimated Issues: 28+ detailed backlog items**

This backlog covers all requirements from initialDesign.md and provides clear, actionable tasks for your development team.
