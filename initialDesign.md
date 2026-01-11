# Software Architecture Prompt: Legal Document Management System

## Executive Summary
Design a secure, multi-tenant legal document management system for managing legal cases/contracts in India. The system must prioritize data isolation, audit compliance, and scalability while supporting law firms and their clients in collaborative case management.

## 1. Context & Business Requirements

### Primary Purpose
Build a SaaS platform where law firms and legal parties can:
- Manage multiple legal cases (contracts) with complete document lifecycle
- Collaborate securely with co-parties, external lawyers, and respondents
- Maintain compliance through comprehensive audit trails
- Share documents via secure links or in-app access
- Search and organize documents with tagging and categorization

### Target Market
- Initial launch: India-based law firms and corporate legal departments
- Year 1 target: 5 companies
- Expected scale: 10-50 active contracts per company initially

### Key Actors
1. **Admins** (LawGate internal users): System administration, support
2. **Company Users** (Law firm representatives): Full access to their company's projects
3. **Project Members**: Users with specific project-level permissions
4. **External Parties**: Invited users (other lawyers, respondents) with limited access
5. **Guest Users**: Temporary link-based document access (future phase)

## 2. Functional Requirements

### Core Features (MVP - Phase 1)
1. **Multi-tenant Company Management**
   - Company registration with GST details
   - User management within companies
   - Subscription and license management

2. **Project/Contract Management**
   - Create projects representing legal cases/contracts
   - Support multiple companies as co-parties on same side
   - Project status tracking (Active, Closed, Archived)
   - Attach original contract document to project

3. **Document Management**
   - Upload documents to Azure Blob Storage
   - Version control with accessible history
   - Document metadata (description, tags, timestamps)
   - Soft delete (never hard delete)
   - Support primarily PDFs, with extensibility for other formats

4. **Access Control & Sharing**
   - Role-Based Access Control (RBAC) at project level
   - Invite external users to view specific projects
   - Generate secure download links for documents
   - Fine-grained permissions: Admin, Editor, Viewer per project

5. **Audit & Compliance**
   - Track all document uploads, downloads, views, shares
   - Log user actions with timestamps
   - Maintain audit trail for minimum 3 years

6. **Tagging & Organization**
   - Custom tags per project/company
   - Standard tag library (Contract, Evidence, Court Filing, Correspondence)
   - Tag-based filtering and search

### Enhanced Features (Phase 2)
7. **Full-Text Search**
   - Search across document content (OCR for scanned PDFs)
   - Filter by date ranges, parties, tags, document types
   - Strict tenant isolation in search results

8. **AI-Powered Document Processing**
   - Upload large PDF bundles
   - Auto-split into individual documents
   - AI-based tagging and categorization
   - Replace manual paralegal work

9. **Advanced Sharing**
   - Temporary access links with expiration
   - Password-protected shares
   - Track external access in audit log

10. **Storage Lifecycle Management**
    - Auto-archive closed projects after 3 months
    - Move to Azure Cool Blob Storage
    - Automatic deletion after subscription ends + retention period

## 3. Non-Functional Requirements

### Security (Critical Priority)
- **Data Isolation**: Absolute separation between companies (row-level security)
- **Encryption**: 
  - At rest: Azure Blob Storage encryption
  - In transit: TLS 1.3
  - Optional: Client-side encryption for highly sensitive documents
- **Authentication**: JWT-based with refresh tokens
- **Authorization**: RBAC with project-level granularity
- **Audit**: Every data access logged with user ID, timestamp, action
- **Compliance**: GDPR-ready, Indian data residency considerations

### Scalability
- **Horizontal scaling**: Stateless API servers behind load balancer
- **Database**: MySQL with connection pooling, read replicas for reporting
- **Blob Storage**: Single Azure Storage Account with container-per-company or hierarchical naming
- **Caching**: Redis for session management and frequently accessed metadata
- **Design for**: 100+ companies, 10K+ documents per company within 3 years

### Performance
- **API Response Time**: < 200ms for metadata operations
- **Document Upload**: Support up to 500MB files with chunked uploads
- **Document Download**: Stream from blob storage with signed URLs
- **Search**: < 1 second for filtered queries (with proper indexing)

### Reliability
- **Uptime**: 99.5% target
- **Backup**: Daily automated backups, point-in-time recovery
- **Disaster Recovery**: Cross-region blob replication
- **Data Retention**: Minimum 3 years for audit logs

