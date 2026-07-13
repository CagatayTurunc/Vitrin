#!/bin/sh
wget -qO- \
  --post-data='{"email":"admin@vitrin.app","password":"Admin1234!"}' \
  --header='Content-Type: application/json' \
  --timeout=10 \
  http://vitrin-gateway:8080/api/auth/login
echo ""
echo "Exit: $?"
