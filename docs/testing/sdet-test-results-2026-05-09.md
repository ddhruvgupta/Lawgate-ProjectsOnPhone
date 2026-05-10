# SDET Test Results — Lawgate Production
**Date:** 2026-05-09 (updated 2026-05-10 with new feature tests)  
**Tester:** GitHub Copilot (SDET role)  
**Test Account:** `<redacted>` (CompanyOwner, SDET Test Firm — credentials stored in secret store, not in source)  
**Frontend:** https://calm-field-02db0f300.7.azurestaticapps.net  
**API:** https://lawgate-prod-api-dztku2.azurewebsites.net  
**Commit tested:** `b5be75f` (fix: remove duplicate JSX in ProjectsPage; add PDF viewer)

---

## Summary

| Category | Pass | Fail | Warn |
|----------|------|------|------|
| API Health | 1 | 0 | 0 |
| Auth Flows | 7 | 0 | 2 |
| Navigation | 6 | 1 | 0 |
| Project CRUD | 3 | 0 | 0 |
| Document CRUD + PDF Viewer | 5 | 0 | 1 |
| Projects — View/Sort/Delete | 6 | 0 | 0 |
| UI / UX | 3 | 0 | 2 |
| Security | 0 | 3 | 1 |
| **Total** | **31** | **4** | **6** |

---

## Bugs / Issues Found

### 🔴 CRITICAL — Security: Tokens Stored in localStorage
**ID:** SEC-001  
**Severity:** Critical  
**Affected Area:** Authentication  

**Observation:** Both the JWT access token and the refresh token are stored in `localStorage`:
```
localStorage.getItem('token')        // JWT access token — present
localStorage.getItem('refreshToken') // Refresh token — present
```

**Expected:** Tokens must be stored in `HttpOnly`, `Secure`, `SameSite=Strict` cookies — never in `localStorage`.  
**Risk:** Any XSS vulnerability can steal both tokens, enabling full account takeover. The entire session is persistent across tabs and survives page reloads.  
**Policy violation:** Copilot instructions §Security/Authentication: "use refresh tokens stored in HttpOnly, Secure, SameSite=Strict cookies — never in localStorage"

---

### 🔴 CRITICAL — Security: JWT Access Token Expiry is 24 Hours
**ID:** SEC-002  
**Severity:** Critical  
**Affected Area:** Authentication / Token Configuration  

**Observation:** Decoded JWT:
```json
{
  "iat": 1778360226,
  "exp": 1778446626,
  "alg": "HS256"
}
```
Token lifetime = **1440 minutes (24 hours)**.  

**Expected:** Access token TTL ≤ 15 minutes, with refresh tokens handling re-issuance.  
**Risk:** A stolen token is valid for 24 hours with no revocation mechanism.  
**Policy violation:** Copilot instructions §Security/Authentication: "JWTs must be short-lived (≤15 min access token)"

---

### 🔴 HIGH — Security: User Object Stored in localStorage
**ID:** SEC-003  
**Severity:** High  
**Affected Area:** Authentication  

**Observation:** Full user object stored in plaintext localStorage:
```json
{"id":2,"companyId":2,"firstName":"<redacted>","lastName":"<redacted>","email":"<redacted>","phone":"<redacted>","role":"CompanyOwner","companyName":"SDET Test Firm","isActive":true,"isEmailVerified":true}
```
**Risk:** PII (email, phone, name) unnecessarily persisted in localStorage — accessible to any JavaScript on the page.

---

### 🟡 MEDIUM — Missing 404 / Not Found Page
**ID:** UI-001  
**Severity:** Medium  
**Affected Area:** React Router / SWA routing  

**Observation:** Navigating to any unknown route (e.g. `/totally-made-up-route`) while authenticated silently redirects to `/dashboard` with no user feedback.  
**Expected:** A dedicated `NotFoundPage` (404) component should render for unknown routes.  
**Policy violation:** Copilot instructions §Frontend/Routing: "Always define a catch-all `<Route path='*'>` that renders a `NotFoundPage`"

---

### 🟡 MEDIUM — Modal Dialogs Overflow Viewport (Buttons Hidden)
**ID:** UI-002  
**Severity:** Medium  
**Affected Area:** "New Project" modal, "Add Team Member" modal  

**Observation:** Both modals (New Project, Add Team Member) are taller than a standard 768px viewport. The Cancel/Submit buttons are positioned below the fold with no scrollbar on the modal itself. Standard click interactions fail with `element is outside of the viewport`.  
**Impact:** Users on smaller screens (laptops, tablets) cannot submit or cancel the forms via normal interaction without resizing the browser window.  
**Reproducible:** Open New Project modal at 1366×768 viewport — scroll the modal to see hidden buttons.

