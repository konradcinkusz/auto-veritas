# Security review — 2026-08-24 (pre-exposure)

Method per `architecture-standards/docs/guides/SECURITY-REVIEW.md`: fixed OWASP
category skeleton, three outcomes per category (finding / clean-and-stated /
N/A-with-proof). **This is static analysis, not a penetration test** — a pentest
against the live deployment remains open work.

## Categories

### A01 Broken access control — applicable, one finding (F-1) + positive findings

Positive: deny-by-default holds — the entire product API sits behind two
`MapGroup`s (`RequireAuthorization()` for reads, `RequireRole("Admin","SuperAdmin")`
for writes) declared in `Program.cs:33-40`; there is no anonymous product
surface. Endpoint × role matrix:

| Endpoint | Anonymous | User (viewer) | Admin/SuperAdmin (agent) |
|---|---|---|---|
| `GET /health`, `/alive`, `/health/ready` | ✓ | ✓ | ✓ |
| `GET /api/v1/car-offers*`, `/financing-offers*`, `/meta/*` | 401 | ✓ | ✓ |
| `POST/PUT/DELETE /api/v1/car-offers*`, `/financing-offers*`, `…/verify` | 401 | 403 | ✓ |

Verified by tests (`CarOfferEndpointsTests`, `FinancingOfferEndpointsTests`:
anonymous → 401, viewer write → 403). No authorization by e-mail comparison
anywhere (`grep -rn "email ==" src/ packages/ apps/` → no authorization sites).

### A02 Cryptographic failures — applicable, clean

RS256 only; exactly one key holder (authservice); consumers hold zero key
material (`AuthenticationExtensions.cs`, `session.ts`). Cookies: HttpOnly,
SameSite=Strict, Secure outside dev, single set/clear definition
(`packages/web-kit/src/cookies.ts`), asserted by unit tests. Tokens never touch
web storage (`grep -rn "localStorage\|sessionStorage" apps/ packages/` → none).
Password hashing is authservice's (ASP.NET Identity PBKDF2 defaults) — recorded
here as inherited, not re-audited.

### A03 Injection — applicable, clean

All data access is EF Core parameterized LINQ; no raw SQL
(`grep -rn "FromSqlRaw\|ExecuteSqlRaw" src/` → none). React encodes at render;
no `dangerouslySetInnerHTML` (`grep -rn dangerouslySetInnerHTML apps/` → none);
no user-supplied markdown is rendered.

### A04 Insecure design — applicable, clean by product shape

The write path is a single trusted agent; viewers are read-only. Trust
degradation (stale/estimated labelling) is server-computed so a compromised
client cannot present stale data as fresh to others.

### A05 Security misconfiguration — applicable, one finding (F-2) + positive

Positive: Swagger is off outside Development unless explicitly enabled
(`SwaggerExtensions.cs:34`); CORS origins are named per environment, never
wildcard (`fly.toml` / compose / appsettings.Development.json); secrets ship
via the platform store with gitleaks in CI.

### A06 Vulnerable components — applicable, mitigated continuously

Pinned versions everywhere (central package management, pinned image tags);
dependabot on nuget/npm/actions/docker; CodeQL on both languages.

### A07 Identification & authentication failures — applicable, residual risk (R-1)

Login/registration/rate-limiting/lockout are authservice's, configured not
rewritten. The UI never distinguishes "no such user" from "wrong password".
Refresh rotation is single-use with replay-detection (family revocation);
the BFF serializes refresh calls per token to avoid tripping it.

### A08 Software & data integrity — applicable, clean

Images pinned by version; build-once-deploy-many; lockfiles committed; no
dynamic code loading.

### A09 Logging & monitoring — partial (see deviation register)

OTel wiring is in the kernel; no collector configured in production yet.
Recorded as an open deviation (P15) rather than silently accepted.

### A10 SSRF — N/A with proof

The only server-side fetches go to configuration-derived bases (the candidate
ladder + `AUTH_URL`), never to user-supplied URLs: `grep -rn "fetch(" packages/
apps/` shows every target built from `process.env` values plus a path the proxy
whitelists by prefix (`proxy.ts:24-38` rejects unknown prefixes with 404).
User-supplied `sourceUrl` values are stored and rendered as text, never fetched.

### Path/file handling — N/A with proof

No user-supplied file paths or uploads exist (`grep -rn "File\.\|IFormFile"
src/` → none beyond framework internals). Preventive: if offer images are ever
added, apply the guide's whitelist spec.

## Findings

**F-1 — Proxy forwards arbitrary methods to the offers service**
`packages/web-kit/src/proxy.ts` · Issue: the catch-all forwards
GET/POST/PUT/PATCH/DELETE for any signed-in user; only the service's role check
stops a viewer's write. Current code: routing table + method re-export. Risk:
**Low**. Attack scenario: a signed-in viewer crafts
`POST /api/proxy/offers/api/v1/car-offers`; the OffersService answers 403 — the
attack fails, but the write attempt consumes service resources and the defense
is single-layered. Impact: none today (role check holds, verified by test);
defense-in-depth gap. Recommendation: accepted as-is — the service **is** the
security boundary (FRONTEND-BFF's own rule) and a method whitelist in the proxy
would drift against future admin tooling. Better: revisit if a second backend
joins the routing table.

**F-2 — Registration is open while e-mail verification is off**
`docker-compose.yml` / fly env (no `SendGrid__ApiKey`) · Issue: anyone reaching
the site can register with an unverified address and read the offers. Risk:
**Medium** (confidentiality of the dataset is the product's value). Attack
scenario: a scraper registers `bot@x.test`, logs in, pulls both tables via the
proxy. Impact: dataset disclosure. Recommendation: before sharing the public
URL beyond trusted people, configure SendGrid + `Auth__RequireConfirmedEmail=true`
(one `fly secrets set` + one env var). Better: an invite-only registration mode
upstream in authservice. Tracked in the deviation register and R-1.

## Ledger

| Priority | Item | Evidence | Context (what makes it cheap) | Status |
|---|---|---|---|---|
| P2 (week 1) | F-2 e-mail verification before public sharing | compose/fly env | authservice supports it end-to-end; one secret + one env var | OPEN |
| P3 (two weeks) | OTLP collector endpoint | DEVIATIONS row 1 | kernel already exports when the env var appears | OPEN |
| P3 (two weeks) | BFF-level rate limiting | DEVIATIONS row 5 | Fly `hard_limit` already caps per-machine concurrency | OPEN |
| P4 (long-term) | F-1 proxy method whitelist | proxy.ts | trivial diff if a second backend lands | NOT PLANNED (accepted) |

**Blocks deployment:** nothing — for a deployment shared with trusted viewers.
**Before public sharing:** F-2.

## Residual risks (deliberate)

- **R-1**: open registration + no e-mail verification until F-2 is done —
  acceptable while the URL is shared privately; wrong the moment it is posted
  anywhere public.
- Password-hash parameters and auth rate limits are inherited from authservice
  v0.3.1 and re-audited only on image bumps.
- The four recurring launch blockers from the guide, checked: production CORS ✓
  (named fly.dev origins in the tomls), e-mail confirmation ✗ (= F-2), rate
  limiting partial (deviation row 5), token storage consistent ✓ (single
  frontend, cookies only).
