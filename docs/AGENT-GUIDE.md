# Agent guide — how offers get into Auto Veritas

The frontend is read-only by design. Every offer is created, updated and
re-verified by the owner's agent through the OffersService API. This is the
complete workflow that agent follows.

Base URLs: local compose `http://localhost:8082` (auth at `:8081`); production
`https://auto-veritas-offers.fly.dev` (auth at
`https://auto-veritas-authservice.fly.dev`).

## 1. One-time: the agent account

The first boot seeds a SuperAdmin from `InitialAdmin__Email` /
`InitialAdmin__Password`. Either use it directly, or (better) register a
dedicated agent account and grant it `Admin`:

```bash
AUTH=http://localhost:8081

# SuperAdmin token
SUPER=$(curl -s $AUTH/api/v1/auth/login -H 'Content-Type: application/json' \
  -d '{"email":"admin@auto-veritas.local","password":"Admin123!"}' | jq -r .accessToken)

# The agent registered normally (consent versions from the live endpoint):
VERSIONS=$(curl -s $AUTH/api/v1/auth/consents/versions)
curl -s $AUTH/api/v1/auth/register -H 'Content-Type: application/json' -d "{
  \"email\":\"agent@auto-veritas.local\",\"password\":\"<strong password>\",
  \"acceptedTermsVersion\":$(echo $VERSIONS | jq .terms),
  \"acceptedPrivacyVersion\":$(echo $VERSIONS | jq .privacy)}"

# Find its user id, then grant Admin (SuperAdmin only; revokes the target's sessions):
AGENT_ID=$(curl -s "$AUTH/api/v1/admin/users?pageSize=100" -H "Authorization: Bearer $SUPER" \
  | jq -r '.users[] | select(.email=="agent@auto-veritas.local") | .id')
curl -s -X POST "$AUTH/api/v1/admin/users/$AGENT_ID/roles" \
  -H "Authorization: Bearer $SUPER" -H 'Content-Type: application/json' -d '{"role":"Admin"}'
```

## 2. Every session: log in

```bash
TOKEN=$(curl -s $AUTH/api/v1/auth/login -H 'Content-Type: application/json' \
  -d '{"email":"agent@auto-veritas.local","password":"<password>"}' | jq -r .accessToken)
```

Access tokens live 60 minutes. Auth endpoints are rate-limited 20/min per IP.

## 3. Adding a car offer

`POST /api/v1/car-offers` — the three dates are the product, treat them as data:

```bash
OFFERS=http://localhost:8082
curl -s $OFFERS/api/v1/car-offers -H "Authorization: Bearer $TOKEN" \
  -H 'Content-Type: application/json' -d '{
  "name": "BYD Seal U DM-i Boost",
  "variant": "SUV / PHEV",
  "dgtLabel": "Cero",
  "powerCv": 218,
  "cashPriceEur": 34990,
  "financedPriceEur": 29990,
  "reliabilityScore": 89,
  "reliabilityText": "Wysoka",
  "notes": "Segment D; 18,3 kWh, do 90 km na prądzie",
  "priceConfidence": "Confirmed",
  "sourceName": "byd.com/es",
  "sourceUrl": "https://www.byd.com/es/car/sealu",
  "lastVerifiedAt": "2026-08-24T12:00:00Z",
  "offerValidUntil": "2026-08-31T23:59:59Z",
  "sourcePublishedAt": "2026-08-24T00:00:00Z"
}'
```

Field rules:

- `lastVerifiedAt` (required) — when *you actually checked the value at source*,
  never "now" by habit. `offerValidUntil` — only if the seller declares one.
  `sourcePublishedAt` — the source's own date (an article's date, not yours).
- `priceConfidence` / `rateConfidence` — `"Confirmed"` only for values read from
  an official source; `"Estimated"` renders a "szacunek" chip in the UI. These,
  `dgtLabel`, `type` and `repaymentStructure` are **required** — the API rejects
  payloads that omit them (an omitted enum would otherwise silently default to
  its most-trusted value), and enum values must be strings, never numbers.
- `slug` is optional (derived from the name); pass it explicitly if you want a
  stable upsert identity. Duplicate slug ⇒ `409` — update the existing offer.
- Financing offers additionally carry `repaymentStructure`:
  `"Linear" | "Balloon" | "Subscription"`. **Never leave a balloon structure
  unmarked** — exposing hidden balloons is the product's reason to exist.

## 4. Re-verification (the cheapest trust operation)

When you re-check an offer and the values still hold, don't resend the payload:

```bash
curl -s -X POST "$OFFERS/api/v1/car-offers/$ID/verify" \
  -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
  -d '{"verifiedAt": "2026-08-31T09:00:00Z"}'
```

Only the verification timestamp (and optionally the source) moves; prices stay.
Freshness thresholds the UI applies: prices 7/30 days, rates 14/45 days, specs
6/12 months; a passed `offerValidUntil` marks the offer expired regardless.

## 5. Updating and removing

- `PUT /api/v1/car-offers/{id}` / `PUT /api/v1/financing-offers/{id}` — full
  replace; send the complete payload with a fresh `lastVerifiedAt`. It is a
  **full** replace — omitting `notes`/`reliabilityScore`/etc. clears them, it
  does not leave them untouched.
- Every `PUT` that actually changes a value automatically snapshots the
  *previous* state into that offer's history — no separate call needed. A
  viewer sees this as a "Historia" link under each row; you can read it back
  yourself at `GET /api/v1/car-offers/{id}/history` (newest first, capped at
  100 entries). Re-sending an unchanged payload writes nothing.
- `DELETE …/{id}` — only for offers that were *wrong*, not for expired ones:
  expired offers stay visible, marked, by design. Deleting an offer deletes
  its history with it.