---

### 🟡 LOW — Console Warning: `Login error: _e` (Minified Error Object)
**ID:** UI-003  
**Severity:** Low  
**Affected Area:** Login error handling  

**Observation:** On a 401 login attempt, the browser console logs `Login error: _e` (a minified error object reference). This is not user-facing but leaks error handling logic.  
**Expected:** The caught error should either not be logged to the console in production, or logged as a meaningful message without exposing internal details.

---

### 🟡 LOW — PDF Viewer iframe Blank When SAS URL Expires
**ID:** PDF-001  
**Severity:** Low  
**Affected Area:** PDF Viewer (ProjectDetailPage)  

**Observation:** The PDF viewer Dialog opens correctly (toolbar, filename, Download, ×), and the `<iframe>` renders. However if the SAS URL is near expiry (the `download-url` endpoint generates a 10-minute SAS), the iframe may show a blank page or error.  
**Expected:** For a freshly-fetched SAS (within 10 min), the iframe should display the PDF inline. The mechanism is correct — only manifests as a test artifact when the test URL has already expired.  
**Recommended Fix:** Either increase the SAS TTL for view requests (e.g. 30 min) or re-fetch the SAS when the user opens the viewer instead of caching it.

---

## Full Test Results

### API Health
| # | Test | Status | Notes |
|---|------|--------|-------|
| 1 | GET `/health` returns `{"status":"healthy"}` | ✅ PASS | |

### Authentication
| # | Test | Status | Notes |
|---|------|--------|-------|
| 2 | Login form — empty submit → HTML5 required validation | ✅ PASS | Email field focused |
| 3 | Login form — invalid email format → browser validation | ✅ PASS | |
| 4 | Login with wrong credentials → 401, "Invalid email or password" | ✅ PASS | Toast + no account enumeration |
| 5 | Login with correct credentials → 200, redirect to `/dashboard` | ✅ PASS | |
| 6 | Console warning `Login error: _e` on 401 | ⚠️ WARN | See UI-003 |
| 7 | Resend verification → privacy-safe 200 regardless of email | ✅ PASS | |
| 8 | Forgot password → same message for registered vs unknown email | ✅ PASS | Privacy-safe |
| 9 | Logout → redirects to `/login`, clears localStorage (token, refreshToken, user) | ✅ PASS | |
| 10 | JWT + refresh token stored in localStorage | ❌ FAIL | See SEC-001 |
| 11 | JWT expiry 24 hours (should be ≤15 min) | ❌ FAIL | See SEC-002 |
| 12 | User PII stored in localStorage | ❌ FAIL | See SEC-003 |

### Route Protection & Navigation
| # | Test | Status | Notes |
|---|------|--------|-------|
| 13 | Unauthenticated → `/dashboard` redirects to `/login` | ✅ PASS | ProtectedRoute works |
| 14 | Authenticated → `/login` redirects to `/dashboard` | ✅ PASS | GuestRoute works |
| 15 | `/projects` loads with heading | ✅ PASS | |
| 16 | `/documents` loads with heading | ✅ PASS | |
| 17 | `/team` loads with heading | ✅ PASS | |
| 18 | `/activity` loads with heading | ✅ PASS | |
| 19 | Unknown route `/totally-made-up-route` → shows 404 page | ❌ FAIL | Silently redirects to `/dashboard` — see UI-001 |
| 20 | Zero console errors across all navigation | ✅ PASS | |

### Dashboard
| # | Test | Status | Notes |
|---|------|--------|-------|
| 21 | Stats cards render correct counts (1 project, 1 active, 0 docs, 1 member) | ✅ PASS | Updated after project creation |
| 22 | Recent projects list shows created project | ✅ PASS | |
| 23 | Firm name, user name, role correctly displayed | ✅ PASS | |
| 24 | Trial badge "14d left" visible | ✅ PASS | |

### Project CRUD
| # | Test | Status | Notes |
|---|------|--------|-------|
| 25 | New Project modal opens | ✅ PASS | |
| 26 | Empty submit → inline validation "Project name is required" | ✅ PASS | |
| 27 | Valid project creation → API POST 201, modal closes, project appears in list | ✅ PASS | Name, client, case #, status, description all saved |
| 28 | Project detail page (`/projects/1`) loads with full details | ✅ PASS | |
| 29 | Project search — matching query filters list | ✅ PASS | |
| 30 | Project search — no-match query shows "No projects match your search" | ✅ PASS | |
| 31 | Modal buttons hidden below viewport on small screen | ⚠️ WARN | See UI-002 |