### Maintainability
- **Code Quality**: SOLID principles, clean architecture
- **Testing**: 80%+ unit test coverage, integration tests for critical paths
- **Documentation**: API docs (OpenAPI/Swagger), architecture diagrams
- **Monitoring**: Application insights, error tracking, performance metrics

## 4. Technical Architecture

### Recommended Tech Stack

#### Backend: .NET Core 8+ (Recommended over Flask)
**Rationale:**
- Superior Azure integration (Azure SDK, Blob Storage, Key Vault)
- Built-in dependency injection, strong typing
- Entity Framework Core for ORM with migration support
- Better enterprise-grade security libraries
- Async/await for better I/O performance
- Easier to enforce coding standards in larger teams

**Alternative**: Flask + SQLAlchemy (faster MVP, but harder to scale)

#### Frontend: React 18+
- TypeScript for type safety
- React Query for server state management
- Zustand/Redux for client state
- Tailwind CSS for styling
- React Native or Capacitor for mobile apps (Phase 2)

#### Database: Azure Database for MySQL or PostgreSQL
- PostgreSQL preferred for better JSON support and full-text search
- Row-level security policies
- Indexing strategy for multi-tenant queries

#### Storage: Azure Blob Storage
- Hot tier for active projects
- Cool tier for archived projects
- Hierarchical namespace: `{company-id}/{project-id}/{document-id}/{filename}`
- Container strategy: Single storage account, one container per environment

#### Caching: Azure Cache for Redis
- Session management
- Frequently accessed project metadata
- Rate limiting counters

#### Search: Azure Cognitive Search (Phase 2)
- Full-text search with tenant isolation
- OCR for scanned documents
- Custom analyzers for legal terminology

#### AI Services: Azure OpenAI / Document Intelligence (Phase 2)
- Document splitting and classification
- Auto-tagging
- Summary generation

#### Authentication: Azure AD B2C or Auth0
- OAuth 2.0 / OpenID Connect
- Multi-factor authentication support
- SSO for enterprise clients

#### Payment: Razorpay
- Subscription management
- Webhook handling for payment events
- Invoice generation

### Architecture Pattern: Clean Architecture (Onion Architecture)

```
┌─────────────────────────────────────────┐
│         Presentation Layer              │
│  (API Controllers, DTOs, Validators)    │
└─────────────────┬───────────────────────┘
                  │
┌─────────────────▼───────────────────────┐
│        Application Layer                │
│  (Use Cases, Services, Interfaces)      │
└─────────────────┬───────────────────────┘
                  │
┌─────────────────▼───────────────────────┐
│          Domain Layer                   │
│  (Entities, Value Objects, Domain       │
│   Events, Business Logic)               │
└─────────────────────────────────────────┘
                  ▲
┌─────────────────┴───────────────────────┐
│       Infrastructure Layer              │
│  (Data Access, Blob Storage, External   │
│   Services, Email, Notifications)       │
└─────────────────────────────────────────┘
```

### Project Structure (.NET)

