#!/bin/sh
set -eu

: "${VITRIN_ADMIN_EMAIL:?VITRIN_ADMIN_EMAIL is required}"
: "${VITRIN_ADMIN_PASSWORD:?VITRIN_ADMIN_PASSWORD is required}"

payload=$(printf '{"email":"%s","password":"%s"}' "$VITRIN_ADMIN_EMAIL" "$VITRIN_ADMIN_PASSWORD")
token=$(wget -qO- \
  --post-data="$payload" \
  --header='Content-Type: application/json' \
  http://vitrin-gateway:8080/api/auth/login | tr -d '"')

if [ -z "$token" ]; then
  echo "Login did not return an access token." >&2
  exit 1
fi

wget -qO- \
  --header="Authorization: Bearer $token" \
  http://vitrin-gateway:8080/api/auth/admin/users
printf '\n'