- The API never mass-deletes and the seeder never overwrites: your edits always
  survive restarts.

## 6. What the API refuses

- Anonymous or viewer-role requests to any write endpoint (401/403).
- Payloads without `lastVerifiedAt`, out-of-range values, malformed URLs (400
  with per-field errors).
- Duplicate slugs on create (409).
- More than 200 requests/min per account (429 `{error, retryAfter}`).

## 7. Bulk ingestion: turning a batch of research into API calls

Sections 1–6 cover one offer at a time. When a separate research pass (a
different agent, or you in research mode) turns up a whole batch at once,
don't write one `curl` per offer by hand: have the research agent write its
findings to a single JSON file, then run `scripts/ingest-offers.sh` against
it. The script logs in once, reads the current catalog, and decides `POST`
vs `PUT` per entry itself — you don't pre-sort new-vs-existing yourself.

### 7.1 What to tell the research agent to produce

The research agent's job ends at a JSON file — it never calls the API
itself. Hand it an instruction block like this one (copy/adapt it — the
field list has to stay in sync with `scripts/offers-input.example.json`,
which is the canonical example either way):

```
Research car offers and financing offers for the Spanish market. For each
one you find, produce a JSON object with exactly these fields — car offers
and financing offers are separate shapes. Do not invent field names and do
not omit a required one.

Car offer:
  slug              string, REQUIRED — a stable id for this offer, e.g.
                     "byd-atto-2-dm-i-active" (kebab-case, model + variant).
                     This is how re-runs tell an update from a new offer —
                     reuse the exact same slug for the same real-world offer
                     every time you re-research it. Changing it later makes
                     the ingest script treat it as a brand-new offer.
  name              string, required
  variant           string, required — body style / powertrain, e.g. "SUV / PHEV"
  dgtLabel          "Cero" | "Eco" | "C" | "B" | "SinEtiqueta", required
  powerCv           integer, required
  cashPriceEur      number or null
  financedPriceEur  number or null
  reliabilityScore  integer 0-100 or null
  reliabilityText   string or null
  bootLiters        integer or null
  notes             string or null
  priceConfidence   "Confirmed" | "Estimated", required — "Confirmed" only
                     if you read the number off an official source page,
                     "Estimated" otherwise. Never guess and mark it Confirmed.
  sourceName        string or null — e.g. "byd.com/es"
  sourceUrl         string or null — the exact page you read the price from
  lastVerifiedAt    ISO 8601 timestamp, required — when YOU actually checked
                     this value, never today's date out of habit
  offerValidUntil   ISO 8601 timestamp or null — only if the seller states one
  sourcePublishedAt ISO 8601 timestamp or null — the source page's own date,
                     not yours

Financing offer:
  slug                    string, REQUIRED — e.g. "revolut"
  provider                string, required
  type                    "Bank" | "Captive" | "Fintech" | "Dealer", required
  tinPercent              number or null
  taePercent              number or null
  repaymentStructure      "Linear" | "Balloon" | "Subscription", required —
                          NEVER guess this one. If you can't confirm it from
                          the source, say so in your findings instead of
                          defaulting to "Linear" — an unmarked balloon
                          structure is the one mistake this product exists
                          to prevent.
  termDescription          string, required
  downPaymentDescription   string, required
  feesDescription          string, required
  monthlyInstallment60Eur  number or null
  totalInterest60Eur       number or null
  bestFor                  string, required — one line on who this suits
  rateConfidence           "Confirmed" | "Estimated", required
  sourceName               string or null
  sourceUrl                string or null
  lastVerifiedAt           ISO 8601 timestamp, required
  offerValidUntil          ISO 8601 timestamp or null
  sourcePublishedAt        ISO 8601 timestamp or null

Output ONE JSON file (not a chat message, not markdown) shaped exactly like:
  { "carOffers": [ ... ], "financingOffers": [ ... ] }

See scripts/offers-input.example.json in the repo for a filled-in example.
```

### 7.2 Running the ingestion

```bash
AGENT_EMAIL=agent@auto-veritas.local AGENT_PASSWORD='<password>' \
  ./scripts/ingest-offers.sh path/to/researched-offers.json
```

`AUTH_URL` / `OFFERS_URL` default to the local compose addresses; override
both for a production run:

```bash
AGENT_EMAIL=agent@auto-veritas.local AGENT_PASSWORD='<password>' \
  AUTH_URL=https://auto-veritas-authservice.fly.dev \
  OFFERS_URL=https://auto-veritas-offers.fly.dev \
  ./scripts/ingest-offers.sh path/to/researched-offers.json
```

What it does, in order:

1. Logs in once with `AGENT_EMAIL` / `AGENT_PASSWORD`.
2. `GET`s the full existing catalog for car offers and financing offers
   (paginated automatically past the 100-item page cap) and builds a
   slug → id lookup from it.
3. For every entry in the input file: if its `slug` is already in that
   lookup, `PUT`s the full payload to that offer's id — same rules as §5,
   including the automatic history snapshot on a real change; otherwise
   `POST`s it as a new offer.
4. Prints a per-entity created/updated/failed count and exits non-zero if
   anything failed, so it's safe to wire into a larger pipeline.

Re-running the same file is safe. Matching entries just `PUT` again, and an
unchanged payload writes no new history row (§5) — so a partial failure can
be fixed and the whole file re-run without polluting the timeline with
no-op entries. The one thing that has to hold on the research side is
**slug stability**: if the research agent assigns a different slug to the
same real-world offer on a later run, the script has no way to know it's
the same offer — it will `POST` a duplicate instead of updating.