```
LegalDocumentSystem/
├── src/
│   ├── LegalDocSystem.Domain/
│   │   ├── Entities/
│   │   │   ├── Company.cs
│   │   │   ├── User.cs
│   │   │   ├── Project.cs
│   │   │   ├── Document.cs
│   │   │   ├── ProjectParty.cs
│   │   │   ├── Permission.cs
│   │   │   ├── AuditLog.cs
│   │   │   ├── Tag.cs
│   │   │   ├── DocumentTag.cs
│   │   │   └── License.cs
│   │   ├── ValueObjects/
│   │   │   ├── Address.cs
│   │   │   └── Money.cs
│   │   ├── Enums/
│   │   │   ├── ProjectRole.cs
│   │   │   ├── ProjectStatus.cs
│   │   │   └── AuditAction.cs
│   │   └── Interfaces/
│   │       └── IEntity.cs
│   │
│   ├── LegalDocSystem.Application/
│   │   ├── Services/
│   │   │   ├── ProjectService.cs
│   │   │   ├── DocumentService.cs
│   │   │   ├── PermissionService.cs
│   │   │   └── AuditService.cs
│   │   ├── DTOs/
│   │   ├── Interfaces/
│   │   │   ├── IDocumentStorage.cs
│   │   │   ├── IRepository.cs
│   │   │   └── IUnitOfWork.cs
│   │   ├── Validators/
│   │   └── Exceptions/
│   │
│   ├── LegalDocSystem.Infrastructure/
│   │   ├── Data/
│   │   │   ├── ApplicationDbContext.cs
│   │   │   ├── Repositories/
│   │   │   └── Migrations/
│   │   ├── Storage/
│   │   │   └── AzureBlobStorageService.cs
│   │   ├── Identity/
│   │   ├── External/
│   │   │   └── RazorpayService.cs
│   │   └── Caching/
│   │       └── RedisCacheService.cs
│   │
│   └── LegalDocSystem.API/
│       ├── Controllers/
│       │   ├── CompaniesController.cs
│       │   ├── ProjectsController.cs
│       │   ├── DocumentsController.cs
│       │   └── UsersController.cs
│       ├── Middleware/
│       │   ├── TenantMiddleware.cs
│       │   ├── AuditMiddleware.cs
│       │   └── ExceptionMiddleware.cs
│       ├── Filters/
│       │   └── ValidateModelAttribute.cs
│       └── Program.cs
│
├── tests/
│   ├── LegalDocSystem.UnitTests/
│   ├── LegalDocSystem.IntegrationTests/
│   └── LegalDocSystem.E2ETests/
│
└── docs/
    ├── api/
    └── architecture/
```

## 5. Data Model Design

### Core Entities

