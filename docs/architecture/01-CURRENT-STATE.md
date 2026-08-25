# 01 — Current state (at session start, 2026-08-24)

The repository was empty: `README.md` (one line), `LICENSE` (MIT), and a stock
.NET `.gitignore`. No code, no build, no deployment, no history beyond the
initial commit.

That makes this a **greenfield delivery**, assessed under the playbook as
MODERNIZE-shaped (see [05-DECISIONS.md](05-DECISIONS.md), D-1): `02-GAP-ANALYSIS`
starts from every compliance item unmet by absence, `03-TARGET-ARCHITECTURE` is
the full design, and `04-MIGRATION-PLAN` is the ordered build plan rather than a
migration.

The product being built, from the owner's requirements: a login-gated comparison
site for car offers and financing on the Spanish market. Viewers can only read;
every offer is entered and re-verified by the owner's agent through the backend
API. The trust mechanism — visible per-value verification dates with
per-data-type staleness thresholds — is a first-class product feature, not
metadata.