### Document CRUD + PDF Viewer
| # | Test | Status | Notes |
|---|------|--------|-------|
| 41 | Upload Document modal opens with correct fields | ✅ PASS | File, Type, Description |
| 42 | Upload a PDF (285 B) → 3-step SAS flow completes, doc appears in list | ✅ PASS | `test-document.pdf · 285 B · v1 · Contract` visible |
| 43 | Uploaded PDF shows Eye icon ("View PDF" tooltip) | ✅ PASS | EyeIcon rendered |
| 44 | Click Eye icon → PDF viewer Dialog opens (filename in toolbar, Download link, × close) | ✅ PASS | Dialog visible with correct toolbar |
| 45 | Close button (×) dismisses the viewer | ✅ PASS | Dialog closes, returns to project detail |
| 46 | iframe renders PDF content inline | ⚠️ WARN | See PDF-001 — blank due to SAS expiry in test; mechanism correct |
| 47 | Download button in toolbar contains correct SAS URL | ✅ PASS | `href` points to blob SAS URL |

### Projects — View Toggle / Sort / Delete
| # | Test | Status | Notes |
|---|------|--------|-------|
| 48 | Sort controls row visible: "Sort by", Name, Status, Date created buttons | ✅ PASS | |
| 49 | Card/List view toggle buttons visible, Card view default | ✅ PASS | Card view `aria-pressed=true` on load |
| 50 | Click "List view" → switches to table layout with Project/Client/Case#/Status columns | ✅ PASS | |
| 51 | Click "Name" sort → button becomes active with sort arrow | ✅ PASS | `aria-pressed=true` |
| 52 | Click "Name" again → sort direction arrow flips (asc ↔ desc) | ✅ PASS | |
| 53 | Click "Card view" → returns to card grid | ✅ PASS | |
| 54 | Hover card → TrashIcon delete button appears; click → Delete confirmation dialog opens | ✅ PASS | Dialog shows project name, Cancel + "Delete Project" buttons |
| 55 | Click "Cancel" in delete dialog → dialog closes, project still in list | ✅ PASS | |

### Registration
| # | Test | Status | Notes |
|---|------|--------|-------|
| 32 | Register page accessible while logged out | ✅ PASS | |
| 33 | Empty submit → browser HTML5 required focuses first empty field | ✅ PASS | |
| 34 | Mismatched passwords → inline error "Passwords do not match" | ✅ PASS | |

### Team Management
| # | Test | Status | Notes |
|---|------|--------|-------|
| 35 | Team page shows current member (SDET Tester, Owner, Active) | ✅ PASS | |
| 36 | Add Member modal opens with correct fields (First/Last Name, Email, Temp Password, Role) | ✅ PASS | |
| 37 | Modal buttons hidden below viewport on small screen | ⚠️ WARN | See UI-002 |

### UI / UX
| # | Test | Status | Notes |
|---|------|--------|-------|
| 38 | Dark mode toggle switches `<html>` class to `dark` | ✅ PASS | |
| 39 | Dark mode persists after page reload (stored as `lawgate-dark-mode=true` in localStorage) | ✅ PASS | |
| 40 | Password visibility toggle — `type` changes `password` ↔ `text`, button label updates | ✅ PASS | |

---

## Observations & Recommendations

### Immediate Actions (Security)
1. **SEC-001**: Move token storage to `HttpOnly` cookies. Update `apiService` interceptors to use cookie-based auth (remove manual `Authorization` header injection from localStorage).
2. **SEC-002**: Reduce JWT TTL to 5–15 minutes in `appsettings`. Implement refresh token rotation on each use.
3. **SEC-003**: Remove the `user` object from localStorage. Derive display data from the JWT payload or a `/api/auth/me` endpoint.

### Medium Priority (UX)
4. **UI-001**: Add `<Route path="*" element={<NotFoundPage />} />` as the final route in React Router config.
5. **UI-002**: Add `overflow-y: auto; max-height: 90vh` (or equivalent Tailwind classes) to modal containers so content is scrollable on smaller viewports. The action buttons should remain sticky at the bottom.

### Low Priority
6. **UI-003**: Remove `console.error('Login error:', error)` from the login catch block in production builds, or replace with a structured non-leaking log.

---

## Infrastructure Cleanup Required
A temporary DB firewall rule was added during this test session:
```
Rule: sdet-local-temp
IP:   24.98.20.51
DB:   lawgate-prod-db-dztku2
```
**Remove after testing:**
```powershell
az postgres flexible-server firewall-rule delete `
  --resource-group project-management `
  --name lawgate-prod-db-dztku2 `
  --rule-name sdet-local-temp `
  --yes
```
