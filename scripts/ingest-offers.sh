#!/usr/bin/env bash
# Turns a batch of researched offers (JSON matching offers-input.example.json)
# into API calls: one GET per entity type to fetch the existing catalog, then
# one create-or-update decision per researched entry — POST if its slug is new,
# PUT to the existing id if it already exists. See docs/AGENT-GUIDE.md §7.
#
# Usage:
#   AGENT_EMAIL=agent@auto-veritas.local AGENT_PASSWORD='...' \
#     ./scripts/ingest-offers.sh path/to/researched-offers.json
#
# AUTH_URL / OFFERS_URL default to the local compose addresses; override both
# for a production run. Credentials are read from the environment, never a
# script argument, so they never land in shell history or a process list.
set -euo pipefail

: "${AGENT_EMAIL:?Set AGENT_EMAIL to the agent account's e-mail}"
: "${AGENT_PASSWORD:?Set AGENT_PASSWORD to the agent account's password}"
AUTH_URL="${AUTH_URL:-http://localhost:8081}"
OFFERS_URL="${OFFERS_URL:-http://localhost:8082}"

if [[ $# -ne 1 ]]; then
  echo "Usage: $0 <path-to-researched-offers.json>" >&2
  exit 1
fi
input_file="$1"

if ! command -v jq >/dev/null 2>&1; then
  echo "ERROR: jq is required (used to build/parse every request in this script)." >&2
  exit 1
fi

if ! jq -e 'has("carOffers") and has("financingOffers")' "$input_file" >/dev/null 2>&1; then
  echo "ERROR: $input_file must be a JSON object with \"carOffers\" and \"financingOffers\" arrays." >&2
  echo "        See scripts/offers-input.example.json for the exact shape." >&2
  exit 1
fi

# This script's matching is entirely slug-based, so every entry needs one —
# the API itself treats slug as optional (it derives one from the name on
# create), but a derived slug can't be predicted client-side without
# reimplementing the server's diacritic-stripping normalizer in jq.
missing=$(jq -r '[.carOffers[]?, .financingOffers[]?] | map(select((.slug // "") == "")) | length' "$input_file")
if [[ "$missing" -gt 0 ]]; then
  echo "ERROR: $missing entries in $input_file have no explicit \"slug\" — every entry needs one" >&2
  echo "        so this script can tell an update from a new offer. Add one to each." >&2
  exit 1
fi

echo "Logging in as $AGENT_EMAIL..."
login_body=$(jq -n --arg e "$AGENT_EMAIL" --arg p "$AGENT_PASSWORD" '{email: $e, password: $p}')
login_response=$(curl -s "$AUTH_URL/api/v1/auth/login" -H 'Content-Type: application/json' -d "$login_body")
TOKEN=$(jq -r '.accessToken // empty' <<<"$login_response")
if [[ -z "$TOKEN" ]]; then
  echo "ERROR: login failed — response: $login_response" >&2
  exit 1
fi

# Fetches every page of a list endpoint and returns the concatenated items as
# one JSON array. Not needed at today's catalog size (well under the 100-item
# page cap), but keeps the script correct as it grows instead of silently
# missing offers past the first page.
fetch_all() {
  local entity="$1" page=1 all="[]" resp items count
  while true; do
    resp=$(curl -s "$OFFERS_URL/api/v1/$entity?limit=100&page=$page" -H "Authorization: Bearer $TOKEN")
    items=$(jq -c '.items // []' <<<"$resp")
    count=$(jq 'length' <<<"$items")
    all=$(jq -c -n --argjson a "$all" --argjson b "$items" '$a + $b')
    [[ "$count" -lt 100 ]] && break
    page=$((page + 1))
  done
  echo "$all"
}

# Loops one entity type: for each researched item, look its slug up in the
# existing-catalog map built by fetch_all; PUT to the matching id if found,
# POST to create otherwise. Counts are updated via the caller's namerefs so
# they survive the loop (a `| while read` pipeline would run in a subshell
# and lose them).
ingest_entity() {
  # created_var/updated_var/failed_var are the fixed literal counter-variable
  # names each call site passes below — never derived from $input_file or any
  # other external input — so the eval below only ever executes a bounded
  # `<one of six known names>=$((<same name> + 1))`. A `local -n` nameref
  # would be cleaner but needs bash 4.3+; eval keeps this working on the
  # older bash macOS ships by default, which is exactly where this gets run.
  local entity="$1" array_key="$2" created_var="$3" updated_var="$4" failed_var="$5"
  local existing slug_map offer slug existing_id status

  echo "Fetching existing $entity..."
  existing=$(fetch_all "$entity")
  slug_map=$(jq -c '[.[] | {(.slug): .id}] | add // {}' <<<"$existing")

  while IFS= read -r offer; do
    slug=$(jq -r '.slug' <<<"$offer")
    existing_id=$(jq -r --arg s "$slug" '.[$s] // empty' <<<"$slug_map")

    if [[ -n "$existing_id" ]]; then
      status=$(curl -s -o /dev/null -w '%{http_code}' -X PUT "$OFFERS_URL/api/v1/$entity/$existing_id" \
        -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' -d "$offer")
      if [[ "$status" == "200" ]]; then
        echo "  updated $slug"
        eval "$updated_var=\$(($updated_var + 1))"
      else
        echo "  FAILED update $slug (HTTP $status)"
        eval "$failed_var=\$(($failed_var + 1))"
      fi
    else
      status=$(curl -s -o /dev/null -w '%{http_code}' -X POST "$OFFERS_URL/api/v1/$entity" \
        -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' -d "$offer")
      if [[ "$status" == "201" ]]; then
        echo "  created $slug"
        eval "$created_var=\$(($created_var + 1))"
      else
        echo "  FAILED create $slug (HTTP $status)"
        eval "$failed_var=\$(($failed_var + 1))"
      fi
    fi
  done < <(jq -c --arg k "$array_key" '.[$k][]' "$input_file")
}

car_created=0 car_updated=0 car_failed=0
fin_created=0 fin_updated=0 fin_failed=0

ingest_entity "car-offers" "carOffers" car_created car_updated car_failed
ingest_entity "financing-offers" "financingOffers" fin_created fin_updated fin_failed

echo
echo "Car offers:       $car_created created, $car_updated updated, $car_failed failed"
echo "Financing offers:  $fin_created created, $fin_updated updated, $fin_failed failed"

if [[ $((car_failed + fin_failed)) -gt 0 ]]; then
  exit 1
fi
