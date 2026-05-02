# Test Documentation — Lawgate

This folder contains SDET-ready test specifications for every feature area.
Each file targets a specific layer or feature and is structured so an AI agent in SDET mode can:

1. Read the spec and understand the test intent
2. Locate the relevant source files
3. Run the existing automated tests
4. Write additional tests following the patterns described

## Index

| File | Layer | What it covers |
|------|-------|----------------|
| [tier-storage-limits.md](tier-storage-limits.md) | Backend unit | `TierStorageLimits` options class — per-tier quota logic |
| [auth-registration-quota.md](auth-registration-quota.md) | Backend unit | `AuthService.RegisterAsync` — quota assignment on signup |
| [company-service.md](company-service.md) | Backend unit | `CompanyService` — storage DTO mapping, caching, update |
| [storage-bar-component.md](storage-bar-component.md) | Frontend unit | `StorageBar` React component — tier badge, progress bar, warnings |
| [storage-quota-integration.md](storage-quota-integration.md) | End-to-end / integration | Full upload-quota enforcement flow across the stack |

## How to run the tests

### Backend
```powershell
cd backend
dotnet test LegalDocSystem.UnitTests --verbosity normal
dotnet test LegalDocSystem.IntegrationTests --verbosity normal
```

### Frontend
```powershell
cd frontend
npm run test          # watch mode
npm run test:coverage # coverage report
```

## Testing conventions used in this project

- **Backend**: xUnit + FluentAssertions + NSubstitute; EF Core InMemory database per test class
- **Frontend**: Vitest + React Testing Library + `@testing-library/user-event`; module-level vi.mock for `apiService`
- Arrange / Act / Assert comments inside each test
- One assertion concept per test; descriptive names following `MethodName_Scenario_ExpectedBehaviour`
