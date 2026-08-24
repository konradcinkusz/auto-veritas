# Deviation register

Deviations from `architecture-standards/docs/architecture/00-REFERENCE-ARCHITECTURE.md`
that are **still unfixed**. Dated per row; a fixed row is deleted; a deliberately
accepted divergence moves to [05-DECISIONS.md](05-DECISIONS.md).

| Since | Deviation | Principle | Plan |
|---|---|---|---|
| 2026-08-24 | No OTLP collector is configured in the Fly topology, so the kernel's telemetry exports nowhere in production (the exporter no-ops without `OTEL_EXPORTER_OTLP_ENDPOINT`). | P15 | Stand up or point at a collector (e.g. a Grafana Cloud endpoint) and set the env var in the fly.tomls. |
| 2026-08-24 | The E2E suite is the smoke tier only (chromium, 6 flows). No core-regression or nightly extended tier exists yet, and no PR preview environments — CI smoke runs against compose-per-run instead. | P13 | Add the core tier when the second real feature lands; preview environments per PR-PREVIEW-ENVIRONMENTS when review traffic justifies them. |
| 2026-08-24 | First live Fly.io deployment has not been executed — the workflow, tomls and secrets runbook are prepared but the `flyio` GitHub environment (FLY_API_TOKEN + root secrets) requires the owner's Fly account. | P7/P12 | Build-plan step 7 in [04-MIGRATION-PLAN.md](04-MIGRATION-PLAN.md). |
| 2026-08-24 | E-mail verification is off (no SendGrid key configured), so registration returns tokens immediately. Acceptable while viewers are invited by the owner; wrong for open registration. | — (SECURITY-REVIEW launch-blocker list) | Configure `SendGrid__ApiKey` + `Auth__RequireConfirmedEmail=true` on the authservice app before opening registration publicly; row also in the security review's residual risks. |
| 2026-08-24 | Web BFF routes carry no rate limiting of their own (authservice rate-limits auth endpoints per IP; the OffersService rate-limits per user). A hostile client can hammer the Next.js server itself. | SERVICE-API-PATTERNS (partial-coverage failure mode) | Add a limiter at the edge (Fly's `hard_limit` partially covers this today) or in the BFF routes. |
