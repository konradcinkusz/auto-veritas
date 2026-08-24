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
  an official source; `"Estimated"` renders a "szacunek" chip in the UI.
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
  replace; send the complete payload with a fresh `lastVerifiedAt`.
- `DELETE …/{id}` — only for offers that were *wrong*, not for expired ones:
  expired offers stay visible, marked, by design.
- The API never mass-deletes and the seeder never overwrites: your edits always
  survive restarts.

## 6. What the API refuses

- Anonymous or viewer-role requests to any write endpoint (401/403).
- Payloads without `lastVerifiedAt`, out-of-range values, malformed URLs (400
  with per-field errors).
- Duplicate slugs on create (409).
- More than 200 requests/min per account (429 `{error, retryAfter}`).
