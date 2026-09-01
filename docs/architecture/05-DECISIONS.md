# 05 — Decisions

Taken and rejected, with reasons — so the next session doesn't re-litigate.
Dated 2026-08-24 unless noted.

## D-1 — Playbook mode: MODERNIZE-shaped, read as a greenfield build plan

Greenfield is not an explicit playbook mode. REVIEW presumes code that roughly
follows the architecture (there was none); RECOVER's archaeology has no subject.
MODERNIZE's five documents degrade gracefully to a build plan, so that shape was
used and this reading recorded.

## D-2 — JWT issuer is an environment value; consumers use `MetadataAddress`, never `Authority`

authservice's OIDC discovery document reports `issuer` as the configured
`Jwt__Issuer` *string* and has no authorization/token endpoints, so strict
Authority-derived validation (issuer == discovery URL) cannot be used. Consumers
therefore configure `Jwt:MetadataAddress` explicitly plus an explicit
`ValidIssuer`/`ValidAudience`. Per IDENTITY-AND-ACCOUNTS the deployed issuer is
the instance's public URL (`https://auto-veritas-authservice.fly.dev`); local
environments use the stable identifier `https://auth.auto-veritas.local` —
consistent across all three components within each environment. Rejected:
hardcoding one issuer for all environments (breaks the public-URL rule in
production) and deriving JWKS from the issuer (breaks in dev where the
identifier is not fetchable).

## D-3 — Viewer/editor authorization on authservice platform roles

Writes require the `Admin` or `SuperAdmin` platform role from authservice's
fixed role set; ordinary registered users carry no role claim and are read-only,
which is exactly the product's viewer model. Rejected: a custom "Agent" role
(authservice seeds a fixed set; inventing one means forking the identity
service, which is out of bounds) and an API-key side channel (a second
credential system next to the identity provider is the anti-pattern P5 exists
to prevent).

## D-4 — Freshness is computed server-side, thresholds published as API

The service computes freshness statuses and exposes the thresholds at
`/api/v1/meta/freshness-policy`; the UI renders what the server enforces.
Rejected: client-side thresholds (two sources of truth that drift — the exact
failure the product exists to fight).

## D-5 — Stale and expired offers are degraded, never hidden

Removing dead rows would make a table hole indistinguishable from "model not on
the market". Stale rows are labelled ("zweryfikuj u dealera"), expired offers get
an explicit expired badge, and the default sort ranks by freshness. Estimated
values are chip-labelled "szacunek" instead of being presented as facts.

## D-6 — Version pins with reasons

- **authservice `v0.3.1`**: newest published release at build time; pinned per
  IDENTITY-AND-ACCOUNTS (never `:latest`).
- **Next.js 15.5**: Next 16 renames `middleware.ts` to `proxy.ts` and is weeks
  old; FRONTEND-BFF's shapes (and its middleware rules) are written against the
  `middleware.ts` convention. Revisit deliberately, not by a dependabot merge.
- **Swashbuckle 9.x**: 10.x moves to Microsoft.OpenApi 2.0's breaking API; the
  estate's worked example is on 9.x.
- **`.slnx`**: the .NET 10 default solution format; CI references it directly.

## D-7 — OffersService scales to zero

P7's diagnostic: for every in-request call A→B, pin B or make A's timeout exceed
B's cold start. The only in-request caller is the web BFF with a 120 s abort;
cold start is ~10–20 s. Taken as a cost decision (arithmetic in
`flyio/INFRASTRUCTURE-ANALYSIS.md`); authservice, by contrast, pins one machine
because every token validation path can fetch its JWKS synchronously.

## D-8 — authservice's `authdb` runs `SchemaMode=EnsureCreated`

authservice publishes no migration set; `Migrate` would require generating one
from its source, which this repo must not vendor. EnsureCreated bootstraps the
empty `authdb` correctly; the cost is that image upgrades across schema-changing
releases need the DDL from authservice's `docs/schema/upgrade/` applied out of
band. That operational duty is recorded here and in `flyio/authservice.fly.toml`.
Friction to raise upstream (an issue, not a patch): shipping provider migrations
with the image would remove this class of work for every consumer.

## D-9 — OffersService requires an explicit connection string (no InMemory fallback)

P4's table shows an InMemory fallback when no connection string is present; the
estate's worked example (authservice) instead fails fast naming the setting, and
this repo follows the worked example: a silently empty in-memory catalogue looks
exactly like a broken deployment. One-command dev still holds — the AppHost and
compose both provision postgres. Tests use SQLite through the factory (not the
EF InMemory provider, which ignores the unique-slug index the tests verify).