```sql
-- Companies (Tenants)
CREATE TABLE Companies (
    CompanyId UUID PRIMARY KEY,
    Name VARCHAR(255) NOT NULL,
    GSTNumber VARCHAR(15) UNIQUE,
    Phone VARCHAR(15),
    IsActive BOOLEAN DEFAULT TRUE,
    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    DeletedAt TIMESTAMP NULL,  -- Soft delete
    
    -- Address (embedded)
    AddressStreet VARCHAR(255),
    AddressCity VARCHAR(100),
    AddressState VARCHAR(100),
    AddressCountry VARCHAR(50) DEFAULT 'India',
    AddressPincode VARCHAR(10)
);

-- Users
CREATE TABLE Users (
    UserId UUID PRIMARY KEY,
    CompanyId UUID NOT NULL,
    Email VARCHAR(255) UNIQUE NOT NULL,
    PasswordHash VARCHAR(255) NOT NULL,
    FirstName VARCHAR(100),
    LastName VARCHAR(100),
    Phone VARCHAR(15),
    IsActive BOOLEAN DEFAULT TRUE,
    LastLoginAt TIMESTAMP,
    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    DeletedAt TIMESTAMP NULL,
    
    FOREIGN KEY (CompanyId) REFERENCES Companies(CompanyId),
    INDEX idx_company_user (CompanyId, UserId),
    INDEX idx_email (Email)
);

-- Projects (Contracts/Cases)
CREATE TABLE Projects (
    ProjectId UUID PRIMARY KEY,
    CompanyId UUID NOT NULL,  -- Primary company/tenant
    Name VARCHAR(255) NOT NULL,
    Description TEXT,
    Status VARCHAR(50) DEFAULT 'Active',  -- Active, Closed, Archived
    ContractDocumentId UUID,  -- Reference to original contract document
    CreatedBy UUID NOT NULL,
    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    ClosedAt TIMESTAMP NULL,
    ArchivedAt TIMESTAMP NULL,
    DeletedAt TIMESTAMP NULL,
    
    FOREIGN KEY (CompanyId) REFERENCES Companies(CompanyId),
    FOREIGN KEY (CreatedBy) REFERENCES Users(UserId),
    INDEX idx_company_project (CompanyId, ProjectId),
    INDEX idx_status (Status)
);

-- Project Parties (Multiple companies can be co-parties)
CREATE TABLE ProjectParties (
    ProjectPartyId UUID PRIMARY KEY,
    ProjectId UUID NOT NULL,
    CompanyId UUID NOT NULL,
    PartyType VARCHAR(50) NOT NULL,  -- Plaintiff, Defendant, ThirdParty
    IsActive BOOLEAN DEFAULT TRUE,
    AddedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    FOREIGN KEY (ProjectId) REFERENCES Projects(ProjectId) ON DELETE CASCADE,
    FOREIGN KEY (CompanyId) REFERENCES Companies(CompanyId),
    UNIQUE (ProjectId, CompanyId),
    INDEX idx_project_parties (ProjectId)
);

-- User Project Permissions (RBAC at project level)
CREATE TABLE ProjectPermissions (
    PermissionId UUID PRIMARY KEY,
    ProjectId UUID NOT NULL,
    UserId UUID NOT NULL,
    Role VARCHAR(50) NOT NULL,  -- Admin, Editor, Viewer
    GrantedBy UUID NOT NULL,
    GrantedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    RevokedAt TIMESTAMP NULL,
    
    FOREIGN KEY (ProjectId) REFERENCES Projects(ProjectId) ON DELETE CASCADE,
    FOREIGN KEY (UserId) REFERENCES Users(UserId),
    FOREIGN KEY (GrantedBy) REFERENCES Users(UserId),
    UNIQUE (ProjectId, UserId),
    INDEX idx_user_permissions (UserId, ProjectId)
);

-- Documents
CREATE TABLE Documents (
    DocumentId UUID PRIMARY KEY,
    ProjectId UUID NOT NULL,
    CompanyId UUID NOT NULL,  -- For tenant isolation
    FileName VARCHAR(255) NOT NULL,
    BlobStoragePath VARCHAR(500) NOT NULL,  -- Path in Azure Blob
    ContentType VARCHAR(100),
    OriginalFileSize BIGINT,
    CompressedFileSize BIGINT,
    Version INT DEFAULT 1,
    ParentDocumentId UUID NULL,  -- For versioning
    Description TEXT,
    UploadedBy UUID NOT NULL,
    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    DeletedAt TIMESTAMP NULL,
    DeletedBy UUID NULL,
    
    FOREIGN KEY (ProjectId) REFERENCES Projects(ProjectId),
    FOREIGN KEY (CompanyId) REFERENCES Companies(CompanyId),
    FOREIGN KEY (UploadedBy) REFERENCES Users(UserId),
    FOREIGN KEY (ParentDocumentId) REFERENCES Documents(DocumentId),
    INDEX idx_project_documents (ProjectId, DocumentId),
    INDEX idx_company_documents (CompanyId, DocumentId),
    INDEX idx_parent_version (ParentDocumentId, Version)
);

-- Tags
CREATE TABLE Tags (
    TagId UUID PRIMARY KEY,
    CompanyId UUID NULL,  -- NULL for system-wide tags
    Name VARCHAR(100) NOT NULL,
    Type VARCHAR(50) DEFAULT 'Custom',  -- Standard, Custom
    CreatedBy UUID,
    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    FOREIGN KEY (CompanyId) REFERENCES Companies(CompanyId),
    FOREIGN KEY (CreatedBy) REFERENCES Users(UserId),
    UNIQUE (CompanyId, Name),
    INDEX idx_company_tags (CompanyId)
);

-- Document Tags (Many-to-Many)
CREATE TABLE DocumentTags (
    DocumentTagId UUID PRIMARY KEY,
    DocumentId UUID NOT NULL,
    TagId UUID NOT NULL,
    AddedBy UUID NOT NULL,
    AddedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    FOREIGN KEY (DocumentId) REFERENCES Documents(DocumentId) ON DELETE CASCADE,
    FOREIGN KEY (TagId) REFERENCES Tags(TagId),
    FOREIGN KEY (AddedBy) REFERENCES Users(UserId),
    UNIQUE (DocumentId, TagId),
    INDEX idx_document_tags (DocumentId),
    INDEX idx_tag_documents (TagId)
);

-- Licenses (Subscriptions)
CREATE TABLE Licenses (
    LicenseId UUID PRIMARY KEY,
    CompanyId UUID NOT NULL,
    Tier VARCHAR(50) NOT NULL,  -- Basic, Professional, Enterprise
    MaxContracts INT NOT NULL,
    MaxStorageGB INT NOT NULL,
    ValidFrom DATE NOT NULL,
    ValidTill DATE NOT NULL,
    PurchasedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    IsActive BOOLEAN DEFAULT TRUE,
    RazorpaySubscriptionId VARCHAR(255),
    
    FOREIGN KEY (CompanyId) REFERENCES Companies(CompanyId),
    INDEX idx_company_license (CompanyId, ValidTill)
);

-- Audit Logs (Critical for compliance)
CREATE TABLE AuditLogs (
    AuditLogId UUID PRIMARY KEY,
    CompanyId UUID NOT NULL,
    UserId UUID,
    Action VARCHAR(100) NOT NULL,  -- DocumentUploaded, DocumentDownloaded, DocumentViewed, etc.
    EntityType VARCHAR(50),  -- Document, Project, User
    EntityId UUID,
    IPAddress VARCHAR(45),
    UserAgent TEXT,
    Metadata JSONB,  -- Additional context
    Timestamp TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    FOREIGN KEY (CompanyId) REFERENCES Companies(CompanyId),
    FOREIGN KEY (UserId) REFERENCES Users(UserId),
    INDEX idx_company_audit (CompanyId, Timestamp DESC),
    INDEX idx_entity_audit (EntityType, EntityId),
    INDEX idx_user_audit (UserId, Timestamp DESC)
);

-- Document Shares (For external sharing)
CREATE TABLE DocumentShares (
    ShareId UUID PRIMARY KEY,
    DocumentId UUID NOT NULL,
    SharedBy UUID NOT NULL,
    SharedWith UUID NULL,  -- NULL for public links
    ShareLink VARCHAR(255) UNIQUE,
    ExpiresAt TIMESTAMP,
    Password VARCHAR(255),  -- Hashed, optional
    AccessCount INT DEFAULT 0,
    MaxAccessCount INT,
    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    RevokedAt TIMESTAMP NULL,
    
    FOREIGN KEY (DocumentId) REFERENCES Documents(DocumentId),
    FOREIGN KEY (SharedBy) REFERENCES Users(UserId),
    FOREIGN KEY (SharedWith) REFERENCES Users(UserId),
    INDEX idx_document_shares (DocumentId),
    INDEX idx_share_link (ShareLink)
);
```

