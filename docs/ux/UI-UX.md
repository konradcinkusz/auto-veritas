# UI/UX — screens, flows, and the ranked backlog

As built in the 2026-08-24 delivery session. UI language is Polish (the owner's
working language for this product); code and docs are English.

## Screens

| Route | Access | What it is |
|---|---|---|
| `/login` | public | Dark-theme card: e-mail + password, generic error messages (never revealing whether the account exists), lockout / unverified-e-mail / rate-limit messages mapped from authservice's response shapes, a second step for 2FA-enabled accounts (authenticator code, or a recovery code when the device is gone). Preserves `?redirect=`. |
| `/register` | public | E-mail + password (policy hinted in the error path), consent checkbox rendering the **live** consent versions fetched from authservice (never hardcoded), handles both the 200-with-tokens and 202-verify-first response shapes. |
| `/` | signed-in | The product: header with user e-mail + logout, badge row (counts, region, newest verification date), and two comparison tables. |
| `/healthz` | public | Platform health only. |

## The dashboard tables

Both tables share the interaction model from the owner's original sketch —
search, dropdown filters, a slider, click-to-sort headers, count tags, reset —
plus the trust layer this product exists for:

- **Weryfikacja column** on every row: freshness badge (🟢 fresh / 🟡 warning /
  🔴 stale "zweryfikuj u dealera" / ⛔ expired), the exact verification date, and
  the seller's declared validity when present ("ważna do 31.08.2026" /
  "wygasła…").
- **Default ordering degrades stale data** to the bottom (freshness rank, then
  price). Stale/expired rows also render dimmed. Nothing is ever hidden — a
  missing row would read as "model unavailable", which is a worse lie than an
  old price.
- **Świeżość danych filter** — "zweryfikowane ≤ 7/14/30 dni" (cars) and
  "≤ 14/45 dni" (credits), matching the server's published thresholds.
- **"szacunek" chips** on any estimated value (e.g. manufacturer TINs the agent
  could not confirm publicly) — estimates are never presented as facts.
- **Struktura column** (credits): linear / **BALON** / abonament chips, with the
  balloon chip styled as a warning. The advertised "niska rata" hiding an
  18 000 € final payment is the single most valuable thing this table exposes.
- Legend and thresholds in the footer come from `/api/v1/meta/freshness-policy`.

## Flows

1. **Viewer**: register (live consent versions) → dashboard → filter/sort/read →
   logout. Session is HttpOnly cookies; an expired access token refreshes
   silently server-side.
2. **Agent** (the owner's Claude agent): login as the Admin-role account →
   `POST/PUT/DELETE` offers, `POST …/verify` to touch verification timestamps —
   documented step by step in [`../AGENT-GUIDE.md`](../AGENT-GUIDE.md). The
   frontend has deliberately no write UI.

## What this session changed and why

Everything — the repo was empty. The sketch's static HTML became a served,
login-gated product; the analysis conversation's requirements (three dates,
per-type thresholds, confirmed-vs-estimated, balloon exposure, degrade-don't-hide)
became server-enforced domain rules rather than table copy.

## Ranked backlog

1. **Configure e-mail verification before opening registration** (SendGrid key +
   `Auth__RequireConfirmedEmail`) — trust/launch blocker, see the deviation
   register. [P5 / SECURITY-REVIEW]
2. **Monthly-budget inverse search** — "mam 500 €/mies." → which cars fit under
   which financing scenarios; the calculation the owner ran by hand in the
   analysis conversation. Needs a rate-scenario engine over the financing table.
   [product core value]
3. ~~**Offer detail view** with source links and verification history~~ —
   **built, both halves**. History: `GET /api/v1/{car,financing}-offers/{id}/history`
   plus a "Historia" toggle per row expanding a compact table of every prior
   value (date changed, old price/rate, who changed it), see [D-13]. Sources:
   a "Szczegóły" toggle per row expanding the three trust dates, the
   confidence marker and the source as a link — rendered from the list
   response, so it costs no extra request. The link is render-only
   (`rel="noopener noreferrer nofollow"`, never fetched server-side), keeping
   the security review's A10/SSRF proof valid.
4. ~~**2FA challenge flow** in the login page~~ — **built**: a 2FA-enabled
   account now gets a code step instead of a dead end, with a recovery-code
   fallback. The challenge token stays in an HttpOnly cookie and never reaches
   page scripts (D-16). [D-12]
5. **Per-column freshness** — ~~a price verified yesterday but a spec from a
   year ago currently share one row badge~~ — **deliberately not built**
   (2026-09-01). The owner confirmed the agent always verifies everything in a
   single pass, so one `lastVerifiedAt` per offer is an accurate
   representation, not a simplification: splitting it would produce three
   fields that are always equal, at the cost of a migration, a changed
   `POST .../verify` contract, and a new way for the three to drift in the data
   without drifting in reality. **The trigger is unchanged** — if the agent
   ever moves to partial passes (prices weekly, specs annually), one shared
   date starts lying and this becomes worth building that day.
   [freshness model fidelity]
6. **OAuth provider buttons** rendered from authservice's `/providers` discovery
   (only if the owner configures Google/GitHub). [D-12]
7. ~~**Saved filters / shareable views** for comparing shortlists~~ —
   **half built**: filter and sort state now round-trips through the query
   string, so a view survives a refresh and can be pasted to another signed-in
   user. Written with `history.replaceState`, not `router.replace`, so typing
   in the search box does not run the App Router's navigation machinery per
   keystroke; parameters at their default are omitted so a shared link stays
   readable. Still open: *named* saved filters, which need per-user
   persistence and a new API surface — deliberately split out rather than
   bundled, since it is a materially larger change than URL state.
