# Authentication: Identity Provider Options Design Document

**Status:** Draft for review
**Date:** March 2026
**Context:** Lawgate is a multi-tenant B2B SaaS platform for law firms. Each law firm is a `Company` (tenant), and users belong to a company with roles (CompanyOwner, Admin, User). The platform is targeting Azure for deployment.

---

## Problem Statement

The current auth implementation is hand-rolled: BCrypt password hashing, custom JWT issuance, refresh tokens stored in the `Users` table. This approach carries real cost:

- **We own the security surface.** Password storage, token rotation, brute-force protection, session management — every vulnerability is ours.
- **Missing features by default.** MFA, SSO, social login, email verification, password reset — each requires custom work.
- **Compliance burden.** Law firms have strict requirements (SOC 2, GDPR, potentially HIPAA). A certified IdP transfers a large portion of that audit burden.
- **Scaling pain.** Refresh token state in the `Users` table is already causing friction; a dedicated token store makes it worse, not better.

The question is not *whether* to offload auth, but *to which provider* and *how much* to offload.

---

## Requirements

| Requirement | Priority |
|---|---|
| Multi-tenant: users are scoped to a company/tenant | Must-have |
| Role-based access (CompanyOwner, Admin, User) | Must-have |
| Email/password login | Must-have |
| JWT tokens consumable by our .NET API | Must-have |
| MFA support | Must-have |
| Password reset / email verification | Must-have |
| Azure deployment compatibility | Must-have |
| Self-service user registration (law firm signs up, creates company) | Must-have |
| Audit log of login events | Should-have |
| SSO / SAML for enterprise law firms (their own AD) | Nice-to-have |
| Social login (Google, Microsoft) | Nice-to-have |
| Per-tenant branding (custom login page per firm) | Nice-to-have |
| GDPR / right to erasure | Should-have |

---

## Options

### Option 1: Azure Entra External ID (formerly Azure AD B2C)

Microsoft's customer identity platform (CIAM). Designed for external users signing up for your SaaS — the exact pattern Lawgate uses.

**How it works:**
- Each user registers in your Entra External ID tenant
- Your app redirects to Microsoft's hosted login page for auth flows
- On success, Entra issues a JWT (ID token + access token) to your frontend
- Your .NET API validates the JWT using Entra's public keys (no DB lookup needed)
- Custom claims (company ID, role) can be added via custom attributes or an API connector that calls your backend on login

**Multi-tenancy fit:**
Entra External ID has a concept of "organisations" (currently in preview), but for Lawgate's model the simpler approach is: store `CompanyId` and `Role` as custom user attributes in Entra. On login, these get embedded in the JWT your API receives. Your `Company` table still exists in your DB; Entra just handles identity.

**Pros:**
- Azure-native — fits the existing deployment plan exactly
- Microsoft manages uptime, security patches, SOC 2 Type II, ISO 27001
- Built-in: MFA, SSPR, email verification, brute-force lockout, conditional access
- OIDC/OAuth2 standard — .NET has first-class support (`Microsoft.Identity.Web`)
- SSO with Microsoft/Google out of the box
- Enterprise SSO: a law firm can federate their own Azure AD (their employees use their corporate credentials)
- Free tier: 50,000 MAU free

