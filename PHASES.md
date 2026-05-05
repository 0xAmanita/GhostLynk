# GhostLynk - Development Phases

**Version:** 2.0.0  
**Last Updated:** 2026-05-06

This document outlines the granular development phases for building GhostLynk from scratch.

---

## Phase 0: Project Setup & Infrastructure

### 0.1 Database Setup
- [ ] Install PostgreSQL 14+
- [ ] Create `ghostlynk` database
- [ ] Run `schema.sql` to create all tables
- [ ] Verify all tables, indexes, and triggers created
- [ ] Test database triggers (updated_at, auto-lock, token invalidation)
- [ ] Set up database backup strategy

### 0.2 ASP.NET Core Backend Setup
- [ ] Create new ASP.NET Core 8 Web API project
- [ ] Install NuGet packages:
  - `Npgsql.EntityFrameworkCore.PostgreSQL`
  - `BCrypt.Net-Next`
  - `System.IdentityModel.Tokens.Jwt`
  - `Microsoft.AspNetCore.Authentication.JwtBearer`
- [ ] Configure `appsettings.json` with database connection
- [ ] Create `.env` file for secrets
- [ ] Set up project structure (Controllers, Services, Models, Data)

### 0.3 Django Backend Setup
- [ ] Create Django project `backend_admin`
- [ ] Install Python packages:
  - `django`
  - `djangorestframework`
  - `psycopg2-binary`
  - `python-decouple`
  - `django-cors-headers`
- [ ] Configure `settings.py` for PostgreSQL
- [ ] Create Django app `api`
- [ ] Configure CORS for frontend access

### 0.4 React Frontend Setup
- [ ] Create Vite + React project
- [ ] Install dependencies:
  - `react-router-dom`
  - `axios`
  - `jwt-decode`
- [ ] Set up project structure (components, services, utils)
- [ ] Configure `.env` for API URLs
- [ ] Set up basic routing

### 0.5 Nginx Setup
- [ ] Install Nginx
- [ ] Create `nginx.conf` for reverse proxy
- [ ] Configure routes for frontend, ASP.NET, Django
- [ ] Test proxy configuration

---
## Phase 1: Database Models & Context

### 1.1 ASP.NET Core - Entity Models
- [ ] Create `Models/User.cs`
  - Properties: Id, Email, Username, FirstName, LastName, Address, PasswordHash, CreatedAt, UpdatedAt
- [ ] Create `Models/UrlEntry.cs`
  - Properties: Id, UserId, OriginalUrl, ObfuscatedUrl, Nickname, PasskeyHash, FailedAttempts, IsLocked, CreatedAt, UpdatedAt
- [ ] Create `Models/IpMetadata.cs`
  - Properties: Id, UrlEntryId, IpAddress, City, Region, Country, Org, Timezone, FetchedAt
- [ ] Create `Models/Session.cs`
  - Properties: Id, UserId, SessionToken, LastSubmitAt, LastDeobfuscateAt, CreatedAt, ExpiresAt
- [ ] Create `Models/PasswordResetToken.cs`
  - Properties: Id, UserId, TokenHash, ResendEmailId, ExpiresAt, UsedAt, CreatedAt

### 1.2 ASP.NET Core - DbContext
- [ ] Create `Data/AppDbContext.cs`
- [ ] Configure DbSet for all models
- [ ] Configure entity relationships (foreign keys)
- [ ] Configure indexes via Fluent API
- [ ] Test database connection

### 1.3 Django - Models (Read-Only)
- [ ] Create `api/models.py`
- [ ] Define User model (managed=False)
- [ ] Define UrlEntry model (managed=False)
- [ ] Define IpMetadata model (managed=False)
- [ ] Define AdminLog model (managed=True)
- [ ] Test Django database connection

---
## Phase 2: Core Services (ASP.NET Core)

### 2.1 JWT Service
- [ ] Create `Services/JwtService.cs`
- [ ] Implement `GenerateToken(User user)` method
- [ ] Implement `ValidateToken(string token)` method
- [ ] Implement `GetUserIdFromToken(string token)` method
- [ ] Configure JWT settings in `appsettings.json`
- [ ] Write unit tests for JWT generation and validation

