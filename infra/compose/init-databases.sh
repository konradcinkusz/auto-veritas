#!/bin/bash
# Runs on the postgres container's FIRST boot only (empty data directory):
# one logical database per service, so splitting instances later is a config change.
set -e

for db in authdb offersdb; do
  psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" <<-EOSQL
    SELECT 'CREATE DATABASE $db' WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = '$db')\gexec
EOSQL
done
