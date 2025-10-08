#!/usr/bin/env bash
# usage: ./seed-orders.sh [COUNT] [CONCURRENCY]
# example: ./seed-orders.sh 1000 20
set -euo pipefail

COUNT="${1:-500}"
CONC="${2:-20}"
ORDERS_API="${ORDERS_API:-http://localhost:5002}"

echo "Seeding $COUNT orders to $ORDERS_API (concurrency=$CONC)…"

seq "$COUNT" | xargs -n1 -P"$CONC" -I{} bash -c '
  cid=$(( (RANDOM % 3) + 1 ))      # CustomerId 1..3
  total=$(awk -v r=$RANDOM "BEGIN { printf \"%.2f\", (r%10000)/100 }")
  curl -s -o /dev/null -w "%{http_code}\n" -X POST "$0/api/orders" \
    -H "Content-Type: application/json" \
    -d "{\"customerId\":$cid,\"total\":$total}" >/dev/null
' "$ORDERS_API"

echo "Seed complete."