### 2.2 Obfuscation Service
- [ ] Create `Services/ObfuscationService.cs`
- [ ] Implement Layer 1: Caesar Cipher
  - `CaesarEncrypt(string input, int shift)`
  - `CaesarDecrypt(string input, int shift)`
- [ ] Implement Layer 2: XOR Encoding
  - `XorEncode(byte[] data, byte[] key)`
  - `XorDecode(byte[] data, byte[] key)` (same as encode)
- [ ] Implement Layer 3: CarlSuello Algorithm
  - `CarlSuelloTransform(byte[] data)`
  - `CarlSuelloReverse(byte[] data)`
- [ ] Implement Layer 4: Base64
  - `Base64Encode(byte[] data)`
  - `Base64Decode(string data)`
- [ ] Implement full pipeline:
  - `Obfuscate(string url)` - chains all 4 layers
  - `Deobfuscate(string obfuscated)` - reverse pipeline
- [ ] Write unit tests for each layer
- [ ] Write integration tests for full pipeline

### 2.3 Rate Limit Service
- [ ] Create `Services/RateLimitService.cs`
- [ ] Implement `CheckSubmissionRateLimit(Guid userId)` method
- [ ] Implement `CheckDeobfuscationRateLimit(Guid userId)` method
- [ ] Implement `UpdateLastSubmitTime(Guid userId)` method
- [ ] Implement `UpdateLastDeobfuscateTime(Guid userId)` method
- [ ] Write unit tests for rate limiting logic

### 2.4 IP Info Service
- [ ] Create `Services/IpInfoService.cs`
- [ ] Implement `GetIpMetadata(string ipAddress)` method
- [ ] Configure ipinfo.io API token in environment
- [ ] Handle API errors gracefully
- [ ] Write integration tests with mock API

### 2.5 Email Service (Render)
- [ ] Create `Services/EmailService.cs`
- [ ] Implement `SendPasswordResetEmail(string email, string token)` method
- [ ] Configure Render API credentials
- [ ] Create email template for password reset
- [ ] Handle email delivery errors
- [ ] Write integration tests with mock email service

---
## Phase 3: Authentication APIs (ASP.NET Core)

### 3.1 User Registration
- [ ] Create `Controllers/AuthController.cs`
- [ ] Implement `POST /api/auth/register` endpoint
- [ ] Validate input fields:
  - Email format and uniqueness
  - Username format and uniqueness
  - Password strength requirements
  - Password match confirmation
- [ ] BCrypt hash password (cost factor 12)
- [ ] Insert user into database
- [ ] Return success response with user ID
- [ ] Handle validation errors (400)
- [ ] Write integration tests

### 3.2 User Login
- [ ] Implement `POST /api/auth/login` endpoint
- [ ] Accept email OR username + password
- [ ] Query user from database
- [ ] BCrypt verify password
- [ ] Generate JWT token via JwtService
- [ ] Create session record in database
- [ ] Return JWT token + user info
- [ ] Handle invalid credentials (401)
- [ ] Write integration tests

### 3.3 Forgot Password
- [ ] Implement `POST /api/auth/forgot-password` endpoint
- [ ] Accept email address
- [ ] Find user by email (silent fail if not found)
- [ ] Generate cryptographically secure 32-byte token
- [ ] SHA-256 hash token for storage
- [ ] Insert into `password_reset_tokens` table (15min expiry)
- [ ] Send email via EmailService with plaintext token
- [ ] Store Render email ID
- [ ] Always return success (prevent email enumeration)
- [ ] Write integration tests

### 3.4 Reset Password
- [ ] Implement `POST /api/auth/reset-password` endpoint
- [ ] Accept token + new password + confirmation
- [ ] SHA-256 hash provided token
- [ ] Query `password_reset_tokens` by hash
- [ ] Validate token exists, not used, not expired
- [ ] Validate new password strength
- [ ] BCrypt hash new password
- [ ] Update user's password_hash
- [ ] Mark token as used
- [ ] Delete all user sessions
- [ ] Return success
- [ ] Handle invalid/expired token (400)
- [ ] Write integration tests

