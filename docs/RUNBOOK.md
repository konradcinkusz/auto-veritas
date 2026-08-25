# Runbook — running auto-veritas

Two ways to run this system, in one place: **locally** (compose or Aspire) and
**deployed** (Fly.io, driven by a git tag). Everything below is what the repo
actually does today — the compose file, `.github/workflows/flyio.yml` and
`flyio/SECRETS.md` are the sources of truth if they ever disagree.

| | Local — compose | Local — Aspire | Deploy — Fly.io |
|---|---|---|---|
| Command | `docker compose up --build` | `dotnet run --project src/AutoVeritas.AppHost` | `git push origin v0.1.0` |
| Needs | Docker | Docker + .NET 10 SDK + Node/pnpm | GitHub environment + 4 secrets + a Fly account |
| Runs the real images | yes | no (offers + web run from source) | yes |
| Use it for | evaluating, e2e, mirroring prod | the .NET/React dev loop | production |

---

## Prerequisites

| Tool | Version | Where the version is pinned |
|---|---|---|
| Docker + Compose | any current | — |
| .NET SDK | 10.0.100+ | `global.json` (`rollForward: latestFeature`) |
| Node.js | 22 | `.github/workflows/ci.yml` |
| pnpm | 10.33.0 | `package.json` → `packageManager` |

Compose alone needs only Docker. The other two are for the Aspire path, the
test suites, and the e2e run.

---

## Option A — local via Docker Compose

The full stack, closest to production: real PostgreSQL, the published
authservice image, and both of our services built from their real Dockerfiles.

### 1. One-time — generate the local RS256 signing key

```bash
./scripts/generate-jwt-key.sh          # Windows: scripts/generate-jwt-key.ps1
```

Writes `certs/jwt-signing.dev.pem` (gitignored) and compose mounts it read-only
at `/keys`. The script refuses to overwrite an existing key.

This key is local-only. **Never reuse it for a deployment** — the deployed key
is generated separately and lives only in the platform secret store.

### 2. Start

```bash
docker compose up --build
```

First boot initialises two databases (`authdb`, `offersdb`) via
`infra/compose/init-databases.sh`, then seeds the offers catalogue.

### 3. Open

| Service | URL | Notes |
|---|---|---|
| Web (Next.js) | http://localhost:3000 | the only thing a browser should touch |
| authservice | http://localhost:8081 | published image, `v0.3.1` |
| OffersService | http://localhost:8082 | REST API — see `docs/AGENT-GUIDE.md` |

Sign-in is required before any offer is visible — there is no anonymous view.

**Seeded local admin:** `admin@auto-veritas.local` / `Admin123!`

Or register any account to browse as a viewer. Viewers can read; only
`Admin`/`SuperAdmin` can write.

### 4. Optional — override the weak defaults

```bash
cp secrets.env.example .env            # .env is gitignored
```

| Variable | Default |
|---|---|
| `POSTGRES_PASSWORD` | `autoveritas-dev` |
| `INITIAL_ADMIN_EMAIL` | `admin@auto-veritas.local` |
| `INITIAL_ADMIN_PASSWORD` | `Admin123!` |

### 5. Stop / reset

```bash
docker compose down                    # stop, keep the database
docker compose down -v                 # stop and wipe the database volume
```

---

## Option B — local via the Aspire AppHost

Same topology, but `offers` and `web` run from source with hot reload instead of
from built images. Better for the dev loop; not what CI or e2e use.

### 1. One-time — the dev key into user-secrets

Aspire passes the key as a parameter rather than a mounted file, so after
running `generate-jwt-key.sh`:

```bash
dotnet user-secrets set Parameters:jwt-signing-key "$(cat certs/jwt-signing.dev.pem)" \
  --project src/AutoVeritas.AppHost
```

### 2. Run

```bash
dotnet run --project src/AutoVeritas.AppHost
```

Same ports as compose: web on 3000, authservice on 8081. The Aspire dashboard
URL is printed on startup. PostgreSQL is started as a container by Aspire.

---

## Tests

```bash
dotnet test AutoVeritas.slnx           # 52 backend tests (SQLite-backed API tests)
pnpm test && pnpm lint && pnpm build   # web-kit tests, lint, production build
pnpm e2e                               # Playwright smoke tier — needs the compose stack up
```

The e2e suite talks to `http://localhost:3000`, so start compose first. Its
charter (which flows are protected, and why) is in `e2e/CHARTER.md`.

---

## Deploy — Fly.io

Four apps in `waw`, deployed in dependency order by a git tag. Configs live in
`flyio/*.fly.toml`.

| App | What |
|---|---|
| `auto-veritas-postgres` | the single database instance (stateful, volume-backed) |
| `auto-veritas-authservice` | published authservice image, holds the RS256 trust root |
| `auto-veritas-offers` | the offers API |
| `auto-veritas-web` | the Next.js BFF — the only public entry point |

### 1. One-time — create the GitHub environment

Settings → Environments → **New environment** → name it `flyio`.

