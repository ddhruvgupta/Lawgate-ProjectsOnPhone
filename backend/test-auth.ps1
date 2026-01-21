# Test Authentication Endpoints
Write-Host "=== Testing Legal Document System Authentication ===" -ForegroundColor Cyan
Write-Host ""

$baseUrl = "http://localhost:5059/api"

# Test 1: Register a new company and user
Write-Host "Test 1: Register Company and Admin User" -ForegroundColor Yellow
$registerData = @{
    companyName = "Test Law Firm"
    email = "admin@testlawfirm.com"
    password = "Test123!@#"
    firstName = "John"
    lastName = "Smith"
    phoneNumber = "+1234567890"
} | ConvertTo-Json

try {
    $registerResponse = Invoke-RestMethod -Uri "$baseUrl/auth/register" -Method POST -Body $registerData -ContentType "application/json"
    Write-Host "[PASS] Registration successful!" -ForegroundColor Green
    Write-Host "Company ID: $($registerResponse.data.user.companyId)" -ForegroundColor Gray
    Write-Host "User ID: $($registerResponse.data.user.id)" -ForegroundColor Gray
    Write-Host "Email: $($registerResponse.data.user.email)" -ForegroundColor Gray
    Write-Host "Token: $($registerResponse.data.token.Substring(0, 50))..." -ForegroundColor Gray
    Write-Host ""
    
    # Save token for next tests
    $global:token = $registerResponse.data.token
    $global:companyId = $registerResponse.data.user.companyId
} catch {
    Write-Host "[FAIL] Registration failed: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Response: $($_.ErrorDetails.Message)" -ForegroundColor Red
    Write-Host ""
}

# Test 2: Login with the registered user
Write-Host "Test 2: Login with Credentials" -ForegroundColor Yellow
$loginData = @{
    email = "admin@testlawfirm.com"
    password = "Test123!@#"
} | ConvertTo-Json

try {
    $loginResponse = Invoke-RestMethod -Uri "$baseUrl/auth/login" -Method POST -Body $loginData -ContentType "application/json"
    Write-Host "✓ Login successful!" -ForegroundColor Green
    Write-Host "User: $($loginResponse.data.user.firstName) $($loginResponse.data.user.lastName)" -ForegroundColor Gray
    Write-Host "Role: $($loginResponse.data.user.role)" -ForegroundColor Gray
    Write-Host "Token: $($loginResponse.data.token.Substring(0, 50))..." -ForegroundColor Gray
    Write-Host ""
    
    # Update token
    $global:token = $loginResponse.data.token
} catch {
    Write-Host "✗ Login failed: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Response: $($_.ErrorDetails.Message)" -ForegroundColor Red
    Write-Host ""
}

# Test 3: Validate Token
Write-Host "Test 3: Validate JWT Token" -ForegroundColor Yellow
$validateData = @{
    token = $global:token
} | ConvertTo-Json

try {
    $validateResponse = Invoke-RestMethod -Uri "$baseUrl/auth/validate" -Method POST -Body $validateData -ContentType "application/json"
    Write-Host "✓ Token validation successful!" -ForegroundColor Green
    Write-Host "Valid: $($validateResponse.data.isValid)" -ForegroundColor Gray
    Write-Host "User ID: $($validateResponse.data.userId)" -ForegroundColor Gray
    Write-Host ""
} catch {
    Write-Host "✗ Token validation failed: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
}

# Test 4: Get current user info (protected endpoint)
Write-Host "Test 4: Get Current User Info [Authorize]" -ForegroundColor Yellow
try {
    $headers = @{
        "Authorization" = "Bearer $($global:token)"
    }
    $meResponse = Invoke-RestMethod -Uri "$baseUrl/auth/me" -Method GET -Headers $headers
    Write-Host "✓ Got user info successfully!" -ForegroundColor Green
    Write-Host "Name: $($meResponse.data.firstName) $($meResponse.data.lastName)" -ForegroundColor Gray
    Write-Host "Email: $($meResponse.data.email)" -ForegroundColor Gray
    Write-Host "Role: $($meResponse.data.role)" -ForegroundColor Gray
    Write-Host "Company ID: $($meResponse.data.companyId)" -ForegroundColor Gray
    Write-Host ""
} catch {
    Write-Host "✗ Failed to get user info: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
}

# Test 5: Try invalid login
Write-Host "Test 5: Invalid Login Credentials" -ForegroundColor Yellow
$invalidLoginData = @{
    email = "admin@testlawfirm.com"
    password = "WrongPassword123"
} | ConvertTo-Json

try {
    $invalidResponse = Invoke-RestMethod -Uri "$baseUrl/auth/login" -Method POST -Body $invalidLoginData -ContentType "application/json"
    Write-Host "✗ Should have failed but didn't!" -ForegroundColor Red
    Write-Host ""
} catch {
    Write-Host "✓ Correctly rejected invalid credentials!" -ForegroundColor Green
    Write-Host "Error: $($_.ErrorDetails.Message)" -ForegroundColor Gray
    Write-Host ""
}

# Test 6: Try accessing protected endpoint without token
Write-Host "Test 6: Access Protected Endpoint Without Token" -ForegroundColor Yellow
try {
    $noAuthResponse = Invoke-RestMethod -Uri "$baseUrl/auth/me" -Method GET
    Write-Host "✗ Should have failed but didn't!" -ForegroundColor Red
    Write-Host ""
} catch {
    Write-Host "✓ Correctly rejected unauthorized request!" -ForegroundColor Green
    Write-Host "Status: $($_.Exception.Response.StatusCode.value__)" -ForegroundColor Gray
    Write-Host ""
}

# Test 7: Verify multi-tenant data in database
Write-Host "Test 7: Verify Database Records" -ForegroundColor Yellow
Write-Host "Checking company and user in database..." -ForegroundColor Gray

try {
    $dbCheckResponse = Invoke-RestMethod -Uri "$baseUrl/test/company/$($global:companyId)" -Method GET
    Write-Host "✓ Company found in database!" -ForegroundColor Green
    Write-Host "Company Name: $($dbCheckResponse.data.name)" -ForegroundColor Gray
    Write-Host "Subscription: $($dbCheckResponse.data.subscriptionTier)" -ForegroundColor Gray
    Write-Host "Trial Ends: $($dbCheckResponse.data.trialEndsAt)" -ForegroundColor Gray
    Write-Host ""
} catch {
    Write-Host "✗ Database check failed: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
}

Write-Host "=== Authentication Tests Complete ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "Summary:" -ForegroundColor White
Write-Host "- Registration: Creates company + admin user with trial subscription" -ForegroundColor White
Write-Host "- Login: Returns JWT token with user claims" -ForegroundColor White
Write-Host "- Token Validation: Verifies JWT token integrity" -ForegroundColor White
Write-Host "- Authorization: Protected endpoints require valid token" -ForegroundColor White
Write-Host "- Multi-tenant: Company ID embedded in JWT claims" -ForegroundColor White