### 3.5 JWT Middleware
- [ ] Create `Middleware/JwtAuthMiddleware.cs`
- [ ] Extract JWT from Authorization header
- [ ] Validate token via JwtService
- [ ] Attach user ID to HttpContext
- [ ] Handle missing/invalid token (401)
- [ ] Register middleware in Program.cs

---
## Phase 4: URL Management APIs (ASP.NET Core)

### 4.1 URL Submission
- [ ] Create `Controllers/UrlsController.cs`
- [ ] Implement `POST /api/urls/submit` endpoint
- [ ] Require JWT authentication
- [ ] Extract user ID from JWT
- [ ] Check submission rate limit (5 minutes)
- [ ] Return 429 if rate limited
- [ ] Validate URL format
- [ ] Validate nickname (1-50 chars)
- [ ] Validate passkey (min 4 chars)
- [ ] BCrypt hash passkey
- [ ] Extract client IP address
- [ ] Call IpInfoService to get metadata
- [ ] Call ObfuscationService to obfuscate URL
- [ ] Insert into `url_entries` table
- [ ] Insert into `ip_metadata` table
- [ ] Update session's `last_submit_at`
- [ ] Return obfuscated URL + metadata
- [ ] Handle errors gracefully
- [ ] Write integration tests

### 4.2 Public Feed
- [ ] Implement `GET /api/urls/feed` endpoint
- [ ] Require JWT authentication
- [ ] Accept pagination parameters (page, limit)
- [ ] Query `url_entries` ordered by `created_at DESC`
- [ ] Return only: obfuscated URL, nickname, timestamp
- [ ] Do NOT include: original URL, IP metadata, user ID
- [ ] Return pagination metadata
- [ ] Write integration tests

### 4.3 Deobfuscation
- [ ] Implement `POST /api/urls/deobfuscate` endpoint
- [ ] Require JWT authentication
- [ ] Extract user ID from JWT
- [ ] Check deobfuscation rate limit (5 minutes)
- [ ] Return 429 if rate limited
- [ ] Accept: obfuscated text, nickname, passkey
- [ ] Query entry by obfuscated_url + nickname
- [ ] Return 401 if no match (generic error)
- [ ] Check if entry is locked
- [ ] Return 423 if locked
- [ ] BCrypt verify passkey
- [ ] If wrong passkey:
  - Increment failed_attempts
  - Lock entry if failed_attempts >= 3
  - Return 401 (generic error)
- [ ] If correct passkey:
  - Reset failed_attempts to 0
  - Update session's `last_deobfuscate_at`
  - Call ObfuscationService to deobfuscate
  - Return original URL + metadata
- [ ] Write integration tests for all scenarios

---
## Phase 5: Admin Backend (Django)

### 5.1 Django Admin Authentication
- [ ] Configure Django admin site
- [ ] Create superuser account
- [ ] Implement `POST /api/admin/login` endpoint
- [ ] Validate admin credentials
- [ ] Create Django session
- [ ] Return session cookie
- [ ] Implement logout endpoint
- [ ] Write integration tests

### 5.2 Admin - View All Entries
- [ ] Create `api/views.py`
- [ ] Implement `GET /api/admin/urls` endpoint
- [ ] Require admin authentication
- [ ] Accept pagination and filter parameters
- [ ] Query `url_entries` with JOIN to `ip_metadata`
- [ ] Return all fields including plaintext URLs
- [ ] Return pagination metadata
- [ ] Write integration tests

### 5.3 Admin - Create Entry
- [ ] Implement `POST /api/admin/urls` endpoint
- [ ] Require admin authentication
- [ ] Accept: user_id, url, nickname, passkey
- [ ] Validate inputs
- [ ] Call ASP.NET obfuscation logic (or duplicate in Python)
- [ ] Insert into `url_entries`
- [ ] Log action in `admin_log` table
- [ ] Return created entry
- [ ] Write integration tests

