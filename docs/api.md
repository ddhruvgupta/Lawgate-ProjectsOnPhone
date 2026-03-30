# API Reference

Base URL (local): `http://localhost:5059/api`
Swagger UI (local, development only): `http://localhost:5059/swagger`
Health check (no auth): `GET http://localhost:5059/health`

---

## Authentication

All endpoints except `POST /api/auth/register`, `POST /api/auth/login`, `POST /api/auth/refresh`, and `POST /api/auth/validate` require a JWT in the `Authorization` header:

```
Authorization: Bearer <access_token>
```

### Standard response envelope

**Success:**
```json
{
  "success": true,
  "data": { },
  "message": "..."
}
```

**Error:**
```json
{
  "success": false,
  "error": "...",
  "errors": ["validation error 1", "validation error 2"],
  "timestamp": "2026-03-27T10:00:00Z"
}
```

---

## Auth (`/api/auth`)

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| POST | `/api/auth/register` | None | Register a new company and owner user |
| POST | `/api/auth/login` | None | Login and receive JWT + refresh token |
| POST | `/api/auth/refresh` | None | Exchange a refresh token for a new JWT |
| POST | `/api/auth/validate` | None | Validate whether a JWT is still valid |
| GET | `/api/auth/me` | Required | Return the current user's profile |

### POST /api/auth/register

```json
// Request
{
  "companyName": "Acme Law Firm",
  "firstName": "John",
  "lastName": "Doe",
  "email": "john@acmelawfirm.com",
  "phone": "+91-9876543210",
  "password": "SecurePass123!",
  "confirmPassword": "SecurePass123!"
}

// Response 200
{
  "success": true,
  "data": {
    "token": "<jwt>",
    "refreshToken": "<refresh_token>",
    "expiresAt": "2026-03-28T10:00:00Z",
    "user": {
      "id": 1,
      "email": "john@acmelawfirm.com",
      "firstName": "John",
      "lastName": "Doe",
      "role": "CompanyOwner",
      "companyId": 1,
      "companyName": "Acme Law Firm"
    }
  }
}
```

### POST /api/auth/login

```json
// Request
{ "email": "john@acmelawfirm.com", "password": "SecurePass123!" }

// Response 200 — same shape as register response
```

### POST /api/auth/refresh

```json
// Request
{ "refreshToken": "<refresh_token>" }

// Response 200 — same shape as register response
```

### GET /api/auth/me

```json
// Response 200
{
  "success": true,
  "data": {
    "id": 1,
    "email": "john@acmelawfirm.com",
    "firstName": "John",
    "lastName": "Doe",
    "role": "CompanyOwner",
    "companyId": 1
  }
}
```

---

## Company (`/api/companies`)

All endpoints require authentication. Operations are scoped to the caller's company.

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/api/companies/me` | Required | Get the current user's company |
| PUT | `/api/companies/me` | Required | Update the current user's company |

### GET /api/companies/me

```json
// Response 200
{
  "success": true,
  "data": {
    "id": 1,
    "name": "Acme Law Firm",
    "email": "contact@acmelawfirm.com",
    "phone": "+91-9876543210",
    "subscriptionTier": "Trial",
    "subscriptionEndDate": "2026-04-10T00:00:00Z",
    "storageUsedBytes": 0,
    "storageQuotaBytes": 10737418240
  }
}
```

---

## Users (`/api/users`)

All endpoints require authentication and operate within the caller's company.

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/api/users` | Required | List all users in the company |
| GET | `/api/users/{id}` | Required | Get a specific user |
| POST | `/api/users` | Required | Invite / create a new user |
| POST | `/api/users/{id}/toggle-status` | Required | Activate or deactivate a user |

### POST /api/users

```json
// Request
{
  "firstName": "Jane",
  "lastName": "Smith",
  "email": "jane@acmelawfirm.com",
  "phone": "+91-9876543211",
  "role": "User",
  "password": "TempPass123!"
}
```

---

## Projects (`/api/projects`)

All endpoints require authentication. Projects are scoped to the caller's company.

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/api/projects` | Required | List all projects for the company |
| GET | `/api/projects/{id}` | Required | Get project details |
| POST | `/api/projects` | Required | Create a new project |
| PUT | `/api/projects/{id}` | Required | Update project details |
| DELETE | `/api/projects/{id}` | Required | Soft-delete a project |

### POST /api/projects

```json
// Request
{
  "name": "Contract Review — Tata Industries",
  "description": "Annual supply agreement review",
  "clientName": "Tata Industries",
  "caseNumber": "CASE-2026-042",
  "startDate": "2026-03-27T00:00:00Z"
}
```

---

## Documents (`/api/documents`)

Documents use a two-step upload flow: get a SAS upload URL, then confirm after the client uploads directly to Azure Blob Storage.

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| POST | `/api/documents/upload-url` | Required | Get a SAS URL to upload a file |
| POST | `/api/documents/{id}/confirm` | Required | Confirm upload is complete |
| GET | `/api/documents/{id}/download-url` | Required | Get a SAS URL to download a file |
| GET | `/api/documents/{id}` | Required | Get document metadata |
| GET | `/api/documents/project/{projectId}` | Required | List documents for a project |
| DELETE | `/api/documents/{id}` | Required | Soft-delete a document |

### POST /api/documents/upload-url

```json
// Request
{
  "projectId": 1,
  "fileName": "contract-v2.pdf",
  "contentType": "application/pdf",
  "fileSizeBytes": 204800
}

// Response 200
{
  "success": true,
  "data": {
    "documentId": 42,
    "uploadUrl": "https://...blob.core.windows.net/...?sas=...",
    "expiresAt": "2026-03-27T10:15:00Z"
  }
}
```

---

## Test (`/api/test`)

Development-only controller. Only available when `ASPNETCORE_ENVIRONMENT=Development`.

| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/test/database` | Verify DB connectivity and return row counts |
| POST | `/api/test/seed-company` | Create a test company |
| POST | `/api/test/seed-user` | Create a test user |
| GET | `/api/test/companies` | List all companies |
| GET | `/api/test/users` | List all users |
| DELETE | `/api/test/clear-data` | Delete all seeded test data |

---

## Error Codes

| HTTP Status | Meaning |
|-------------|---------|
| 400 | Validation failure or business rule violation |
| 401 | Missing or expired JWT |
| 403 | Authenticated but not authorised for this resource |
| 404 | Entity not found (or not visible to caller's tenant) |
| 500 | Unhandled server error (details in server logs) |

All errors return the standard envelope with `success: false`.
