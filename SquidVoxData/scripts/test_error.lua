-- Test script to trigger an error dialog
-- This script intentionally contains an error to test the ScriptErrorGameObject

print("Test script starting...")

-- This will cause an error: attempting to call a function that doesn't exist
local result = nonexistent_function()

print("This line should never be reached")
