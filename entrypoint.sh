#!/bin/bash
set -e

echo "Running database migrations via Flyway..."
flyway \
  -url="${DB_CONNECTION_URL}" \
  -user="${DB_USER}" \
  -password="${DB_PASSWORD}" \
  -locations="filesystem:/app/migrations" \
  migrate

echo "Migrations complete. Starting DeckLinq API..."
exec dotnet backend.dll
