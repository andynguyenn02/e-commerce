#!/usr/bin/env bash
# Run a SQL seed from seeds/database against the sql-dev container.
# Usage:
#   ./seeds/run-seed.sh              # pick from a menu
#   ./seeds/run-seed.sh checkout     # run seeds/database/checkout-seed.sql directly
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
SEED_DIR="$ROOT_DIR/seeds/database"
CONTAINER="sql-dev"
DB_NAME="${DB_NAME:-EcommerceDb}"

mapfile -t seeds < <(find "$SEED_DIR" -maxdepth 1 -name '*.sql' -printf '%f\n' | sort)
if [[ ${#seeds[@]} -eq 0 ]]; then
    echo "No .sql files in $SEED_DIR" >&2
    exit 1
fi

if [[ $# -ge 1 ]]; then
    seed="$1"
    [[ "$seed" == *.sql ]] || seed="$seed-seed.sql"
    if [[ ! -f "$SEED_DIR/$seed" ]]; then
        echo "Seed not found: $seed" >&2
        echo "Available: ${seeds[*]}" >&2
        exit 1
    fi
else
    echo "Select a seed to run:"
    PS3="Number: "
    select seed in "${seeds[@]}"; do
        [[ -n "${seed:-}" ]] && break
        echo "Invalid choice"
    done
fi

if ! docker ps --format '{{.Names}}' | grep -qx "$CONTAINER"; then
    echo "Container '$CONTAINER' is not running. Start it with: docker compose up -d" >&2
    exit 1
fi

echo "Running $seed on $DB_NAME ..."
# The SA password is read from the container's own environment
docker exec -i "$CONTAINER" bash -c \
    '/opt/mssql-tools18/bin/sqlcmd -C -b -I -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -d "$1"' _ "$DB_NAME" \
    < "$SEED_DIR/$seed"
echo "Done: $seed"

# Print the seed's companion notes (e.g. checkout-seed.txt) when present
notes="$SEED_DIR/${seed%.sql}.txt"
[[ -f "$notes" ]] && cat "$notes"
exit 0
