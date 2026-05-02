# SDET Spec — StorageBar Component

## Overview

`StorageBar` is a React component rendered in the sidebar of the app layout for all non-platform-admin users.
It fetches `GET /api/companies/me` via the `useCompany` hook (React Query) and renders:

- A tier badge (colour-coded per tier)
- A responsive progress bar (blue → amber → red as usage rises)
- Human-readable "X of Y used" text
- A trial countdown ("Nd left") when the tier is Trial and `subscriptionEndDate` is set
- An upgrade warning when usage ≥ 90%

## Source files

| File | Purpose |
|------|---------|
| `frontend/src/components/StorageBar.tsx` | Component under test |
| `frontend/src/components/StorageBar.test.tsx` | Automated tests |
| `frontend/src/hooks/useCompany.ts` | React Query hook |
| `frontend/src/services/api.ts` | `getMyCompany()` method |
| `frontend/src/types/index.ts` | `CompanyInfo` interface |

## Run
```powershell
cd frontend
npm run test -- StorageBar
```

## Test cases

### TC-SB-01 — Skeleton renders while loading
- **Arrange** `getMyCompany` never resolves (pending promise)
- **Assert** `.animate-pulse` elements visible; no "of" text present (no storage text)
- **File** `StorageBar > renders skeleton while loading`

### TC-SB-02 — Trial badge shown for Trial tier
- **Assert** text "Trial" rendered
- **File** `StorageBar > shows Trial tier badge when tier is Trial`

### TC-SB-03 — Basic badge shown for Basic tier
- **Assert** text "Basic" rendered

### TC-SB-04 — Professional badge shown for Professional tier
- **Assert** text "Professional" rendered

### TC-SB-05 — Enterprise badge shown for Enterprise tier
- **Assert** text "Enterprise" rendered

### TC-SB-06 — Human-readable storage text
- **Arrange** `storageUsedBytes = 500 MB`, `storageQuotaBytes = 1 GB`
- **Assert** "500 MB" and "1 GB" both visible
- **File** `StorageBar > displays used and total storage in human-readable format`

### TC-SB-07 — 0 B shown when no storage used
- **Assert** "0 B" visible
- **File** `StorageBar > shows 0 B when storage is empty`

### TC-SB-08 — Progressbar has correct aria-valuenow (50%)
- **Arrange** 512 MB of 1 GB used
- **Assert** `role="progressbar"` with `aria-valuenow="50"`, `aria-valuemin="0"`, `aria-valuemax="100"`
- **File** `StorageBar > renders a progressbar with correct aria-valuenow`

### TC-SB-09 — aria-valuenow never exceeds 100 when usage > quota
- **Arrange** used = 2 GB, quota = 1 GB
- **Assert** `aria-valuenow <= 100`
- **File** `StorageBar > clamps aria-valuenow to 100 when used exceeds quota`

### TC-SB-10 — Upgrade warning shown at ≥ 90% usage
- **Arrange** 950 MB of 1 GB used (92.8%)
- **Assert** "Storage almost full" text visible
- **File** `StorageBar > shows upgrade warning when storage is >= 90%`

### TC-SB-11 — Upgrade warning NOT shown below 90%
- **Arrange** 500 MB of 1 GB used (48.8%)
- **Assert** "Storage almost full" absent
- **File** `StorageBar > does not show upgrade warning when storage is below 90%`

### TC-SB-12 — Trial countdown shows days remaining
- **Arrange** `subscriptionTier = "Trial"`, `subscriptionEndDate = now + 10 days`
- **Assert** "10d left" visible
- **File** `StorageBar > shows days-left countdown for Trial tier`

### TC-SB-13 — Trial countdown shows 0d when expired
- **Arrange** `subscriptionEndDate = yesterday`
- **Assert** "0d left" visible

### TC-SB-14 — No countdown for non-Trial tiers
- **Arrange** `subscriptionTier = "Basic"`, end date in the future
- **Assert** no "d left" text
- **File** `StorageBar > does not show days-left for non-Trial tiers`

### TC-SB-15 — No countdown when subscriptionEndDate is null
- **Assert** no "d left" text even for Trial tier

### TC-SB-16 — Component renders nothing when data is null
- **Arrange** `getMyCompany` resolves with `null`
- **Assert** container is empty
- **File** `StorageBar > renders nothing when query returns no data`

## Visual behaviour (manual verification)

| Usage % | Bar colour | Warning text |
|---------|-----------|-------------|
| 0–74% | Blue (`bg-blue-500`) | None |
| 75–89% | Amber (`bg-amber-400`) | None |
| ≥ 90% | Red (`bg-red-500`) | "Storage almost full — upgrade your plan" |

## Accessibility checklist

- [ ] `role="progressbar"` present
- [ ] `aria-valuenow`, `aria-valuemin`, `aria-valuemax` all set
- [ ] `aria-label` on progressbar describes usage percentage
- [ ] Trial countdown has `aria-label` for screen readers
- [ ] Skeleton elements are `aria-hidden="true"`

## Integration with Layout

`StorageBar` is rendered inside `Layout.tsx` between the nav section and the user section.
It is hidden for platform admin users (`canAccessPlatformAdmin === true`).

To test this boundary:
1. Log in as a regular `CompanyOwner` → storage bar visible in sidebar
2. Log in as `PlatformAdmin` → storage bar absent