**Cons:**
- Vendor lock-in to Microsoft
- Custom user flows (registration, profile edit) require configuration in Azure portal
- Custom claims require setting up an "API connector" (HTTP call to your backend) — extra infrastructure
- `CompanyId` assignment at registration needs a webhook/connector flow (user signs up → your API creates the company → writes CompanyId back to the user's profile)
- Per-tenant branding is available but requires configuration per tenant
- "Organisations" multi-tenancy is still in preview (May 2026)

**Cost:**
Free up to 50,000 MAU. After that: ~$0.016/MAU/month. For a law firm SaaS this is negligible — you'd need 50k active users before paying anything.

**Migration effort:** Medium. Existing `PasswordHash`, `RefreshToken`, `ResetToken` fields on `User` become redundant. `User.Email` becomes the link between your DB record and Entra's object ID. You'd add `ExternalId` (Entra's object ID) to your `User` entity.

---

### Option 2: Azure Entra ID (Workforce / B2B)

Standard Azure AD — designed for your *own employees* or for B2B scenarios where partner organisations already have Azure AD.

**How it works:**
Each law firm would need their own Azure AD tenant, or users would be invited as guests into your tenant.

**Why this is the wrong fit:**
Entra ID is designed for organisations managing their own employees. It expects users to be in an Azure AD tenant (corporate accounts). Law firm staff are external customers, not your employees. The self-service signup flow is not a native concept here.

**Verdict: Not recommended.** Use Entra External ID (Option 1) for external customers. Entra ID is relevant only if you later add an internal admin portal for your own team.

---

### Option 3: Auth0 (by Okta)

The most developer-friendly IdP on the market. Auth0 offers a similar feature set to Entra External ID but with a better DX and more flexibility.

**How it works:**
Same OIDC/OAuth2 flow as Option 1. Auth0 issues JWTs, your API validates them. Custom claims via "Actions" (serverless JS functions that run on login events).

**Multi-tenancy fit:**
Auth0 has an explicit multi-tenancy model called "organisations." Each law firm becomes an Auth0 Organisation. Users are members of organisations. Your JWT can include `org_id` as a claim. This maps more cleanly to Lawgate's data model than Entra External ID's custom attributes approach.

**Pros:**
- Best-in-class developer experience — clear docs, great dashboard
- Native multi-tenancy via "organisations" (not in preview — GA and mature)
- Actions (custom JS) are more flexible than Entra's API connectors
- Per-organisation branding out of the box
- SSO federation: each law firm can connect their IdP (Google Workspace, their own AD)
- Excellent .NET SDK (`Auth0.AspNetCore.Authentication`)

**Cons:**
- Not Azure-native — adds a non-Microsoft vendor to your stack
- Pricing is significantly higher at scale: free up to 7,500 MAU, then ~$23/month for up to 1,000 MAU on the B2C plan (paid per MAU, adds up fast)
- Enterprise SSO (SAML, custom IdP per organisation) requires the Enterprise plan (~$800+/month)
- Data residency: Auth0 US region by default; EU region available but must be configured

**Cost:**
Free: 7,500 MAU. $23/month: up to 1,000 MAU on B2C plan. Enterprise features (SAML SSO per org) jump significantly in price. For a growing law firm SaaS with potentially large firms, this could become expensive.

**Migration effort:** Medium. Same as Option 1 — replace custom auth with OIDC flow.

---

### Option 4: Keep Custom Auth (Improve Current)

Enhance the current hand-rolled approach: move refresh tokens to a dedicated table, add MFA via TOTP, add email verification, add proper session management.

**Pros:**
- Full control — no vendor dependency
- No per-MAU cost
- No external network call on every token validation

**Cons:**
- You own every vulnerability. BCrypt timing attacks, JWT algorithm confusion, token replay — these are all your problem.
- MFA, SSO, email verification are all custom builds
- Compliance: auditors will scrutinise your custom implementation; certified IdPs get a pass
- Law firms may specifically require "do you use a certified auth provider" — custom auth fails this question
- Ongoing maintenance burden as auth best practices evolve

**Verdict: Not recommended for production.** Acceptable for the current development phase, but should not go to production with real law firm data.

---

## Comparison Matrix

| | Entra External ID | Auth0 | Custom |
|---|---|---|---|
| **Azure-native** | ✅ | ❌ | ✅ |
| **Multi-tenancy** | ⚠️ (custom attrs; Orgs in preview) | ✅ (Organisations, GA) | ✅ (your DB) |
| **MFA** | ✅ built-in | ✅ built-in | ❌ build it |
| **SSO / social login** | ✅ | ✅ | ❌ build it |
| **Enterprise SAML per org** | ✅ | 💰 Enterprise plan | ❌ build it |
| **Free tier** | 50,000 MAU | 7,500 MAU | ∞ |
| **Cost at scale** | Low (~$0.016/MAU) | High (~$0.023/MAU+) | Infra only |
| **DX / docs** | Good | Excellent | N/A |
| **Per-org branding** | ✅ | ✅ | ❌ build it |
| **Compliance (SOC2 etc.)** | ✅ | ✅ | ❌ |
| **GDPR / erasure** | ✅ | ✅ | ❌ build it |
| **.NET integration** | ✅ first-class | ✅ good | ✅ |
| **Migration effort** | Medium | Medium | None |

---

## Recommendation

**Primary: Azure Entra External ID**

Given that Lawgate is already targeting Azure, Entra External ID is the natural choice:
- It fits the deployment architecture (same tenant, same portal, Managed Identity for any API connectors)
- Free up to 50,000 MAU covers years of growth
- Microsoft handles SOC 2, GDPR, security patches
- Enterprise law firms that run Microsoft 365 (most do) can use their existing corporate credentials via federation — a real selling point

**The one weakness** is multi-tenancy. Entra's "Organisations" feature (which would give Auth0-like per-org isolation) is still in preview. The workaround today is storing `CompanyId` as a custom user attribute and injecting it into the JWT via an API connector. This is slightly more complex at setup but works well in practice.

**If the Organisations feature is critical now:** Auth0 is the alternative. The developer experience is better and multi-tenancy is mature. The trade-off is cost (especially for enterprise SSO) and adding a non-Azure vendor.

---

## What Changes in the Codebase

### What gets removed
- `PasswordHash`, `RefreshToken`, `RefreshTokenExpiry`, `ResetToken`, `ResetTokenExpiry` from the `User` entity (and their migrations)
- `AuthService` (RegisterAsync, LoginAsync, RefreshTokenAsync)
- `JwtTokenService` (we validate Entra-issued JWTs instead of generating our own)
- `AuthController` (register, login, refresh endpoints — these move to Entra's hosted flows)

### What gets added
- `ExternalId` (string) on `User` — stores the Entra object ID (`oid` claim); this is the stable link between your DB user record and the Entra identity
- `Microsoft.Identity.Web` NuGet package on the API
- An API connector endpoint (a new controller action) that Entra calls on user sign-up to create the `Company` and `User` record in your DB and return the `CompanyId` claim
- Frontend: MSAL.js (`@azure/msal-react`) replaces the custom `AuthContext`

### What stays the same
- JWT validation middleware (just points at Entra's JWKS endpoint instead of a local key)
- Role-based `[Authorize(Roles = "...")]` attributes on controllers — these read from JWT claims as before
- All business logic, services, and domain entities (except auth fields on User)
- The `Company` table and tenant-scoping logic

### Registration flow change
Today: user fills form → your API creates Company + User + issues JWT.
With Entra: user fills form on Entra-hosted page → Entra calls your API connector → your API creates Company record, returns `CompanyId` → Entra embeds `CompanyId` in the JWT → user redirected back to app with token.

---

## Migration Path (if approved)

1. **Phase 1** — Set up Entra External ID tenant, configure user flows (sign-up, sign-in, password reset), test locally with MSAL dev config. No DB changes yet.
2. **Phase 2** — Add `ExternalId` to `User`, implement the API connector endpoint, migrate `AuthController` to the new flow.
3. **Phase 3** — Remove deprecated auth fields from `User`, remove `AuthService`/`JwtTokenService`, clean up migrations.
4. **Phase 4** — Replace frontend `AuthContext` with `@azure/msal-react`. Test all protected routes.
5. **Phase 5** — Load test, pen test, compliance review.

Phases 1–2 can run in parallel with existing auth (feature flag the new login flow) so there is no hard cutover until Phase 3.

---

## Open Questions for Review

1. **Timeline:** Is this a Phase 6 item or does it block production launch?
2. **Organisations preview:** Are we comfortable depending on Entra Organisations (preview), or should we accept the custom-attributes workaround for now?
3. **Enterprise SSO:** Do we expect law firms to want to federate their own AD in the near term? If yes, this strengthens the Entra case (it's free in Entra; expensive in Auth0).
4. **Data residency:** Do we need EU data residency for GDPR? Both Entra and Auth0 support it but must be configured upfront — changing region later is painful.
5. **Existing users:** Once we go live with custom auth and then migrate, we'd need a migration flow (send existing users a "set password" email via Entra). This is only an issue if we onboard real users before migrating.