### Subscription Tiers & Pricing

| Tier | Contracts | Storage | Price (Monthly) |
|------|-----------|---------|-----------------|
| Basic | 0-10 | 50 GB | ₹1,000 |
| Professional | 11-50 | 250 GB | ₹5,000 |
| Enterprise | 51-200 | 1 TB | ₹15,000 |
| Custom | 200+ | Custom | Custom pricing |

**Storage Cost Analysis (Azure Blob):**
- Hot Tier: ~₹1.5/GB/month
- Cool Tier: ~₹0.75/GB/month
- With 30% margin for operations/bandwidth, pricing above is sustainable

**Blob Storage Strategy:**
- Use single storage account with hierarchical namespace
- Path structure: `{environment}/{company-id}/{project-id}/{document-id}/v{version}/{filename}`
- No separate containers per client (reduces management overhead)
- Use Azure RBAC and SAS tokens for access control

## 6. API Design

### RESTful API Endpoints

**Authentication**
- `POST /api/auth/register` - Register new company/user
- `POST /api/auth/login` - Login
- `POST /api/auth/refresh` - Refresh access token
- `POST /api/auth/logout` - Logout

**Companies**
- `GET /api/companies/{companyId}` - Get company details
- `PUT /api/companies/{companyId}` - Update company
- `GET /api/companies/{companyId}/users` - List users
- `POST /api/companies/{companyId}/users` - Invite user

