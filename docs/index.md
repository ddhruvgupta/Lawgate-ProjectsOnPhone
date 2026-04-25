# Lawgate Documentation Index

> All project documentation lives in this folder. Start here.

## Quick Links

| I want to... | Go to |
|---|---|
| Get the project running fast | [guides/quick-start.md](guides/quick-start.md) |
| Look up a command | [guides/quick-reference.md](guides/quick-reference.md) |
| Set up my local environment | [guides/environment-setup.md](guides/environment-setup.md) |
| Understand the architecture | [architecture/architecture.md](architecture/architecture.md) |
| See what's done / what's next | [tracking/implementation-checklist.md](tracking/implementation-checklist.md) |
| Deploy to Azure | [guides/deployment.md](guides/deployment.md) |

---

## All Documents

### `guides/` — Getting Started & Operations
| File | Description |
|------|-------------|
| [quick-start.md](guides/quick-start.md) | One-page guide to get the stack running in minutes |
| [quick-reference.md](guides/quick-reference.md) | Cheat sheet — common commands, ports, credentials |
| [environment-setup.md](guides/environment-setup.md) | Full local dev environment setup (prerequisites, config) |
| [docker-setup.md](guides/docker-setup.md) | Docker Compose services, commands, troubleshooting |
| [github-setup.md](guides/github-setup.md) | Repository setup and GitHub project board |
| [deployment.md](guides/deployment.md) | Full Azure deployment guide with cost estimates |

### `architecture/` — Design & Technical Reference
| File | Description |
|------|-------------|
| [architecture.md](architecture/architecture.md) | System design, data model, technology decisions |
| [backend.md](architecture/backend.md) | Backend API — controllers, services, migrations, config |
| [frontend.md](architecture/frontend.md) | Frontend — pages, components, routing, state management |
| [database.md](architecture/database.md) | Database operations, recreation script, backup/restore |
| [schema-changelog.md](architecture/schema-changelog.md) | Migration history (v1.0 – v1.4) |

### `testing/` — Testing
| File | Description |
|------|-------------|
| [testing.md](testing/testing.md) | Test suites overview and how to run them |
| [testing-guide.md](testing/testing-guide.md) | Manual testing procedures for all major flows |
| [test-results.md](testing/test-results.md) | Infrastructure validation test results |

### `tracking/` — Progress
| File | Description |
|------|-------------|
| [implementation-checklist.md](tracking/implementation-checklist.md) | Phase-by-phase checklist — **current phase: 7** |

### `ai-context/` — AI Agent Context Files
> Deep context for AI agents — load the relevant file before starting work on that layer.

| File | Scope | Companion doc |
|------|-------|---------------|
| [ai-context/main.md](ai-context/main.md) | Full project overview | [architecture/architecture.md](architecture/architecture.md) |
| [ai-context/backend.md](ai-context/backend.md) | Backend patterns and conventions | [architecture/backend.md](architecture/backend.md) |
| [ai-context/frontend.md](ai-context/frontend.md) | Frontend patterns and conventions | [architecture/frontend.md](architecture/frontend.md) |
| [ai-context/database.md](ai-context/database.md) | Database conventions and recovery | [architecture/database.md](architecture/database.md) |

### `archive/`
> Historical event-log documents kept for reference. Not maintained going forward.

| File | Description |
|------|-------------|
| [archive/SETUP-COMPLETE.md](archive/SETUP-COMPLETE.md) | Initial scaffold completion summary |
| [archive/AUTHENTICATION-COMPLETE.md](archive/AUTHENTICATION-COMPLETE.md) | JWT authentication implementation summary |
| [archive/FRONTEND-SETUP.md](archive/FRONTEND-SETUP.md) | Frontend initialization summary |
| [archive/AUTH-TEST-RESULTS.md](archive/AUTH-TEST-RESULTS.md) | Authentication system test results |

---

## Project Status (as of 2026-04-04)

```
Backend   █████████████████████  98% — All core features + tests complete
Frontend  ████████████████░░░░░  75% — Core pages done; enhancements pending
Database  █████████████████████ 100% — Schema finalized (7 migrations)
DevOps    ████████████░░░░░░░░░  55% — Docker ready; CI/CD and Azure pending
```

**Currently in**: Phase 7 — Azure Deployment

**Not started**: Phase 8 (CI/CD), Phase 9 (Production Launch)
