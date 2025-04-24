#!/bin/sh

# Color codes
GREEN='\033[0;32m'
RED='\033[0;31m'
NC='\033[0m'
BLUE='\033[0;34m'

# Test helpers
assert_equals() {
    if [ "$1" = "$2" ]; then
        printf "${GREEN}✓${NC} $3\n"
    else
        printf "${RED}✗${NC} $3\n"
        printf "  Expected: $2\n"
        printf "  Got: $1\n"
        exit 1
    fi
}

assert_contains() {
    if echo "$1" | grep -q "$2"; then
        printf "${GREEN}✓${NC} $3\n"
    else
        printf "${RED}✗${NC} $3\n"
        printf "  Expected to contain: $2\n"
        printf "  Got: $1\n"
        exit 1
    fi
}

# Test scenarios
echo "${BLUE}Running E2E tests...${NC}"

# Scenario 1: Create a new Todo
echo "\n${BLUE}Scenario 1: Create a new Todo${NC}"
CREATE_RESPONSE=$(curl -s -X POST http://localhost:5001/api/todos \
    -H "Content-Type: application/json" \
    -d '{"title":"E2E Test Todo","description":"This is a test todo","priority":1}')

TODO_ID=$(echo $CREATE_RESPONSE | sed 's/.*"id":"\([^"]*\)".*/\1/')
assert_contains "$CREATE_RESPONSE" "E2E Test Todo" "Should create a todo with correct title"

# Scenario 2: Get the created Todo
echo "\n${BLUE}Scenario 2: Get created Todo${NC}"
GET_RESPONSE=$(curl -s http://localhost:5001/api/todos/$TODO_ID)
assert_contains "$GET_RESPONSE" "E2E Test Todo" "Should retrieve the created todo"
assert_contains "$GET_RESPONSE" "This is a test todo" "Should have correct description"

# Scenario 3: Update the Todo
echo "\n${BLUE}Scenario 3: Update Todo${NC}"
UPDATE_RESPONSE=$(curl -s -X PUT http://localhost:5001/api/todos/$TODO_ID \
    -H "Content-Type: application/json" \
    -d '{"title":"Updated E2E Test Todo","description":"Updated description","priority":2,"isComplete":true}')
assert_contains "$UPDATE_RESPONSE" "Updated E2E Test Todo" "Should update todo title"
assert_contains "$UPDATE_RESPONSE" "Updated description" "Should update todo description"

# Verify the update
GET_UPDATED_RESPONSE=$(curl -s http://localhost:5001/api/todos/$TODO_ID)
assert_contains "$GET_UPDATED_RESPONSE" "Updated E2E Test Todo" "Should persist updated title"
assert_contains "$GET_UPDATED_RESPONSE" "true" "Should be marked as complete"

# Scenario 4: Delete the Todo
echo "\n${BLUE}Scenario 4: Delete Todo${NC}"
DELETE_RESPONSE=$(curl -s -X DELETE http://localhost:5001/api/todos/$TODO_ID -w "%{http_code}" -o /dev/null)
assert_equals "$DELETE_RESPONSE" "204" "Should return 204 status code"

# Verify deletion
GET_DELETED_RESPONSE=$(curl -s -w "%{http_code}" http://localhost:5001/api/todos/$TODO_ID)
assert_contains "$GET_DELETED_RESPONSE" "404" "Should return 404 for deleted todo"

echo "\n${GREEN}All E2E tests passed successfully!${NC}"
