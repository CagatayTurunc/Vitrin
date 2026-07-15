#!/bin/sh
set -eu

: "${VITRIN_ADMIN_EMAIL:?VITRIN_ADMIN_EMAIL is required}"
: "${VITRIN_ADMIN_PASSWORD:?VITRIN_ADMIN_PASSWORD is required}"

payload=$(printf '{"email":"%s","password":"%s"}' "$VITRIN_ADMIN_EMAIL" "$VITRIN_ADMIN_PASSWORD")

wget -qO- \
  --post-data="$payload" \
  --header='Content-Type: application/json' \
  --timeout=10 \
  http://vitrin-gateway:8080/api/auth/login
printf '\n'
