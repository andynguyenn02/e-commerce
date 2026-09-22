#!/usr/bin/env bash
# Seeds the local dev database. Works from a fresh clone on any machine:
# brings up the docker-compose SQL Server container, waits for it to be
# healthy, then applies seed.sql inside it.
#
# Usage:
#   ./seeds/database/seed.sh
#
# Requires a repo-root .env with MSSQL_SA_PASSWORD (see docker-compose.yml).
# Assumes the EF Core migrations have already been applied
# (dotnet ef database update --project ecommerce.Infrastructure --startup-project ecommerce.Api).

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
SQL_FILE="$SCRIPT_DIR/seed.sql"
CONTAINER_NAME="sql-dev"
DB_NAME="${MSSQL_DB_NAME:-EcommerceDb}"
HEALTH_TIMEOUT_SECONDS=60

cd "$REPO_ROOT"

if [ ! -f .env ]; then
  echo "Error: .env not found in repo root. Create one with MSSQL_SA_PASSWORD=<password> (see docker-compose.yml)." >&2
  exit 1
fi

SA_PASSWORD="$(grep -E '^MSSQL_SA_PASSWORD=' .env | head -1 | cut -d '=' -f2-)"
if [ -z "$SA_PASSWORD" ]; then
  echo "Error: MSSQL_SA_PASSWORD not set in .env" >&2
  exit 1
fi

echo "Starting sqlserver container (docker compose up -d)..."
docker compose up -d sqlserver

echo -n "Waiting for SQL Server to report healthy"
elapsed=0
until [ "$(docker inspect -f '{{.State.Health.Status}}' "$CONTAINER_NAME" 2>/dev/null)" = "healthy" ]; do
  if [ "$elapsed" -ge "$HEALTH_TIMEOUT_SECONDS" ]; then
    echo
    echo "Error: $CONTAINER_NAME did not become healthy within ${HEALTH_TIMEOUT_SECONDS}s." >&2
    exit 1
  fi
  echo -n "."
  sleep 2
  elapsed=$((elapsed + 2))
done
echo " healthy"

echo "Copying seed.sql into container..."
docker cp "$SQL_FILE" "$CONTAINER_NAME:/tmp/seed.sql"
docker exec -u root "$CONTAINER_NAME" chmod 644 /tmp/seed.sql

echo "Applying seed data to database '$DB_NAME'..."
docker exec "$CONTAINER_NAME" /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P "$SA_PASSWORD" -d "$DB_NAME" -i /tmp/seed.sql

docker exec -u root "$CONTAINER_NAME" rm -f /tmp/seed.sql

echo "Seed complete."
