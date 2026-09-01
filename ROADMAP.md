# Roadmap

Phased plan for auto-veritas, derived from the registers the repo already
keeps: [`docs/architecture/04-MIGRATION-PLAN.md`](docs/architecture/04-MIGRATION-PLAN.md)
(build steps 7–8), [`docs/architecture/DEVIATIONS.md`](docs/architecture/DEVIATIONS.md)
(unfixed gaps) and the ranked backlog in [`docs/ux/UI-UX.md`](docs/ux/UI-UX.md).
Nothing here was invented; every item traces to one of those.

Created 2026-08-31.

## State of the repo

Not an early-stage skeleton. The product is **built, tested and documented but
never deployed**: 52 backend tests, a Playwright smoke tier, four CI workflows,
a four-app Fly.io topology, an OWASP-mapped security review, and nine
architecture/UX documents. The gap is not "write the thing" — it is "run the
thing in production, then finish the ranked backlog".

## ⚠️ Two constraints that shape execution

**1. Milestones do not exist.** They could not be created: `gh` is unavailable
in the authoring session and `api.github.com` returns 403
(`"GitHub access is not enabled for this session"`) even with a token, and the
GitHub MCP server exposes no milestone tool. **This file is the phase
authority.** If milestones are created later, attach the issues below to them
and this section can go.

**2. Four issues cannot be executed by an agent at all.** They need the owner's
Fly.io account, a SendGrid key, or a repository settings toggle. They are
marked 🔒 **OWNER**. An autonomous execution loop must **skip** them — not
attempt them, not force-merge around them. They gate real work, so they are
listed first, but the agent-executable path starts at Phase 2.

---

## Phase 1 — Go live 🔒 (target: 2026-09-14)

Goal: the product exists on the public internet. Build-plan steps 7–8.
**Every issue here is owner-gated.**

| Issue | Title | Status |
|---|---|---|
| [#16](../../issues/16) 🔒 | Execute the first live Fly.io deploy | **open** — needs Fly account + org token |
| [#17](../../issues/17) | Enable GitHub code scanning so CodeQL stops failing | ✅ **done** — owner enabled it; CodeQL is green |
| [#18](../../issues/18) 🔒 | Turn on e-mail verification before opening registration | **open** — needs SendGrid key · depends on #16 |
| [#19](../../issues/19) 🔒 | Grant the agent account Admin, run first production verification | **open** — depends on #16 |

#16 blocks the rest of this phase and nothing else can proceed without it.

## Phase 2 — Product core (target: 2026-10-12)

Goal: deliver the two backlog items that carry the product's actual value.
First fully agent-executable phase.

| Issue | Title | Status |
|---|---|---|
| [#20](../../issues/20) | Monthly-budget inverse search | **open** — largest remaining item; see the must-not-regress note below |
| [#21](../../issues/21) | Offer detail view exposing source links | ✅ **done** — expandable panel, zero extra requests, SSRF proof intact |

**Must not regress:** #20 ranks by monthly cost. Any ranking that surfaces a low
installment without its total cost and balloon marking recreates the exact
deception this product exists to expose. Balloon exposure is structural, not
cosmetic.

## Phase 3 — Hardening (target: 2026-11-09)

Goal: drain the deviation register. Mostly small, independent, well-specified.

| Issue | Title | Status |
|---|---|---|
| [#22](../../issues/22) | Pin `superfly/flyctl-actions` to a SHA instead of `@master` | ✅ **done** — all 7 sites; no moving refs remain |
| [#23](../../issues/23) | Rate-limit the web BFF routes | ✅ **done** — partitions on a *verified* subject (D-15) |
| [#24](../../issues/24) | Add gitleaks as a committed pre-commit hook | ✅ **done** — verified it actually blocks a staged credential |
| [#26](../../issues/26) 🔒 | Configure an OTLP collector | **open** — needs a vendor choice · depends on #16 |
| [#31](../../issues/31) | Add the e2e core-regression tier | ✅ **done** — 5 flows, shared `storageState`, own CI job |
| [#25](../../issues/25) | Add a manual flyio-scale workflow | ✅ **done** — postgres excluded structurally *and* at runtime |

## Phase 4 — Auth & convenience (target: 2026-12-07)

Goal: the remaining ranked backlog. Lowest value density — reassess before
starting rather than executing on autopilot.

| Issue | Title | Status |
|---|---|---|
| [#27](../../issues/27) | Split freshness per data class | ⛔ **closed, not planned** — the agent verifies everything in one pass, so one date is accurate rather than simplified. Trigger recorded in `UI-UX.md`; reopen if partial passes ever start. |
| [#28](../../issues/28) | 2FA challenge flow in the login page | ✅ **done** — contract read from authservice `v0.3.1`; challenge kept HttpOnly (D-16) |
| [#29](../../issues/29) 🔒 | OAuth provider buttons from `/providers` | **open** — conditional on the owner configuring Google/GitHub; close as not-planned if that never happens |
| [#30](../../issues/30) | Shareable filter/sort state via the URL | ✅ **done** — URL state shipped; *named* saved filters split out |

---

## Must not regress

Structural things a change may not quietly break. Force-merging past a failure
in any of these is not acceptable:

- **The freshness/trust model.** Degrade-don't-hide ordering, per-type
  thresholds, `Estimated` marking, and balloon exposure are the product, not
  features of it.
- **The offer history diff-gate.** History snapshots only on real change and
  never on `verify` (D-13). Regressing it floods the timeline and makes the
  audit trail useless.
- **The SSRF proof.** `sourceUrl` is rendered, never fetched server-side. The
  security review asserts this with evidence; #21 must keep it true.
- **Secret discipline.** Nothing in a `fly.toml` `[env]` block is private.
  `.gitleaks.toml` and the CI job stay.
- **The migrations path.** Committed EF migrations, never `EnsureCreated` for
  the offers database.

## Where this stands (2026-09-01)

**8 of 16 issues closed** — 7 delivered, 1 closed as not planned. Every remaining
open issue is blocked on something only the owner can do, except **#20**:

- **#16, #18, #19, #26** — need a Fly account, a SendGrid key, or a telemetry
  vendor.
- **#29** — needs a decision about whether OAuth is wanted at all.
- **#20 (monthly-budget inverse search)** is the one piece of substantial
  product work still open and *is* buildable; it was held back because its
  design forks (server vs client, fixed vs parameterised term, and above all
  how balloon structures rank) produce materially different products.

The deviation register went from 8 rows to 4 over this pass.

## Velocity assumption

Solo project. Phases are spaced ~4 weeks; Phase 1 is shorter because its work is
minutes of human setup, not days of building. The commit history (40 commits in
about 29 hours across two days) reflects one intensive build session and is
**not** a sustainable rate to plan against — these dates assume ordinary
part-time pace, and should be corrected once there is a second real data point.
