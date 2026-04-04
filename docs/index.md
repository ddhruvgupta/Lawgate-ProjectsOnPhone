# Lawgate Documentation Index

> All project documentation lives in this folder. Start here.

## Quick Links

| I want to... | Go to |
|---|---|
| Get the project running fast | [quick-start.md](quick-start.md) |
| Look up a command | [quick-reference.md](quick-reference.md) |
| Set up my local environment | [environment-setup.md](environment-setup.md) |
| Understand the architecture | [architecture.md](architecture.md) |
| See what's done / what's next | [implementation-checklist.md](implementation-checklist.md) |
| Deploy to Azure | [deployment.md](deployment.md) |

---

## All Documents

### Getting Started
| File | Description |
|------|-------------|
| [quick-start.md](quick-start.md) | One-page guide to get the stack running in minutes |
| [quick-reference.md](quick-reference.md) | Cheat sheet — common commands, ports, credentials |
| [environment-setup.md](environment-setup.md) | Full local dev environment setup (prerequisites, config) |

### Architecture & Design
| File | Description |
|------|-------------|
| [architecture.md](architecture.md) | System design, data model, technology decisions |
| [backend.md](backend.md) | Backend API — controllers, services, migrations, config |
| [frontend.md](frontend.md) | Frontend — pages, components, routing, state management |
| [database.md](database.md) | Database operations, recreation script, backup/restore |
| [schema-changelog.md](schema-changelog.md) | Migration history (v1.0 – v1.4) |

### Operations
| File | Description |
|------|-------------|
| [docker-setup.md](docker-setup.md) | Docker Compose services, commands, troubleshooting |
| [testing-guide.md](testing-guide.md) | Manual testing procedures for all major flows |
| [test-results.md](test-results.md) | Infrastructure validation test results |
| [github-setup.md](github-setup.md) | Repository setup and GitHub project board |
| [deployment.md](deployment.md) | Full Azure deployment guide with cost estimates |

### Progress Tracking
| File | Description |
|------|-------------|
| [implementation-checklist.md](implementation-checklist.md) | Phase-by-phase checklist — **current phase: 7** |

### AI Context Files
> Context files for AI assistants — loaded at the start of each session.

| File | Scope |
|------|-------|
| [ai-context/main.md](ai-context/main.md) | Full project overview |
| [ai-context/backend.md](ai-context/backend.md) | Backend-specific patterns and conventions |
| [ai-context/frontend.md](ai-context/frontend.md) | Frontend-specific patterns and conventions |
| [ai-context/database.md](ai-context/database.md) | Database conventions and recovery procedures |

### Archive
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
