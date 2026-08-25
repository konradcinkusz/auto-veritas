#!/usr/bin/env bash
# Regenerates EF Core migrations for the OffersService.
#   scripts/generate-migrations.sh AddSomeColumn
set -euo pipefail

name="${1:?Usage: scripts/generate-migrations.sh <MigrationName>}"
repo_root="$(cd "$(dirname "$0")/.." && pwd)"

cd "$repo_root"
DATABASE_PROVIDER=PostgreSQL dotnet ef migrations add "$name" \
  --project src/AutoVeritas.OffersService.Migrations.PostgreSQL \
  --startup-project src/AutoVeritas.OffersService