It must be an *Environment*, not repository secrets: every deploy job declares
`environment: flyio`, so repository-level secrets are not visible to them.

### 2. One-time — add four secrets to that environment

| Secret | Consumed as | How to produce it |
|---|---|---|
| `FLY_API_TOKEN` | auth for every deploy step | `fly tokens create org` |
| `POSTGRES_PASSWORD` | the postgres app + both connection strings | your choice — **alphanumeric only**, see gotchas |
| `JWT_SIGNING_KEY_PEM` | authservice → `Jwt__PrivateKeyPem` | `openssl genpkey -algorithm RSA -pkeyopt rsa_keygen_bits:2048` |
| `INITIAL_ADMIN_PASSWORD` | authservice → `InitialAdmin__Password` | your choice — this is the agent's SuperAdmin account |

Do **not** create `GITHUB_TOKEN`. It appears in `secret-scan.yml` but GitHub
injects it automatically.

Paste the JWT key including its `-----BEGIN PRIVATE KEY-----` / `-----END …-----`
lines.

### 3. Deploy

```bash
git tag v0.1.0
git push origin v0.1.0
```

Merging to `main` runs CI only — nothing deploys without a `v*` tag.

The workflow then:

1. **test** — build + unit tests, a fast gate.
2. **detect-changes** — diffs against the previous tag to pick which services
   changed. A service whose Fly app does not exist yet is always selected, so
   the first tag deploys everything.
3. **build** — one image per changed built service, pushed to `registry.fly.io`.
4. **deploy** — ordered: `postgres → authservice → offers → web`. Each gate
   accepts a skipped upstream, so an unchanged service doesn't block the chain.

### What you do not set by hand

These are assembled by the workflow from the four root secrets and staged with
`fly secrets set --stage`:

| App | Secret | Derived from |
|---|---|---|
| `auto-veritas-authservice` | `ConnectionStrings__DefaultConnection` | password + `…postgres.internal:5432/authdb` |
| `auto-veritas-authservice` | `Jwt__PrivateKeyPem` | `JWT_SIGNING_KEY_PEM` |
| `auto-veritas-authservice` | `InitialAdmin__Password` | `INITIAL_ADMIN_PASSWORD` |
| `auto-veritas-offers` | `ConnectionStrings__DefaultConnection` | password + `…postgres.internal:5432/offersdb` |

`auto-veritas-web` holds **no** secrets: sessions are HttpOnly cookies carrying
authservice's tokens, verified against its JWKS.

---

## Gotchas that have actually bitten this repo

**`Jwt__PublicBaseUrl` must be the in-network address.** It feeds the discovery
document's `jwks_uri`, and the JWKS is fetched by the *validators* (the offers
container, the web BFF) — not by a browser. Setting it to `http://localhost:8081`
sent the offers container to its own loopback for signing keys: connection
refused, empty key set, every token 401'd, and both services still reported
healthy. Compose therefore uses `http://authservice:8080`.

**The signing key must be PKCS#8.** authservice rejects PKCS#1. `openssl genpkey`
gives you `BEGIN PRIVATE KEY` (correct); the older `openssl genrsa` gives
`BEGIN RSA PRIVATE KEY` (rejected). Minimum 2048-bit.

**`POSTGRES_PASSWORD` must be alphanumeric.** `+ / = ;` break the assembled
connection strings.

**The deploy fails deliberately if the published JWKS is empty** after an
authservice deploy. A missing key silently selects HS256 and serves `keys: []`
while every consumer rejects every token — better to fail the deploy.

**Rotating the signing key is a rolling operation.** Set the new key as
`Jwt__PrivateKeyPem`, move the old *public* key to `Jwt__PreviousPublicKeyPem`,
keep it for one access-token lifetime (60 min), then drop it. Dropping it
immediately signs every user out.

**`lastVerifiedAt` in the future is rejected** (400). If an ingestion run fails
on every entry, check the clock skew between wherever the timestamps were
generated and the server.

**CodeQL is red on every commit** and is not your change. Code scanning is not
enabled in the repository settings; the analysis itself runs clean and fails
only at the SARIF upload. Tracked in `docs/architecture/DEVIATIONS.md`.

---

## Where else to look

| Topic | Document |
|---|---|
| Getting offers into the system (the agent workflow) | [`AGENT-GUIDE.md`](AGENT-GUIDE.md) |
| Bulk ingestion + the research-agent output contract | [`AGENT-GUIDE.md` §7](AGENT-GUIDE.md) |
| Secret definitions and rotation rules | [`../flyio/SECRETS.md`](../flyio/SECRETS.md) |
| Why things are built the way they are | [`architecture/05-DECISIONS.md`](architecture/05-DECISIONS.md) |
| Known unfixed gaps | [`architecture/DEVIATIONS.md`](architecture/DEVIATIONS.md) |
| Screens, flows, backlog | [`ux/UI-UX.md`](ux/UI-UX.md) |
| E2E charter | [`../e2e/CHARTER.md`](../e2e/CHARTER.md) |
