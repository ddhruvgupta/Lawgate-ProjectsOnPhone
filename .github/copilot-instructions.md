# Copilot Instructions

## Code Quality
- Write clean, readable, and maintainable code
- Use design patterns and best practices appropriate for the problem at hand
- Use descriptive, intention-revealing variable and function names
- Apply OOP principles where appropriate; avoid over-engineering
- Follow TDD principles — write unit tests for all new features and bug fixes
- Consider edge cases and potential pitfalls before implementing

## Tech Stack Conventions
- Backend: C# .NET with Clean Architecture (Domain → Application → Infrastructure → API)
- Frontend: React + TypeScript + Tailwind CSS
- Follow CQRS in the Application layer — Commands and Queries handled via MediatR
- Use the repository pattern in Infrastructure; never access `DbContext` directly from controllers
- DTOs belong in the Application layer; never expose domain entities directly from the API
- Use FluentValidation for all request validation

## API & Error Handling
- All API responses follow a consistent envelope: `{ data, error, statusCode }`
- Use ProblemDetails (RFC 7807) for error responses
- Handle all exceptions in global middleware — not in controllers
- Validate inputs at the API boundary; trust data inside the domain

## Security
- Never log sensitive data (passwords, tokens, PII)
- Always validate and sanitize user input before processing
- Use parameterized queries; never concatenate SQL strings
- Follow least-privilege principle for roles and permissions

## Frontend
- Colocate component tests with the component file (`Component.test.tsx`)
- Encapsulate business logic in custom hooks; keep components presentational
- Prefer `const` arrow functions for components
- Use React Query for server state; avoid storing server data in local React state

## Logging & Observability
- Serilog is the logging provider (configured via `UseSerilog()` in `Program.cs`)
- Inject `ILogger<T>` (from `Microsoft.Extensions.Logging`) in all services and controllers — never use `Log.*` static calls outside of `Program.cs`
- Use structured logging with named properties: `_logger.LogInformation("Fetched {Count} documents for user {UserId}", count, userId)`
- Include correlation IDs on all log entries
- Log levels: `LogDebug` for dev detail, `LogInformation` for significant events, `LogWarning` for recoverable issues, `LogError` for failures with exceptions
- Never log full exception stack traces at `Information` level — use `LogError(ex, "message")`

## Git & Branching
- Branch naming: `feature/<issue-number>-short-description`, `fix/<issue-number>-short-description`, `refactor/<issue-number>-short-description`
- Never commit directly to `main` or `develop`
- Squash commits before merging to `main`
- Every commit message must reference the issue number (e.g., `Fix login error #123`)

## Issue & Documentation Tracking
- Use GitHub Issues to track all bugs, features, and refactors
- For each issue, create a folder: `docs/issue-<number>-short-description/` with a `.md` file covering:
  - Problem description, steps to reproduce, expected vs actual behaviour, relevant logs
- For each feature, create a folder: `docs/feature-<name>/` with a `.md` file covering:
  - Feature description, user stories, design mockups, implementation trade-offs
- For each refactor, create a folder: `docs/refactor-<name>/` with a `.md` file covering:
  - Motivation, affected areas, risks, and test/validation plan
- Link documentation files to their corresponding GitHub Issues

## PR Process & Human-in-the-Loop
- Every PR must reference a GitHub Issue
- **Before opening a PR, always pause and ask the user to review the planned changes** — do not open PRs autonomously
- PR description must include: what changed, why it was changed, and how to test it
- Do not suggest merging a PR — the user decides when to merge
- Never push to `main` directly; always go through a PR
- Do not merge with failing tests or lint errors
- After implementation is complete, summarise what was done and prompt the user to review before any git push or PR creation
- For destructive operations (migrations, schema changes, config changes), explicitly flag them in the PR description and wait for user confirmation

## AI Context Files
- The `docs/ai-context/` folder contains deep project context for each layer — read the relevant file before working on a task:
  - `docs/ai-context/main.md` — overall architecture, tech decisions, project goals
  - `docs/ai-context/backend.md` — .NET API structure, patterns, critical files
  - `docs/ai-context/frontend.md` — React/TypeScript setup, component patterns
  - `docs/ai-context/database.md` — schema, EF migrations, seeding strategy
- Keep these files up to date as the project evolves — they are the source of truth for AI agents working on this codebase
- For task-specific human-readable reference, consult the relevant doc in `docs/`:
  - `docs/architecture/` — system design, backend, frontend, database, schema changelog
  - `docs/guides/` — quick start, environment setup, Docker, deployment, GitHub setup
  - `docs/testing/` — test suites, how to run tests, test results
  - `docs/tracking/implementation-checklist.md` — current phase and what's done
  - `docs/index.md` — full documentation map