### 5.4 Admin - Update Entry
- [ ] Implement `PUT /api/admin/urls/{id}` endpoint
- [ ] Require admin authentication
- [ ] Accept: original_url, nickname
- [ ] Find entry by ID
- [ ] Store old values for audit log
- [ ] If URL changed, re-obfuscate
- [ ] Update entry in database
- [ ] Log action in `admin_log` with old/new values
- [ ] Return updated entry
- [ ] Write integration tests

### 5.5 Admin - Delete Entry
- [ ] Implement `DELETE /api/admin/urls/{id}` endpoint
- [ ] Require admin authentication
- [ ] Find entry by ID
- [ ] Store entry data for audit log
- [ ] Delete entry (cascades to ip_metadata)
- [ ] Log deletion in `admin_log`
- [ ] Return success message
- [ ] Write integration tests

### 5.6 Admin - Bulk Delete
- [ ] Implement `POST /api/admin/urls/bulk-delete` endpoint
- [ ] Require admin authentication
- [ ] Accept array of entry IDs
- [ ] Validate all IDs exist
- [ ] Delete all entries
- [ ] Log each deletion in `admin_log`
- [ ] Return count of deleted entries
- [ ] Write integration tests

### 5.7 Admin - Unlock Entry
- [ ] Implement `PATCH /api/admin/urls/{id}/unlock` endpoint
- [ ] Require admin authentication
- [ ] Find entry by ID
- [ ] Store old values for audit log
- [ ] Set `failed_attempts = 0` and `is_locked = FALSE`
- [ ] Log unlock action in `admin_log`
- [ ] Return updated entry
- [ ] Write integration tests

### 5.8 Admin - View Audit Log
- [ ] Implement `GET /api/admin/logs` endpoint
- [ ] Require admin authentication
- [ ] Accept pagination and action filter
- [ ] Query `admin_log` ordered by `performed_at DESC`
- [ ] Return log entries with pagination
- [ ] Write integration tests

### 5.9 Django Serializers
- [ ] Create `api/serializers.py`
- [ ] Create serializers for all models
- [ ] Configure field inclusion/exclusion
- [ ] Add validation rules

---
## Phase 6: Frontend Development (React)

### 6.1 Authentication UI
- [ ] Create `components/Auth/Register.jsx`
  - Form fields: email, username, firstName, lastName, address, password, passwordRepeat
  - Client-side validation
  - Call `/api/auth/register`
  - Display success/error messages
  - Redirect to login on success
- [ ] Create `components/Auth/Login.jsx`
  - Form fields: emailOrUsername, password
  - Call `/api/auth/login`
  - Store JWT in localStorage
  - Redirect to feed on success
- [ ] Create `components/Auth/ForgotPassword.jsx`
  - Form field: email
  - Call `/api/auth/forgot-password`
  - Display success message
- [ ] Create `components/Auth/ResetPassword.jsx`
  - Extract token from URL query params
  - Form fields: newPassword, newPasswordRepeat
  - Call `/api/auth/reset-password`
  - Redirect to login on success

### 6.2 URL Management UI
- [ ] Create `components/Urls/SubmitForm.jsx`
  - Form fields: url, nickname, passkey
  - Validate URL format client-side
  - Include JWT in Authorization header
  - Call `/api/urls/submit`
  - Display obfuscated result
  - Handle rate limit errors (429)
  - Show retry countdown
- [ ] Create `components/Urls/Feed.jsx`
  - Fetch from `/api/urls/feed`
  - Display obfuscated URLs with nickname and timestamp
  - Implement pagination
  - Auto-refresh every 30 seconds (optional)
- [ ] Create `components/Urls/DeobfuscatePanel.jsx`
  - Form fields: obfuscatedText, nickname, passkey
  - No hints or tooltips
  - Include JWT in Authorization header
  - Call `/api/urls/deobfuscate`
  - Display original URL on success
  - Handle generic error (401)
  - Handle locked entry error (423)
  - Handle rate limit error (429)

