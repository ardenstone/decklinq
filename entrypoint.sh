#!/bin/bash
set -e

FLYWAY_URL="${FLYWAY_URL:-jdbc:postgresql://postgres:5432/decklinq}"
FLYWAY_USER="${FLYWAY_USER:-${DB_USER:-postgres}}"
FLYWAY_PASSWORD="${FLYWAY_PASSWORD:-${DB_PASSWORD:-postgres}}"

echo "Running database migrations via Flyway..."
flyway \
  -url="${FLYWAY_URL}" \
  -user="${FLYWAY_USER}" \
  -password="${FLYWAY_PASSWORD}" \
  -locations="filesystem:/app/migrations" \
  migrate

echo "Migrations complete. Starting DeckLinq API..."
exec dotnet backend.dll
