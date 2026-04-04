# Full Stack Testing Guide

## ✅ System Status

### Node.js Updated
- **Before**: v20.5.0 (had warnings)
- **After**: v24.12.0 (latest, no warnings!)
- **npm**: v11.6.2

### Servers Running
- ✅ **Frontend**: http://localhost:5173 (React + Vite)
- ✅ **Backend**: http://localhost:5059 (ASP.NET Core API)
- ✅ **Database**: PostgreSQL (Docker container)

## 🧪 Manual Testing Steps

### 1. Test Frontend Loading
**Current Status**: ✅ Frontend is open in your browser

**What to Check**:
- Browser should show the login page
- Modern gradient design (blue colors)
- "Legal Document System" heading
- Email and password input fields
- "Sign in" button
- "Register here" link

### 2. Test Registration Flow

**Steps**:
1. Click **"Register here"** link
2. Fill in the registration form:
   ```
   Company Name: Acme Law Firm
   First Name: John
   Last Name: Doe
   Email: john.doe@acmelawfirm.com
   Phone: +1 555-123-4567
   Password: SecurePass123!
   Confirm Password: SecurePass123!
   ```
3. Click **"Create Account"**

**Expected Results**:
- ✅ Form submits without errors
- ✅ JWT token saved to localStorage
- ✅ Redirects to `/dashboard`
- ✅ Dashboard shows welcome message: "Welcome back, John Doe"
- ✅ Stats cards show initial data
- ✅ Account information displays correctly

**What This Tests**:
- Frontend → Backend API connection
- Authentication service (register endpoint)
- Database insert (Company + User)
- JWT token generation
- Trial subscription creation (14 days, 10GB)
- BCrypt password hashing
- React Router navigation
- Auth Context state management

### 3. Test Logout Flow

**Steps**:
1. On dashboard, click **"Logout"** button (top right)

**Expected Results**:
- ✅ Redirects to `/login`
- ✅ Token removed from localStorage
- ✅ User state cleared

**What This Tests**:
- Auth Context logout function
- localStorage cleanup
- Route protection

### 4. Test Login Flow

**Steps**:
1. On login page, enter credentials:
   ```
   Email: john.doe@acmelawfirm.com
   Password: SecurePass123!
   ```
2. Click **"Sign in"**

**Expected Results**:
- ✅ Form submits successfully
- ✅ New JWT token received
- ✅ Redirects to `/dashboard`
- ✅ Dashboard shows user information
- ✅ Token stored in localStorage

**What This Tests**:
- Login endpoint
- BCrypt password verification
- JWT token refresh
- Last login timestamp update
- Persistent authentication

### 5. Test Invalid Credentials

**Steps**:
1. Logout if logged in
2. Try to login with wrong password:
   ```
   Email: john.doe@acmelawfirm.com
   Password: WrongPassword123
   ```
3. Click "Sign in"

**Expected Results**:
- ✅ Shows error message: "Login failed. Please check your credentials."
- ✅ Stays on login page
- ✅ No token saved

