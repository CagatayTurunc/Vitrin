#!/bin/sh
# Önce login ol
TOKEN=$(wget -qO- \
  --post-data='{"email":"admin@vitrin.app","password":"Admin1234!"}' \
  --header='Content-Type: application/json' \
  http://vitrin-gateway:8080/api/auth/login | tr -d '"')

echo "Token: ${TOKEN:0:50}..."

# Token ile admin users endpoint'ini çağır
echo "--- Admin Users Response ---"
wget -qO- \
  --header="Authorization: Bearer $TOKEN" \
  http://vitrin-gateway:8080/api/auth/admin/users
echo ""
echo "Exit: $?"
