# Infrastructure analysis

The topology, its sizing, and the cost reasoning — written down so the next change
argues against numbers, not vibes.

## Topology

| App | Image | Public | min machines | Why |
|---|---|---|---|---|
| `auto-veritas-postgres` | `postgres:17-alpine` | **no** — 6PN only | 1 (stateful, `--ha=false`) | Two logical DBs (`authdb`, `offersdb`), one owner each |
| `auto-veritas-authservice` | `ghcr.io/konradcinkusz/authservice:v0.3.1` | yes | **1** | Issuer + JWKS: on the synchronous validation path of every service. The one named call that forces the pin: `web-kit` middleware and OffersService both fetch `/.well-known/jwks.json` on key-cache expiry while a user waits. |
| `auto-veritas-offers` | built from `src/AutoVeritas.OffersService/Dockerfile` | yes | 0 | Only in-request caller is the web BFF, whose 120 s proxy timeout ≫ ~10–20 s .NET cold start; the first request after idle is slow, not failed. Pinning would cost ~1.9 $/mo for a hobby-scale product with one writer. |
| `auto-veritas-web` | built from `apps/web/Dockerfile` | yes | 0 | Browser-entered only; nothing calls it in-request. |

## What runs when idle

One postgres machine and one authservice machine (~2 × shared-cpu-1x/512 MB).
Everything else stops. Estimated idle cost: ≈ 4 $/month at current Fly pricing.

## The cheaper option and its cost

Scaling authservice to zero would save ~1.9 $/month and break the first request
after every idle window for *all* users at once (JWKS fetch during token
validation), plus slow every login by a cold boot. Rejected.

## Off the table

- No `force_https = false`, anywhere.
- No shared database user across services beyond the single instance owner —
  and no second service ever connecting to another service's logical DB.
- No public listener on postgres, ever. Laptop access is `fly proxy 15432:5432`.
- No scaling the postgres app past one machine (a second machine gets a second,
  empty volume, not a replica).

## Region

`waw` (Warsaw): closest region to the owner; the audience is a single household,
not a fleet — latency to Spain is irrelevant next to cold-start behavior.

## Naming

Single environment today, so apps are `auto-veritas-<service>`. If a second
environment ever appears, it takes the standard `auto-veritas-<service>-<env>`
suffix scheme and this file records the split.
