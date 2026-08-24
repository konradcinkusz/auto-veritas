# 02 — Gap analysis

Measured against the compliance checklist in
`architecture-standards/docs/architecture/00-REFERENCE-ARCHITECTURE.md` §3.
At session start every item was unmet by absence (empty repository); the
**After this session** column is the as-built evidence.

| # | Checklist item | After this session | Evidence |
|---|---|---|---|
| 1 | Declared in the AppHost with `WithReference`, `WaitFor`, `WithHttpHealthCheck` | Met | `src/AutoVeritas.AppHost/AppHost.cs` |
| 2 | Calls `AddServiceDefaults()` and `MapDefaultEndpoints()` | Met | `src/AutoVeritas.OffersService/Program.cs:8,27` |
| 3 | Exposes `/health` and `/alive`; platform check points at health | Met — platform checks target readiness (`/health/ready` for authservice, `/health` for offers) | `src/AutoVeritas.ServiceDefaults/Extensions.cs`, `flyio/*.fly.toml` |
| 4 | Emits OTLP traces, metrics and logs | Met in code; no collector endpoint configured in the Fly topology yet | `src/AutoVeritas.ServiceDefaults/Extensions.cs:40-72`; open row in [DEVIATIONS.md](DEVIATIONS.md) |
| 5 | Owns its database; no other service connects to it | Met — `offersdb` (OffersService) and `authdb` (authservice) as separate logical DBs, one owner each | `flyio/postgres.fly.toml`, `docker-compose.yml` |
| 6 | Schema by `MigrateAsync` from provider-specific migrations, in a hosted service | Met for OffersService (committed PostgreSQL migration set, applied after Kestrel starts). authservice ships no migrations upstream — its `authdb` uses `EnsureCreated`, recorded as D-8 | `src/AutoVeritas.OffersService.Migrations.PostgreSQL/`, `src/AutoVeritas.OffersService/Extensions/MigrationBackgroundService.cs` |
| 7 | All configuration from env; no secret in source; secret scanner in CI | Met | `.gitleaks.toml`, `.github/workflows/secret-scan.yml`, `flyio/SECRETS.md` |
| 8 | Exactly one service holds a signing key; others validate via its JWKS | Met — authservice holds the RS256 key; OffersService and the web middleware are verify-only | `src/AutoVeritas.ServiceDefaults/AuthenticationExtensions.cs`, `packages/web-kit/src/session.ts` |
| 9 | Kernel holds no entity/DTO/enum/seed/pricing/user-facing string; CI size check | Met — mechanical guards in CI (800-line ceiling + domain-type grep) | `.github/workflows/ci.yml` (mechanical-guards job) |
| 10 | Every optional integration has a working no-op or fallback | Met by scope: the system has no optional integrations yet; the OTLP exporter registers only when its endpoint is set | `src/AutoVeritas.ServiceDefaults/Extensions.cs:66-69` |
| 11 | Multi-stage Dockerfile; runtime major = TFM major; listens on `:8080`; non-root where possible | Met (web runs as `nextjs`; .NET base image default user recorded as-is, matching the estate's worked example) | `src/AutoVeritas.OffersService/Dockerfile`, `apps/web/Dockerfile` |
| 12 | One `fly.toml`; `min_machines_running = 1` if called in-request | Met — authservice pins one machine (JWKS issuer); offers scales to zero under a caller timeout that covers its cold start (recorded cost decision D-7) | `flyio/*.fly.toml`, `flyio/INFRASTRUCTURE-ANALYSIS.md` |
| 13 | Outbound `HttpClient`s carry the standard resilience handler with explicit timeouts | Met for .NET (`ConfigureHttpClientDefaults` + resilience handler); the BFF proxy carries its own 120 s abort → 504 | `src/AutoVeritas.ServiceDefaults/Extensions.cs:29-33`, `packages/web-kit/src/proxy.ts` |
| 14 | `Program.cs` is a manifest; wiring in `ServiceCollectionExtensions` | Met — 60 lines, every block one extension call | `src/AutoVeritas.OffersService/Program.cs` |
| 15 | Extension points are DI interfaces, not base classes | Met (`IMigrationCompletionSignal`, `IHealthCheck`, endpoint filters) | `src/AutoVeritas.OffersService/Extensions/` |
| 16 | Test project covers the logic-bearing layer | Met — 36 tests: freshness thresholds, auth/role enforcement, seeder idempotency; plus web-kit unit tests and the Playwright smoke tier | `tests/`, `packages/web-kit/test/`, `e2e/` |
| 17 | Built by the tag-driven workflow with path-based change detection | Met (workflow prepared; first live deploy pending the one-time `flyio` environment setup) | `.github/workflows/flyio.yml` |
| 18 | Architectural decisions recorded in `docs/` | Met | this directory |

Unmet or partially met items live as dated rows in [DEVIATIONS.md](DEVIATIONS.md);
deliberate divergences are decisions in [05-DECISIONS.md](05-DECISIONS.md).