### 6.3 Layout Components
- [ ] Create `components/Layout/Navbar.jsx`
  - Links: Home, Submit, Deobfuscate, Feed
  - Show username if logged in
  - Logout button (clears localStorage)
- [ ] Create `components/Layout/Footer.jsx`
  - Copyright and version info

### 6.4 Services Layer
- [ ] Create `services/authService.js`
  - `register(userData)`
  - `login(credentials)`
  - `forgotPassword(email)`
  - `resetPassword(token, newPassword)`
  - `logout()`
- [ ] Create `services/urlService.js`
  - `submitUrl(urlData, token)`
  - `getFeed(page, limit, token)`
  - `deobfuscate(data, token)`
- [ ] Create `utils/jwtHelper.js`
  - `storeToken(token)`
  - `getToken()`
  - `removeToken()`
  - `isTokenExpired(token)`
  - `getUserFromToken(token)`
- [ ] Create `utils/validators.js`
  - `validateEmail(email)`
  - `validatePassword(password)`
  - `validateUrl(url)`

### 6.5 Routing
- [ ] Configure React Router in `App.jsx`
- [ ] Routes:
  - `/` - Home/Landing page
  - `/register` - Registration form
  - `/login` - Login form
  - `/forgot-password` - Forgot password form
  - `/reset-password` - Reset password form
  - `/submit` - URL submission form (protected)
  - `/feed` - Public feed (protected)
  - `/deobfuscate` - Deobfuscation panel (protected)
- [ ] Implement protected route wrapper
- [ ] Redirect to login if not authenticated

### 6.6 Styling
- [ ] Set up CSS framework (Tailwind CSS or custom)
- [ ] Create responsive layouts
- [ ] Style all forms and buttons
- [ ] Add loading spinners
- [ ] Add error/success toast notifications

---
## Phase 7: Integration & Testing

### 7.1 Backend Integration Testing
- [ ] Test user registration → login flow
- [ ] Test JWT generation and validation
- [ ] Test URL submission with rate limiting
- [ ] Test obfuscation/deobfuscation pipeline
- [ ] Test passkey verification and locking
- [ ] Test forgot password → reset flow
- [ ] Test email delivery (Render integration)
- [ ] Test ipinfo.io integration
- [ ] Test admin CRUD operations
- [ ] Test admin audit logging

### 7.2 Frontend Integration Testing
- [ ] Test all forms with valid inputs
- [ ] Test all forms with invalid inputs
- [ ] Test JWT storage and retrieval
- [ ] Test protected route access
- [ ] Test rate limit UI feedback
- [ ] Test error message display
- [ ] Test pagination in feed

### 7.3 End-to-End Testing
- [ ] User registration → login → submit URL → view feed
- [ ] User deobfuscates own entry successfully
- [ ] User fails deobfuscation 3 times → entry locks
- [ ] Admin unlocks entry
- [ ] User forgets password → receives email → resets password
- [ ] Admin views all entries with IP metadata
- [ ] Admin deletes entry → verify cascade

### 7.4 Load Testing
- [ ] Test 100 concurrent registrations
- [ ] Test 100 concurrent URL submissions
- [ ] Test 100 concurrent deobfuscations
- [ ] Verify rate limiting under load
- [ ] Monitor database performance
- [ ] Monitor API response times

### 7.5 Security Testing
- [ ] Test SQL injection attempts
- [ ] Test XSS attempts
- [ ] Test JWT tampering
- [ ] Test password reset token tampering
- [ ] Test rate limit bypass attempts
- [ ] Test unauthorized admin access attempts

---
## Phase 8: Deployment

### 8.1 Production Database Setup
- [ ] Provision PostgreSQL server (AWS RDS, DigitalOcean, etc.)
- [ ] Configure database security (firewall, SSL)
- [ ] Run schema.sql on production database
- [ ] Set up automated backups
- [ ] Configure connection pooling

### 8.2 ASP.NET Core Deployment
- [ ] Build production release: `dotnet publish -c Release`
- [ ] Configure production `appsettings.json`
- [ ] Set environment variables (secrets)
- [ ] Deploy to server (Linux VM, Docker, etc.)
- [ ] Configure systemd service for auto-restart
- [ ] Test API endpoints