**Projects**
- `GET /api/projects` - List projects (filtered by user's company)
- `POST /api/projects` - Create project
- `GET /api/projects/{projectId}` - Get project details
- `PUT /api/projects/{projectId}` - Update project
- `DELETE /api/projects/{projectId}` - Soft delete project
- `POST /api/projects/{projectId}/parties` - Add co-party
- `POST /api/projects/{projectId}/permissions` - Grant user access
- `GET /api/projects/{projectId}/permissions` - List project members

**Documents**
- `GET /api/projects/{projectId}/documents` - List documents
- `POST /api/projects/{projectId}/documents` - Upload document
- `GET /api/documents/{documentId}` - Get document metadata
- `GET /api/documents/{documentId}/download` - Download document (returns SAS URL)
- `PUT /api/documents/{documentId}` - Update metadata
- `POST /api/documents/{documentId}/versions` - Upload new version
- `DELETE /api/documents/{documentId}` - Soft delete document
- `POST /api/documents/{documentId}/tags` - Add tags
- `POST /api/documents/{documentId}/share` - Create share link

**Search**
- `GET /api/search/documents?q={query}&projectId={id}&tags={tags}&fromDate={date}` - Search documents

**Audit**
- `GET /api/audit/logs?entityType={type}&entityId={id}` - Get audit trail

**Admin**
- `GET /api/admin/companies` - List all companies
- `GET /api/admin/usage-stats` - System usage statistics

### Key Middleware Components

1. **TenantMiddleware**: Extracts CompanyId from JWT, sets context
2. **AuditMiddleware**: Logs all API calls automatically
3. **PermissionMiddleware**: Validates project-level permissions
4. **RateLimitMiddleware**: Prevents abuse (per company/user)
5. **ExceptionMiddleware**: Global error handling

## 7. Security Architecture

### Multi-Tenancy Security (CRITICAL)

**Database Level:**
```csharp
// Always filter by CompanyId in queries
public class TenantQueryFilter : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.HasQueryFilter(d => d.CompanyId == _tenantService.CurrentTenantId);
    }
}
```

**Application Level:**
```csharp
// Validate tenant access before any operation
public async Task<Document> GetDocumentAsync(Guid documentId)
{
    var document = await _repository.GetByIdAsync(documentId);
    
    if (document.CompanyId != _tenantService.CurrentTenantId)
    {
        throw new UnauthorizedException("Access denied");
    }
    
    // Log access
    await _auditService.LogAsync(AuditAction.DocumentViewed, documentId);
    
    return document;
}
```

**Blob Storage Level:**
- Generate short-lived SAS tokens (15 min expiry) for document access
- Include CompanyId in blob path for defense-in-depth
- Validate token against database before generating SAS URL

### Authentication Flow

1. User logs in → JWT issued with claims: `UserId`, `CompanyId`, `Email`
2. Access token (15 min), Refresh token (7 days)
3. Middleware extracts CompanyId from JWT on every request
4. All queries auto-filtered by CompanyId via EF Core query filters

### Authorization (RBAC)

**Project Roles:**
- **Admin**: Full control (manage members, delete project)
- **Editor**: Upload/edit/delete documents, manage tags
- **Viewer**: Read-only access

**Permission Check:**
```csharp
public async Task<bool> UserHasPermission(Guid userId, Guid projectId, ProjectRole requiredRole)
{
    var permission = await _permissionRepository
        .GetByUserAndProjectAsync(userId, projectId);
    
    return permission != null 
        && permission.RevokedAt == null 
        && permission.Role >= requiredRole;
}
```

### Audit Logging Strategy

**What to Log:**
- All document operations (upload, download, view, share, delete)
- User authentication events
- Permission changes
- Project creation/modification
- Failed access attempts

**Implementation:**
```csharp
public async Task LogAsync(AuditAction action, Guid entityId, object metadata = null)
{
    var log = new AuditLog
    {
        CompanyId = _tenantService.CurrentTenantId,
        UserId = _userService.CurrentUserId,
        Action = action.ToString(),
        EntityId = entityId,
        IPAddress = _httpContext.Connection.RemoteIpAddress.ToString(),
        UserAgent = _httpContext.Request.Headers["User-Agent"],
        Metadata = JsonSerializer.Serialize(metadata),
        Timestamp = DateTime.UtcNow
    };
    
    await _auditRepository.AddAsync(log);
}
```

## 8. Development Workflow

### Folder Structure Best Practices

**Domain Layer:** Pure business logic, no dependencies
```csharp
public class Project : Entity
{
    public Guid CompanyId { get; private set; }
    public string Name { get; private set; }
    public ProjectStatus Status { get; private set; }
    
    public void Close()
    {
        if (Status == ProjectStatus.Closed)
            throw new DomainException("Project already closed");
        
        Status = ProjectStatus.Closed;
        ClosedAt = DateTime.UtcNow;
    }
}
```

**Application Layer:** Orchestration, use cases
```csharp
public class DocumentService
{
    public async Task<DocumentDto> UploadDocumentAsync(UploadDocumentRequest request)
    {
        // 1. Validate
        await _permissionService.EnsureCanEditProject(request.ProjectId);
        
        // 2. Upload to blob storage
        var blobPath = await _blobStorage.UploadAsync(request.File);
        
        // 3. Create entity
        var document = new Document(request.ProjectId, request.FileName, blobPath);
        await _documentRepository.AddAsync(document);
        
        // 4. Audit
        await _auditService.LogAsync(AuditAction.DocumentUploaded, document.Id);
        
        // 5. Commit
        await _unitOfWork.CommitAsync();
        
        return _mapper.Map<DocumentDto>(document);
    }
}
```

### Testing Strategy

**Unit Tests:** Domain logic, services (80% coverage minimum)
```csharp
[Fact]
public void Project_Close_ShouldSetStatusAndTimestamp()
{
    var project = new Project("Test Project", companyId);
    project.Close();
    
    Assert.Equal(ProjectStatus.Closed, project.Status);
    Assert.NotNull(project.ClosedAt);
}
```

**Integration Tests:** Database, blob storage, API endpoints
```csharp
[Fact]
public async Task UploadDocument_ShouldStoreInBlobAndDatabase()
{
    // Arrange
    var client = _factory.CreateClient();
    var content = new MultipartFormDataContent();
    content.Add(new ByteArrayContent(testPdf), "file", "test.pdf");
    
    // Act
    var response = await client.PostAsync("/api/projects/123/documents", content);
    
    // Assert
    response.EnsureSuccessStatusCode();
    var document = await _dbContext.Documents.FirstAsync();
    Assert.NotNull(document.BlobStoragePath);
}
```

**E2E Tests:** Critical user flows (Playwright/Selenium)

### CI/CD Pipeline

```yaml
# Azure DevOps / GitHub Actions
stages:
  - build:
      - Restore dependencies
      - Build .NET solution
      - Run linters (StyleCop, SonarQube)
  
  - test:
      - Run unit tests
      - Run integration tests
      - Code coverage report (min 80%)
  
  - security:
      - Dependency vulnerability scan
      - SAST (static analysis)
      - Secret scanning
  
  - deploy-staging:
      - Build Docker image
      - Push to container registry
      - Deploy to Azure App Service (staging)
      - Run smoke tests
  
  - deploy-production:
      - Manual approval gate
      - Blue-green deployment
      - Health checks
      - Rollback on failure
```

## 9. Deployment Strategy

### Azure Resources

**Compute:**
- Azure App Service (Linux, .NET 8 Runtime)
- Auto-scale: 2-10 instances based on CPU/memory
- Deployment slots: Production, Staging

**Database:**
- Azure Database for MySQL/PostgreSQL (General Purpose tier)
- Automated backups (7-day retention)
- Read replica for reporting queries

**Storage:**
- Azure Blob Storage (Standard, Hot tier initially)
- Lifecycle management rules (move to Cool after 90 days for closed projects)

**Caching:**
- Azure Cache for Redis (Basic C1 for start)

**Networking:**
- Azure Application Gateway (WAF enabled)
- Azure Front Door (CDN for static assets)
- Private endpoints for database

**Monitoring:**
- Application Insights (logging, tracing, metrics)
- Azure Monitor (alerts, dashboards)
- Log Analytics workspace

### Environment Setup

**Development:**
- Local MySQL/PostgreSQL
- Azurite (blob storage emulator)
- Redis container

**Staging:**
- Mirrors production but smaller scale
- Separate storage account
- Same database schema

**Production:**
- Multi-region (primary: India Central, DR: India South)
- Connection string in Azure Key Vault
- Managed identities (no secrets in code)

### Infrastructure as Code

Use Terraform or Bicep for reproducible infrastructure:
```hcl
resource "azurerm_storage_account" "legal_docs" {
  name                     = "legaldocs${var.environment}"
  resource_group_name      = azurerm_resource_group.main.name
  location                 = "Central India"
  account_tier             = "Standard"
  account_replication_type = "GRS"
  
  blob_properties {
    versioning_enabled = true
    delete_retention_policy {
      days = 30
    }
  }
}
```

## 10. Monitoring & Observability

### Key Metrics to Track

**Application:**
- Request rate, error rate, latency (P50, P95, P99)
- Document upload/download success rate
- Active users per company
- API endpoint performance

**Business:**
- Daily/monthly active companies
- Documents uploaded per day
- Storage usage per company
- License expiration warnings

**Infrastructure:**
- App Service CPU/memory utilization
- Database connections, query performance
- Blob storage operations (read/write IOPS)
- Redis hit/miss ratio