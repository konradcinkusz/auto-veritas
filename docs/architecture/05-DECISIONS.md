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