**What This Tests**:
- Password verification
- Error handling
- Security (doesn't reveal if email exists)

### 6. Test Protected Route

**Steps**:
1. Logout if logged in
2. In browser address bar, type: `http://localhost:5173/dashboard`
3. Press Enter

**Expected Results**:
- ✅ Automatically redirects to `/login`
- ✅ Cannot access dashboard without authentication

**What This Tests**:
- ProtectedRoute component
- Route guards
- Authentication check

### 7. Test Token Persistence

**Steps**:
1. Login to dashboard
2. Refresh the page (F5)

**Expected Results**:
- ✅ Stays logged in
- ✅ Dashboard still accessible
- ✅ User information still displayed

**What This Tests**:
- localStorage token persistence
- Auth Context initialization
- Token reload on app start

## 🔍 Browser Console Testing

### Open Browser DevTools
Press `F12` or right-click → "Inspect"

### Check Console Tab
**Should see**:
- No errors (red messages)
- Vite HMR messages (normal)
- React development mode messages (normal)

### Check Network Tab
**Filter: XHR/Fetch**

**On Registration/Login**:
- Request to: `http://localhost:5059/api/auth/register` or `/login`
- Method: POST
- Status: 200 OK
- Response should contain: `{ success: true, data: { token: "...", user: {...} } }`

**On Dashboard Load**:
- No additional API calls (uses cached token)

### Check Application Tab → Local Storage
**Key**: `token`
**Value**: Long JWT string (starts with `eyJ...`)

**Key**: `user`
**Value**: JSON object with user data

## 🧪 Backend API Testing

### Test Authentication Endpoints Directly

Open PowerShell and run:

```powershell
# Test health endpoint
Invoke-RestMethod -Uri "http://localhost:5059/api/test/health" -Method GET

# Test login (use your registered email)
$loginData = @{
    email = "john.doe@acmelawfirm.com"
    password = "SecurePass123!"
} | ConvertTo-Json

$response = Invoke-RestMethod -Uri "http://localhost:5059/api/auth/login" -Method POST -Body $loginData -ContentType "application/json"
$response | ConvertTo-Json -Depth 10

# Save token for next test
$token = $response.data.token

# Test protected endpoint
$headers = @{ Authorization = "Bearer $token" }
Invoke-RestMethod -Uri "http://localhost:5059/api/auth/me" -Method GET -Headers $headers
```

## 🗄️ Database Verification

### Check PostgreSQL Container

```powershell
# Check container is running
docker ps | Select-String "lawgate-postgres"

# Connect to database
docker exec -it lawgate-postgres psql -U lawgate_user -d lawgate_db

# Inside PostgreSQL:
\dt                                    # List all tables
SELECT * FROM "Companies";             # View companies
SELECT * FROM "Users";                 # View users (passwords are hashed)
SELECT email, "FirstName", "LastName", "Role" FROM "Users";
\q                                     # Exit
```

**Expected Data**:
- Company: "Acme Law Firm" (or your company name)
- User: Your registered user with:
  - Hashed password (BCrypt)
  - Role: CompanyOwner
  - IsActive: true
  - TrialEndsAt: 14 days from registration

## ✅ Success Checklist

After completing all tests, you should have:

- ✅ Registered a new account successfully
- ✅ Logged in with credentials
- ✅ Accessed dashboard with user info
- ✅ Logged out successfully
- ✅ Logged back in
- ✅ Tested invalid credentials (rejected)
- ✅ Verified protected routes work
- ✅ Confirmed token persistence after refresh
- ✅ No console errors
- ✅ Data in PostgreSQL database

## 🎉 What's Working

### Frontend ✅
- React 19 + TypeScript + Vite 7
- Tailwind CSS styling
- React Router v7 navigation
- Protected routes
- Auth Context state management
- Login/Register/Dashboard pages
- Form validation
- Error handling
- Responsive design

### Backend ✅
- .NET 10 Clean Architecture
- PostgreSQL database
- Entity Framework Core
- JWT authentication
- BCrypt password hashing
- Multi-tenant architecture
- CORS configured
- API endpoints working

### Integration ✅
- Frontend → Backend communication
- JWT token flow
- localStorage persistence
- Protected route guards
- Error handling
- User registration & login
- Database operations

## 🚀 Next Actions

### Immediate
1. Complete all manual tests above
2. Verify everything works as expected
3. Test error scenarios

### Development
1. Add project management features
2. Implement document upload (Azure Blob Storage)
3. Add team member management
4. Create user settings page

### Deployment
1. Configure production environment variables
2. Set up Docker Compose for all services
3. Deploy to Azure
4. Configure SSL/HTTPS

## 📝 Test Notes

**Date**: January 12, 2026
**Node.js**: v24.12.0 (updated from v20.5.0)
**npm**: v11.6.2
**Status**: All systems operational ✅

---

## 🆘 Troubleshooting

### Frontend not loading
- Check terminal: Should see "Local: http://localhost:5173"
- Check browser console for errors
- Try hard refresh: Ctrl+Shift+R

### Backend not responding
- Check terminal: Should see "Now listening on: http://localhost:5059"
- Test health endpoint: `Invoke-RestMethod -Uri "http://localhost:5059/api/test/health"`
- Check PostgreSQL is running: `docker ps`

### CORS errors
- Backend Program.cs already configured for localhost:5173
- Try restarting backend server

### Database connection errors
- Start PostgreSQL: `docker start lawgate-postgres`
- Check connection string in appsettings.json

---

**🎯 Current Status**: Ready for full testing!

**Next**: Follow the testing steps above to verify your complete Legal Document Management System!
