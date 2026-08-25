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
- Auth: at the current suite size every test registers its own fresh account —
  registration is itself part of the protected surface, and six tests do not
  amortize a `storageState` setup. The stored-context pattern becomes mandatory
  when the core-regression tier lands and login stops being what most tests are
  about.
- Data: generated per-test accounts (`e2e-<uuid>@example.test`); the stack the
  suite runs against is ephemeral (compose-per-run), so cleanup is mechanical.
