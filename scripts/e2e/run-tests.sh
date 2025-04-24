#!/bin/sh

# Color codes
GREEN='\033[0;32m'
NC='\033[0m'
BLUE='\033[0;34m'

echo "${BLUE}Running E2E Health Check...${NC}"

HEALTH_RESPONSE=$(curl -s -w "%{http_code}" http://localhost:5001/health)
if [ "$HEALTH_RESPONSE" = "200" ]; then
    echo "${GREEN}✓${NC} Health check passed"
    exit 0
else
    echo "${RED}✗${NC} Health check failed"
    echo "Expected: 200"
    echo "Got: $HEALTH_RESPONSE"
    exit 1
fi