### 8.3 Django Deployment
- [ ] Install production dependencies
- [ ] Configure production `settings.py`
- [ ] Set `DEBUG=False`
- [ ] Collect static files: `python manage.py collectstatic`
- [ ] Configure Gunicorn or uWSGI
- [ ] Configure systemd service
- [ ] Test admin endpoints

### 8.4 Frontend Deployment
- [ ] Build production bundle: `npm run build`
- [ ] Upload `dist/` folder to server
- [ ] Configure Nginx to serve static files
- [ ] Test all routes and API calls

### 8.5 Nginx Configuration
- [ ] Configure SSL certificate (Let's Encrypt)
- [ ] Set up HTTPS redirect
- [ ] Configure reverse proxy for both backends
- [ ] Enable HSTS header
- [ ] Configure CORS headers
- [ ] Test all routes

### 8.6 External Services
- [ ] Configure ipinfo.io API token
- [ ] Configure Render email service
- [ ] Test email delivery in production
- [ ] Test IP geolocation in production

### 8.7 Monitoring Setup
- [ ] Set up application logging (Serilog, Django logging)
- [ ] Configure log aggregation (optional)
- [ ] Set up uptime monitoring
- [ ] Configure error alerting
- [ ] Set up performance monitoring

### 8.8 Periodic Jobs
- [ ] Set up cron job for session cleanup
- [ ] Set up cron job for token cleanup
- [ ] Test cleanup functions

---

## Phase 9: Post-Launch

### 9.1 Monitoring & Maintenance
- [ ] Monitor error rates
- [ ] Monitor API response times
- [ ] Monitor database performance
- [ ] Monitor disk space
- [ ] Review logs regularly

### 9.2 Bug Fixes & Optimization
- [ ] Address reported bugs
- [ ] Optimize slow queries
- [ ] Improve frontend performance
- [ ] Reduce bundle size

### 9.3 Documentation
- [ ] Update README.md with deployment instructions
- [ ] Document API endpoints (Swagger/OpenAPI)
- [ ] Create user guide
- [ ] Create admin guide

### 9.4 Future Enhancements (Optional)
- [ ] Email verification on registration
- [ ] Two-factor authentication (2FA)
- [ ] User profile management
- [ ] Entry expiration feature
- [ ] Custom obfuscation keys per user
- [ ] WebSocket for real-time feed
- [ ] Mobile app (React Native)
- [ ] Export entries to CSV (admin)
- [ ] Advanced search and filtering

---

## Development Timeline Estimate

| Phase | Estimated Time | Dependencies |
|-------|----------------|--------------|
| Phase 0: Setup | 1-2 days | None |
| Phase 1: Models | 1 day | Phase 0 |
| Phase 2: Services | 3-4 days | Phase 1 |
| Phase 3: Auth APIs | 2-3 days | Phase 2 |
| Phase 4: URL APIs | 2-3 days | Phase 2, 3 |
| Phase 5: Admin Backend | 3-4 days | Phase 1 |
| Phase 6: Frontend | 5-7 days | Phase 3, 4 |
| Phase 7: Testing | 3-5 days | Phase 6 |
| Phase 8: Deployment | 2-3 days | Phase 7 |
| Phase 9: Post-Launch | Ongoing | Phase 8 |
| **Total** | **22-34 days** | |

---

## Priority Checklist

### Must-Have (MVP)
- [x] User registration and login
- [x] JWT authentication
- [x] URL submission with obfuscation
- [x] Public feed
- [x] Deobfuscation with passkey
- [x] Rate limiting
- [x] Entry locking after failed attempts
- [x] Admin dashboard with CRUD
- [x] IP geolocation tracking

### Should-Have
- [x] Forgot/reset password
- [x] Admin audit logging
- [x] Pagination
- [x] Error handling and validation

### Nice-to-Have
- [ ] Email verification
- [ ] 2FA
- [ ] Real-time feed updates
- [ ] Advanced admin filtering
- [ ] Export functionality

---

**End of Development Phases**