## D-10 — Middleware lets an expired access token through when a refresh cookie exists

FRONTEND-BFF's middleware clears and redirects on any invalid token. Applied
literally with authservice's 60-minute access tokens, every navigation after an
hour bounces through `/login` even though the refresh cookie could mint a new
session silently. The middleware therefore rejects only requests that could not
possibly hold a session (no cookies, or an unverifiable token with no refresh
cookie); expired-but-refreshable requests pass through and the BFF routes
refresh server-side. The security boundary is unchanged — every API call is
still verified by the services; the middleware remains UX (the guide's own
framing). Refresh rotation is serialized per token in-process; with more than
one web machine a raced rotation can trip authservice's replay detection and
sign the user out — accepted at `min_machines_running = 0..1`, revisit before
scaling the web app horizontally.

## D-11 — Database provider plumbing lives in the Data project, not the kernel

P2's table lists `AddDatabaseContext` among kernel concerns; the worked example
keeps provider selection in the Data assembly, where the design-time factory
(`dotnet ef`) can reach it without referencing web plumbing. The worked
example's shape was followed; the kernel stays EF-free and far under its
ceiling.

## D-12 — No organizations, no 2FA UI, no OAuth buttons in v1

authservice supports all three; the product needs none of them for a
single-owner comparison site with invited viewers. Login handles the
`requiresTwoFactor` response with an honest message instead of a broken flow.
All three are ranked in the UI/UX backlog, none blocks the core journey.

## D-13 — Offer history is keyed by the offer's id, snapshotted on `PUT` only, viewer-readable

Resolves UI/UX backlog item 3. Four choices, each rejected alternative recorded:

