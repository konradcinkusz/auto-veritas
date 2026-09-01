# E2E charter

Written before the tests, as the testing standard requires. The suite protects the
flows a paying visitor and the owner's agent actually depend on; everything else is
covered at a cheaper tier.

## Protected flows (smoke, every PR, budget 5–10 minutes, single browser)

1. An anonymous visitor is redirected to the login page — there is no anonymous
   product surface.
2. A new user registers (accepting the current consent versions), lands on the
   dashboard, and sees both offer tables with data.
3. Every visible offer row carries verification metadata — the freshness badge and
   the "sprawdzono" date are rendered, not optional.
4. The financing table exposes the repayment structure as a first-class column
   (the BALON chip is visible for balloon offers).
5. An offer added by the agent through the API appears for a signed-in viewer
   after reload.
6. Logout ends the session: the dashboard redirects to login again.
7. An offer whose price the agent changes via the API shows the *previous*
   price in its "Historia" panel for a signed-in viewer — the core trust claim
   ("you can see when and to what a value changed") actually renders, not just
   the endpoint answering 200.

## Core-regression flows (core, alongside smoke, single browser)

Dashboard behaviour that is not smoke-critical but expensive to break. These
run against one shared stored session, so they are about the dashboard rather
than about logging in.

1. The search box narrows the car table, and clearing it restores the original
   count — including the explicit "no matches" state rather than a silently
   empty table.
2. The DGT filter leaves *only* matching rows — asserted across every remaining
   row, since a filter that leaks one wrong row is precisely the bug this tier
   exists to catch.
3. A stale offer that is **cheaper** still sorts below a fresh dearer one.
   Default ordering is freshness first, price second; this is degrade-don't-hide
   expressed as an assertion rather than a comment.
4. An offer that has never been edited reports an empty history in words, not a
   blank table or a spinner that never resolves — "history is broken" and
   "nothing has changed yet" must not look the same.
5. An offer with no recorded source says so in its details panel. Claiming
   verification while pointing at nothing is the failure that panel closes.

## Non-goals

- No single-field validation through a browser (unit tier owns it).
- No duplication of backend integration tests through the UI (the
  WebApplicationFactory suite owns API behavior, including role enforcement).
- No pixel-checking; assertions target roles, labels and text.

## Conventions

- Locators: role + accessible name first, label/text second, `data-testid` as the
  deliberate fallback. The two structural `section.card` scopes in the suite are
  an accepted exception until the sections carry accessible landmarks; no other
  CSS chains.
- Waiting: web-first auto-retrying assertions only; fixed sleeps are banned and
  grepped for in CI.
- Auth: **smoke** registers a fresh account per test on purpose — registration
  is itself part of the surface it protects, and its first flow is the anonymous
  redirect, which a pre-seeded session would defeat. **core** uses one shared
  `storageState` seeded by `tests/auth.setup.ts`, as this charter required once
  the core tier landed: those tests are about the dashboard, and paying for a
  registration round-trip in each buys nothing but a slower suite and more ways
  to fail.
- Tiers are Playwright **projects**, not just tags, because they need different
  session handling. Each project matches exactly one spec file, so no test is
  collected twice under two different session assumptions.
- Data: generated per-test accounts (`e2e-<uuid>@example.test`); the stack the
  suite runs against is ephemeral (compose-per-run), so cleanup is mechanical.
