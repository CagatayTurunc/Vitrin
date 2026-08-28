#!/bin/bash

echo "=== Testing Subscription System ==="

# Register
echo "1. Register..."
docker exec -i vitrin-auth curl -s -X POST http://localhost:8080/api/auth/register \
  -H "Content-Type: application/json" \
  -d @register.json

# Login
echo -e "\n2. Login..."
LOGIN_RESPONSE=$(docker exec -i vitrin-auth curl -s -X POST http://localhost:8080/api/auth/login \
  -H "Content-Type: application/json" \
  -d @login.json)

TOKEN=$(echo $LOGIN_RESPONSE | jq -r '.token')
echo "Token: ${TOKEN:0:40}..."

# Get subscription
echo -e "\n3. Get subscription..."
docker exec vitrin-auth curl -s http://localhost:8080/api/subscription/me \
  -H "Authorization: Bearer $TOKEN"

# Create checkout
echo -e "\n\n4. Create checkout..."
CHECKOUT_RESPONSE=$(docker exec -i vitrin-auth curl -s -X POST http://localhost:8080/api/subscription/checkout \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d @checkout.json)

echo $CHECKOUT_RESPONSE | jq '.'

CHECKOUT_URL=$(echo $CHECKOUT_RESPONSE | jq -r '.checkoutUrl')
if [ "$CHECKOUT_URL" != "null" ]; then
  echo -e "\n✓✓✓ SUCCESS!"
  echo "Payment URL: $CHECKOUT_URL"
  echo -e "\nTest Card: 5528790000000008 | CVV: 123 | Date: 12/30"
fi