- **Keyed by `CarOfferId`/`FinancingOfferId` (the entity's GUID), never by
  `Slug` or name.** Both `PUT` handlers already let an agent change `Slug`
  (with a uniqueness check); keying history by a value that can be edited
  would strand or misattribute rows the moment that happens. The id never
  changes across an offer's lifetime.
- **A snapshot is written only when the pre-`PUT` and post-`PUT` value fields
  actually differ** (compared via record equality on `CarOfferSnapshot` /
  `FinancingOfferSnapshot`). A no-op re-send of an unchanged payload — which
  the agent workflow does not forbid — would otherwise pollute the timeline
  with identical rows.
- **`POST .../verify` never writes a history row.** It is deliberately the
  cheap, values-unchanged re-verification operation (AGENT-GUIDE.md §4); a
  full snapshot on every re-check would drown real price/rate changes in
  noise. A source-name/URL correction belongs in a `PUT`, not a `verify`.
- **Read access matches the rest of the read API — any signed-in viewer, not
  admin-only** — the point raised was "can the [end] user see this
  accessibly", and the history endpoints (`GET .../{id}/history`) sit under
  the same `viewerApi` group as everything else. Capped at the 100 most
  recent entries per offer (no pagination) since real history for a single
  offer stays short; a deeper archive was rejected as premature.

Deleting an offer (`DELETE`) cascades onto its history: `DELETE` is reserved
for entries that were wrong, and a wrong entry's history is wrong too — it
does not survive as an orphaned row nothing can look up again.

## D-14 — Dependabot version updates are off; dependency currency is a deliberate, batched review (2026-08-25)

`.github/dependabot.yml` is deleted. The repo baseline
([04-MIGRATION-PLAN.md](04-MIGRATION-PLAN.md) step 1) listed Dependabot as a
standard control, so this is a deliberate divergence from that baseline,
recorded here rather than in [DEVIATIONS.md](DEVIATIONS.md) because it is a
choice, not an unfixed gap.

**Why.** The weekly bot produced eight open PRs against a repo with one
maintainer, and triaging them cost more than the currency was worth. Of the
eight, three were actively harmful and two of those would have broken `main`
had they been merged on the bot's say-so:

- `node:22-alpine` → `26-alpine` broke the web image outright. Node stopped
  distributing Corepack in v25 (`(SEMVER-MAJOR) build: stop distributing
  Corepack`, nodejs/node#57617) and `apps/web/Dockerfile` opens with
  `RUN corepack enable`. Node 26 is also not an LTS line.
- `Swashbuckle.AspNetCore` 9.0.6 → 10.2.3 did not compile (`CS0234` on
  `Microsoft.OpenApi.Models`) — it was re-proposing a pin whose comment in
  `Directory.Packages.props` explains exactly why it is pinned.
- `pnpm/action-setup` v4 → v6 passed CI but targets pnpm 11; this repo is on
  pnpm 10.33.0, and v6's own README directs pnpm-10 users to stay on the
  older line. It would have added a pnpm-11-bootstrap-then-downgrade step
  with a known open upstream failure mode, for no capability gain.

A bot that is right five times out of eight, where two of the three misses are
build-breaking, is a review queue, not an automation.

**What replaces it.** Dependency currency becomes an explicit task: check
`dotnet list package --outdated`, `pnpm outdated`, and the pinned action/image
versions when touching the relevant area, and bump deliberately in a batch. The
action versions were brought current in this same pass (checkout v7,
setup-node v7, setup-dotnet v6, gitleaks-action v3, xunit.runner.visualstudio
4.0.0).

**What this does NOT turn off.** Deleting the config stops Dependabot *version
updates* only. Dependabot *alerts* and *security updates* are repository
settings (Settings → Code security), not file-driven, and are deliberately left
on — a CVE in a dependency is worth an interrupt in a way a routine minor bump
is not. Rejected: keeping the config with `open-pull-requests-limit: 0`, which
pauses the same way but leaves a file implying the control is active.

## D-15 — The BFF limiter partitions on a *verified* subject, and is per-instance by design (2026-09-01)

Closes the SERVICE-API-PATTERNS partial-coverage deviation: authservice limited
its auth endpoints and the OffersService limited per user, but the Next.js
process fanning requests out to both was unprotected.

**Partition key is the verified JWT subject, falling back to client IP.**
Decoding the access cookie without checking its signature would have been
cheaper and is the obvious implementation — and it is a bypass, not a limiter:
anyone can mint an endless supply of partitions by varying `sub` in an unsigned
token, and the flood still lands on the process being protected. Verification
is a local check against the already-cached JWKS, and the request is about to
make a network hop anyway, so the cost is noise. Anything that does not verify
— anonymous, expired, forged — keys on the client IP, which the caller cannot
choose.

**Per-user, not per-IP, for signed-in traffic**, so users behind one NAT do not
share a budget. Auth routes (`login`, `register`) are unauthenticated by
definition and key on IP directly rather than paying for a verification that
cannot succeed; `logout`, `session` and `consents` are deliberately ungated,
since rate-limiting a user out of ending their own session is a worse outcome
than the traffic it would prevent.

**The 429 body matches the kernel's exactly** (`{ error, retryAfter }` plus a
`Retry-After` header, per `RateLimitingExtensions`), so a client sees one
contract whether it was limited at the edge or at the service.

**Counters are in-process and this is a stated limit, not an oversight.** Two
Fly machines mean two independent budgets and a restart forgets everything. It
defends what the deviation described — one client hammering one process — and
does nothing against a distributed flood, which belongs at the edge/CDN.
Rejected: a shared store (Redis) for a correct global quota, which adds a
stateful dependency to a stack that currently has exactly one, for a threat
this product does not yet face.


## D-16 — The 2FA challenge token lives in an HttpOnly cookie, not in the page (2026-09-01)

Completes backlog item 4. authservice's login returns `200` with either a
`TokenResponse` or a `TwoFactorRequiredResponse { requiresTwoFactor,
challengeToken, expiresIn: 300 }`; the second is completed at
`POST /api/v1/auth/2fa/login` with the challenge plus either an authenticator
code or a single-use recovery code. (Contract read from authservice at tag
`v0.3.1` — the version compose and Fly both pin — not assumed.)

**The BFF keeps the challenge; the browser never sees it.** Before this change
the challenge body was passed straight through to the login page. authservice
describes the token as "useless for anything except completing this login", and
that is true — but it is still a bearer artifact that finishes an
authentication, and this BFF's entire premise is that such artifacts live in
HttpOnly cookies where page scripts cannot reach them. Passing it to the page
would have made the second factor the one credential in the system handled
differently from every other. The page is told only `{ requiresTwoFactor: true }`.

Consequences taken deliberately:

- **The cookie's `maxAge` matches authservice's `ExpiresIn` (300s)** rather than
  a rounder number, so the browser and the server stop trusting it together.
- **The challenge is read from the cookie and never from the request body.** A
  challenge the page can supply is a challenge an attacker can supply.
- **Exactly one second factor is sent.** authservice checks `code` first and
  only falls through to `recoveryCode` when it is absent, so forwarding both
  would silently spend a single-use recovery code on a request the
  authenticator could have served.
- **A dead challenge is cleared on 401.** Leaving it set turns one expired
  attempt into a loop the user cannot escape without clearing cookies by hand;
  the page drops back to the password step and says so.

Not built: enabling/disabling 2FA from this UI. authservice exposes
`/2fa/enable`, `/verify`, `/disable` and `/recovery-codes`, but this product's
frontend is deliberately read-only apart from authentication itself, and
enrolment is account management rather than sign-in.
