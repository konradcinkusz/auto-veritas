# 04 — Build plan (the greenfield reading of the migration plan)

Ordered, individually shippable steps; each carries the verification that it
worked. Steps 1–6 were executed in the 2026-08-24 delivery session.

| # | Step | Verification | Status |
|---|---|---|---|
| 1 | Repo baseline: build props, central package versions, editorconfig, gitleaks, templates, dependabot, CODEOWNERS, `.claude/settings.json` | `dotnet build` succeeds on an empty solution; gitleaks job green | **Done** |
| 2 | OffersService + kernel + Data/Migrations split + AppHost | `dotnet build AutoVeritas.slnx`; 36 tests green; `dotnet format --verify-no-changes` clean | **Done** |
| 3 | Frontend: pnpm workspace, web-kit, Next app, BFF routes, middleware, offers UI | `pnpm lint && pnpm test && pnpm build` green | **Done** |
| 4 | Composition: docker-compose with the pinned authservice image; key-generation scripts | Full-stack smoke test — register → login → view offers → agent write → freshness visible (run in-session against the live stack) | **Done** |
| 5 | Fly.io infra: four fly.tomls, SECRETS.md, INFRASTRUCTURE-ANALYSIS.md, tag-driven workflow, destroy workflow | Workflow syntax-checked; deploy gates and JWKS assertion reviewed against FLY-IO-DEPLOYMENT | **Done** (not yet exercised live — see step 7) |
| 6 | Documentation: this set, `docs/ux/UI-UX.md`, `docs/AGENT-GUIDE.md`, security review, truthful README | Docs match the built system; stale claims are review findings | **Done** |
| 7 | **First live deploy**: create the GitHub `flyio` environment (FLY_API_TOKEN + three root secrets per `flyio/SECRETS.md`), push tag `v0.1.0`, watch the ordered deploy, then demonstrate register → login → offers against `https://auto-veritas-web.fly.dev` | The public URL serves the journey; JWKS assertion passed in the workflow | **Remaining — needs the owner's Fly account** (one-time human setup, ~10 minutes) |
| 8 | Post-launch: grant the agent account `Admin` via the seeded SuperAdmin, run the first real verification pass through the API | `docs/AGENT-GUIDE.md` walkthrough succeeds against production | Remaining |

Backlog beyond the plan (ranked, each tied to its principle or gap) lives in
[`../ux/UI-UX.md`](../ux/UI-UX.md) for UI items and [DEVIATIONS.md](DEVIATIONS.md)
for architecture items.
