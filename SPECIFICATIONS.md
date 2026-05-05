# GhostLynk - Technical Specifications

**Version:** 2.0.0  
**Last Updated:** 2026-05-06  
**Database:** PostgreSQL  
**Email Service:** Render

---

## 1. Project Overview

GhostLynk is a full-stack web application where registered users can submit a website URL with a nickname and a secret passkey. The backend stores the URL, enriches it with IP geolocation via ipinfo.io, and applies a custom multi-layer obfuscation pipeline (Caesar Cipher → XOR → Custom Algorithm → Base64). The public feed shows only obfuscated output. A user who knows the correct nickname and passkey for an entry can deobfuscate it to reveal the original URL.

A single privileged Admin account can view all entries in plaintext via a protected dashboard and has full CRUD control. Per-user rate limiting is enforced on both submission and deobfuscation using JWT authentication.

### Architecture

The system operates on a dual-backend microservice-style architecture:
- **Public API:** ASP.NET Core 8 (C#) - handles user authentication, URL submission, deobfuscation
- **Admin API:** Python Django - manages admin dashboard and administrative operations
- **Database:** PostgreSQL (shared between both backends)
- **Email Service:** Render (for password reset emails)

---

## 2. Tech Stack

| Component | Technology |
|-----------|-----------|
| Backend (Public) | ASP.NET Core 8 Web API (C#) |
| Backend (Admin) | Python Django & Django REST Framework |
| Frontend | React + Vite |
| Database | PostgreSQL |
| Email Service | Render |
| External API | ipinfo.io |
| Reverse Proxy | Nginx |
| Authentication | JWT (JSON Web Tokens) |
| Password Hashing | BCrypt |

---
## 3. Database Schema (PostgreSQL)

### 3.1 Table: users
Stores registered user accounts.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| id | UUID | PRIMARY KEY | Auto-generated user ID |
| email | VARCHAR(255) | NOT NULL, UNIQUE | User email address |
| username | VARCHAR(100) | NOT NULL, UNIQUE | Unique username |
| first_name | VARCHAR(100) | NOT NULL | User's first name |
| last_name | VARCHAR(100) | NOT NULL | User's last name |
| address | TEXT | NOT NULL | User's address |
| password_hash | TEXT | NOT NULL | BCrypt hashed password |
| created_at | TIMESTAMPTZ | NOT NULL | Account creation timestamp |
| updated_at | TIMESTAMPTZ | NOT NULL | Last update timestamp |

**Indexes:** email, username  
**Managed by:** ASP.NET Core (Full CRUD), Django (Read, Delete)

### 3.2 Table: url_entries
Stores submitted URL entries with obfuscation data.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| id | UUID | PRIMARY KEY | Auto-generated entry ID |
| user_id | UUID | NOT NULL, FK → users(id) | Owner of the entry |
| original_url | TEXT | NOT NULL | Original plaintext URL |
| obfuscated_url | TEXT | NOT NULL, UNIQUE | Obfuscated URL output |
| nickname | VARCHAR(50) | NOT NULL | Entry nickname |
| passkey_hash | TEXT | NOT NULL | BCrypt hashed passkey |
| failed_attempts | INTEGER | NOT NULL, DEFAULT 0 | Wrong passkey attempts |
| is_locked | BOOLEAN | NOT NULL, DEFAULT FALSE | Entry lock status |
| created_at | TIMESTAMPTZ | NOT NULL | Entry creation timestamp |
| updated_at | TIMESTAMPTZ | NOT NULL | Last update timestamp |

**Indexes:** user_id, obfuscated_url, is_locked  
**Managed by:** ASP.NET Core (INSERT, READ), Django (Full CRUD + UNLOCK)

### 3.3 Table: ip_metadata
Stores IP geolocation data from ipinfo.io (one-to-one with url_entries).

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| id | UUID | PRIMARY KEY | Auto-generated metadata ID |
| url_entry_id | UUID | NOT NULL, UNIQUE, FK → url_entries(id) | Associated entry |
| ip_address | VARCHAR(45) | | IP address |
| city | VARCHAR(100) | | City name |
| region | VARCHAR(100) | | Region/state |
| country | VARCHAR(10) | | Country code |
| org | VARCHAR(255) | | Organization/ISP |
| timezone | VARCHAR(100) | | Timezone |
| fetched_at | TIMESTAMPTZ | NOT NULL | Metadata fetch timestamp |

**Indexes:** url_entry_id  
**Managed by:** ASP.NET Core (INSERT, READ), Django (Read only)

### 3.4 Table: sessions
Tracks per-user rate limit timestamps.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| id | UUID | PRIMARY KEY | Auto-generated session ID |
| user_id | UUID | NOT NULL, FK → users(id) | Session owner |
| session_token | VARCHAR(255) | NOT NULL, UNIQUE | JWT token identifier |
| last_submit_at | TIMESTAMPTZ | | Last URL submission time |
| last_deobfuscate_at | TIMESTAMPTZ | | Last deobfuscation time |
| created_at | TIMESTAMPTZ | NOT NULL | Session creation time |
| expires_at | TIMESTAMPTZ | NOT NULL | Session expiration time |

**Indexes:** user_id, session_token, expires_at  
**Managed by:** ASP.NET Core (Full CRUD), Django (None)

### 3.5 Table: password_reset_tokens
Stores password reset tokens for forgot-password flow.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| id | UUID | PRIMARY KEY | Auto-generated token ID |
| user_id | UUID | NOT NULL, FK → users(id) | User requesting reset |
| token_hash | VARCHAR(255) | NOT NULL, UNIQUE | SHA-256 hashed token |
| resend_email_id | VARCHAR(255) | | Render email tracking ID |
| expires_at | TIMESTAMPTZ | NOT NULL | Token expiration (15 min) |
| used_at | TIMESTAMPTZ | | Token usage timestamp |
| created_at | TIMESTAMPTZ | NOT NULL | Token creation timestamp |

**Indexes:** user_id, token_hash, expires_at  
**Managed by:** ASP.NET Core (Full CRUD), Django (None)

### 3.6 Table: admin_log
Audit trail for admin actions.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| id | UUID | PRIMARY KEY | Auto-generated log ID |
| action | VARCHAR(50) | NOT NULL | Action type (CREATE, UPDATE, DELETE, UNLOCK) |
| target_table | VARCHAR(100) | NOT NULL | Affected table name |
| target_id | UUID | NOT NULL | Affected record ID |
| old_value | JSONB | | Previous state (JSON) |
| new_value | JSONB | | New state (JSON) |
| performed_at | TIMESTAMPTZ | NOT NULL | Action timestamp |

**Indexes:** target_id, performed_at, action  
**Managed by:** ASP.NET Core (None), Django (INSERT, READ)

---
## 4. User Roles

### 4.1 REGISTERED USER (Account Required)

**Capabilities:**
- Creates an account via registration with email verification
- Logs in to generate a JWT token
- Submits URLs with nickname and passkey per entry
- Views all entries as obfuscated text with nickname and timestamp
- Deobfuscates entries using correct obfuscated text, nickname, and passkey
- Requests password reset via email if forgotten

**Restrictions:**
- Rate limited: 1 URL submission per 5-minute window per user account
- Rate limited: 1 deobfuscation attempt per 5-minute window per user account
- Max 3 wrong passkey attempts before entry locks
- Cannot edit or delete any entry
- Cannot view other users' plaintext URLs

### 4.2 ADMIN (Single account, login required)

**Capabilities:**
- Logs in via Django `/api/admin/login` or Django Admin portal
- Views dashboard with all entries in plaintext
- Sees: URL, obfuscated text, nickname, timestamp, IP metadata, lock status, user ID
- Full CRUD: create, read, update, delete any entry
- Unlocks passkey-locked entries (resets failed_attempts and is_locked)
- Views admin audit log
- Bypasses all rate limiting

**Restrictions:**
- None (full system access)

---
## 5. Feature Specifications

### 5.1 User Registration

**Endpoint:** `POST /api/auth/register`

**Request Body:**
```json
{
  "email": "user@example.com",
  "username": "ghostuser",
  "firstName": "John",
  "lastName": "Doe",
  "address": "123 Main St, City, Country",
  "password": "SecurePass123!",
  "passwordRepeat": "SecurePass123!"
}
```

**Validation Rules:**
- Email: valid format, unique in database
- Username: 3-100 chars, alphanumeric + underscore, unique
- First name: 1-100 chars, required
- Last name: 1-100 chars, required
- Address: non-empty text
- Password: min 8 chars, must contain uppercase, lowercase, digit, special char
- Password repeat: must match password

**Process:**
1. Validate all fields
2. Check email and username uniqueness
3. BCrypt hash the password (cost factor: 12)
4. Insert user record into `users` table
5. Return success with user ID

**Response (Success - 201):**
```json
{
  "userId": "uuid",
  "email": "user@example.com",
  "username": "ghostuser",
  "message": "Registration successful"
}
```

**Response (Error - 400):**
```json
{
  "error": "ValidationFailed",
  "details": {
    "email": "Email already exists",
    "username": "Username already taken"
  }
}
```

### 5.2 User Login

**Endpoint:** `POST /api/auth/login`

**Request Body:**
```json
{
  "emailOrUsername": "user@example.com",
  "password": "SecurePass123!"
}
```

**Process:**
1. Find user by email OR username
2. BCrypt verify password against stored hash
3. Generate JWT token (expiry: 24 hours)
4. Create session record in `sessions` table
5. Return JWT token

**JWT Payload:**
```json
{
  "sub": "user-uuid",
  "email": "user@example.com",
  "username": "ghostuser",
  "iat": 1234567890,
  "exp": 1234654290
}
```

**Response (Success - 200):**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresAt": "2026-05-07T03:44:10Z",
  "user": {
    "id": "uuid",
    "email": "user@example.com",
    "username": "ghostuser"
  }
}
```

**Response (Error - 401):**
```json
{
  "error": "InvalidCredentials"
}
```

### 5.3 Forgot Password

**Endpoint:** `POST /api/auth/forgot-password`

**Request Body:**
```json
{
  "email": "user@example.com"
}
```

**Process:**
1. Find user by email (if not found, still return success to prevent email enumeration)
2. Generate cryptographically secure random token (32 bytes)
3. SHA-256 hash the token for storage
4. Store hashed token in `password_reset_tokens` table with 15-minute expiry
5. Invalidate all previous unused tokens for this user (via trigger)
6. Send email via Render with plaintext token in reset link
7. Store Render email ID in `resend_email_id` field
8. Return success (always, even if email doesn't exist)

**Email Template (via Render):**
```
Subject: GhostLynk - Password Reset Request

Hello,

You requested a password reset for your GhostLynk account.

Click the link below to reset your password:
https://ghostlynk.com/reset-password?token={plaintext_token}

This link expires in 15 minutes.

If you didn't request this, please ignore this email.

- GhostLynk Team
```

**Response (Success - 200):**
```json
{
  "message": "If the email exists, a reset link has been sent"
}
```

### 5.4 Reset Password

**Endpoint:** `POST /api/auth/reset-password`

**Request Body:**
```json
{
  "token": "plaintext-token-from-email",
  "newPassword": "NewSecurePass123!",
  "newPasswordRepeat": "NewSecurePass123!"
}
```

**Process:**
1. SHA-256 hash the provided token
2. Query `password_reset_tokens` by `token_hash`
3. Check token exists, not used (`used_at IS NULL`), and not expired
4. Validate new password (same rules as registration)
5. BCrypt hash new password
6. Update user's `password_hash` in `users` table
7. Mark token as used (`used_at = NOW()`)
8. Invalidate all user sessions (delete from `sessions` table)
9. Return success

**Response (Success - 200):**
```json
{
  "message": "Password reset successful. Please log in with your new password."
}
```

**Response (Error - 400):**
```json
{
  "error": "InvalidOrExpiredToken"
}
```

---
### 5.5 URL Submission

**Endpoint:** `POST /api/urls/submit`

**Authentication:** Required (JWT Bearer token)

**Request Headers:**
```
Authorization: Bearer {jwt_token}
```

**Request Body:**
```json
{
  "url": "https://example.com",
  "nickname": "MySecret",
  "passkey": "secret123"
}
```

**Validation Rules:**
- URL: valid HTTP/HTTPS format
- Nickname: 1-50 chars, alphanumeric + spaces
- Passkey: min 4 chars

**Process:**
1. Verify JWT token and extract user_id
2. Check rate limit: query `sessions` table for `last_submit_at`
3. If within 5-minute window, return 429
4. Validate URL format
5. BCrypt hash the passkey (cost factor: 12)
6. Resolve client IP address
7. Call ipinfo.io API: `GET https://ipinfo.io/{ip}?token={IPINFO_TOKEN}`
8. Run obfuscation pipeline on URL
9. Insert record into `url_entries` table
10. Insert IP metadata into `ip_metadata` table
11. Update `sessions.last_submit_at` to NOW()
12. Return obfuscated URL + metadata

**Obfuscation Pipeline:**
```
Original URL
  ↓ Layer 1: Caesar Cipher (shift by key)
  ↓ Layer 2: XOR Encoding (repeating key)
  ↓ Layer 3: CarlSuello Algorithm (custom)
  ↓ Layer 4: Base64 Encode
Obfuscated Output
```

**Response (Success - 201):**
```json
{
  "id": "entry-uuid",
  "obfuscatedUrl": "Q2FybFN1ZWxsb0V4YW1wbGU=",
  "nickname": "MySecret",
  "createdAt": "2026-05-06T03:44:10Z"
}
```

**Response (Error - 429):**
```json
{
  "error": "RateLimitExceeded",
  "message": "Please wait 5 minutes between submissions",
  "retryAfter": 180
}
```

### 5.6 Public Feed

**Endpoint:** `GET /api/urls/feed`

**Authentication:** Required (JWT Bearer token)

**Query Parameters:**
- `page` (optional, default: 1)
- `limit` (optional, default: 20, max: 100)

**Process:**
1. Verify JWT token
2. Query `url_entries` table ordered by `created_at DESC`
3. Return obfuscated URLs with nickname and timestamp only
4. Do NOT include: original URL, IP metadata, user_id, passkey info

**Response (Success - 200):**
```json
{
  "entries": [
    {
      "obfuscatedUrl": "Q2FybFN1ZWxsb0V4YW1wbGU=",
      "nickname": "MySecret",
      "createdAt": "2026-05-06T03:44:10Z"
    }
  ],
  "pagination": {
    "page": 1,
    "limit": 20,
    "total": 150,
    "totalPages": 8
  }
}
```

### 5.7 Deobfuscation

**Endpoint:** `POST /api/urls/deobfuscate`

**Authentication:** Required (JWT Bearer token)

**Request Headers:**
```
Authorization: Bearer {jwt_token}
```

**Request Body:**
```json
{
  "obfuscatedText": "Q2FybFN1ZWxsb0V4YW1wbGU=",
  "nickname": "MySecret",
  "passkey": "secret123"
}
```

**Process:**
1. Verify JWT token and extract user_id
2. Check rate limit: query `sessions` table for `last_deobfuscate_at`
3. If within 5-minute window, return 429
4. Query `url_entries` WHERE `obfuscated_url = obfuscatedText` AND `nickname = nickname`
5. If no match found, return 401 with generic error
6. If `is_locked = TRUE`, return 423
7. BCrypt verify passkey against `passkey_hash`
8. If wrong passkey:
   - Increment `failed_attempts`
   - If `failed_attempts >= 3`, set `is_locked = TRUE`
   - Return 401 with generic error
9. If correct passkey:
   - Reset `failed_attempts = 0`
   - Update `sessions.last_deobfuscate_at` to NOW()
   - Run reverse obfuscation pipeline
   - Return original URL

**Deobfuscation Pipeline:**
```
Obfuscated String
  ↓ Layer 4: Base64 Decode
  ↓ Layer 3: CarlSuello Reverse
  ↓ Layer 2: XOR Decode
  ↓ Layer 1: Caesar Decipher
Original URL
```

**Response (Success - 200):**
```json
{
  "originalUrl": "https://example.com",
  "nickname": "MySecret",
  "createdAt": "2026-05-06T03:44:10Z"
}
```

**Response (Error - 401):**
```json
{
  "error": "InvalidCredentials"
}
```

**Response (Error - 423):**
```json
{
  "error": "EntryLocked",
  "message": "This entry has been locked due to too many failed attempts"
}
```

**Response (Error - 429):**
```json
{
  "error": "RateLimitExceeded",
  "message": "Please wait 5 minutes between deobfuscation attempts",
  "retryAfter": 240
}
```

---
### 5.8 Admin Dashboard

**Endpoint:** `POST /api/admin/login`

**Request Body:**
```json
{
  "username": "admin",
  "password": "admin_password"
}
```

**Process:**
1. Verify admin credentials via Django authentication
2. Create Django session or issue JWT
3. Return session cookie or token

**Response (Success - 200):**
```json
{
  "message": "Login successful",
  "sessionId": "django-session-id"
}
```

### 5.9 Admin - View All Entries

**Endpoint:** `GET /api/admin/urls`

**Authentication:** Required (Django session or admin JWT)

**Query Parameters:**
- `page` (optional, default: 1)
- `limit` (optional, default: 50)
- `filter` (optional: "locked", "unlocked", "all")

**Response (Success - 200):**
```json
{
  "entries": [
    {
      "id": "entry-uuid",
      "userId": "user-uuid",
      "originalUrl": "https://example.com",
      "obfuscatedUrl": "Q2FybFN1ZWxsb0V4YW1wbGU=",
      "nickname": "MySecret",
      "failedAttempts": 0,
      "isLocked": false,
      "createdAt": "2026-05-06T03:44:10Z",
      "ipMetadata": {
        "ip": "192.168.1.1",
        "city": "San Francisco",
        "region": "California",
        "country": "US",
        "org": "Example ISP",
        "timezone": "America/Los_Angeles"
      }
    }
  ],
  "pagination": {
    "page": 1,
    "limit": 50,
    "total": 150
  }
}
```

### 5.10 Admin - Create Entry

**Endpoint:** `POST /api/admin/urls`

**Authentication:** Required (Django admin)

**Request Body:**
```json
{
  "userId": "user-uuid",
  "url": "https://example.com",
  "nickname": "AdminEntry",
  "passkey": "admin123"
}
```

**Process:**
1. Validate admin authentication
2. Validate URL and nickname
3. BCrypt hash passkey
4. Run obfuscation pipeline
5. Insert into `url_entries` (no rate limit)
6. Log action in `admin_log` table
7. Return created entry

### 5.11 Admin - Update Entry

**Endpoint:** `PUT /api/admin/urls/{id}`

**Authentication:** Required (Django admin)

**Request Body:**
```json
{
  "originalUrl": "https://newexample.com",
  "nickname": "UpdatedNickname"
}
```

**Process:**
1. Validate admin authentication
2. Find entry by ID
3. If URL changed, re-run obfuscation pipeline
4. Update record in `url_entries`
5. Log old and new values in `admin_log`
6. Return updated entry

### 5.12 Admin - Delete Entry

**Endpoint:** `DELETE /api/admin/urls/{id}`

**Authentication:** Required (Django admin)

**Process:**
1. Validate admin authentication
2. Delete entry from `url_entries` (cascades to `ip_metadata`)
3. Log deletion in `admin_log`
4. Return success

**Response (Success - 200):**
```json
{
  "message": "Entry deleted successfully"
}
```

### 5.13 Admin - Bulk Delete

**Endpoint:** `POST /api/admin/urls/bulk-delete`

**Authentication:** Required (Django admin)

**Request Body:**
```json
{
  "ids": ["uuid1", "uuid2", "uuid3"]
}
```

**Process:**
1. Validate admin authentication
2. Delete all entries matching IDs
3. Log each deletion in `admin_log`
4. Return count of deleted entries

**Response (Success - 200):**
```json
{
  "message": "3 entries deleted successfully",
  "deletedCount": 3
}
```

### 5.14 Admin - Unlock Entry

**Endpoint:** `PATCH /api/admin/urls/{id}/unlock`

**Authentication:** Required (Django admin)

**Process:**
1. Validate admin authentication
2. Find entry by ID
3. Set `failed_attempts = 0` and `is_locked = FALSE`
4. Log unlock action in `admin_log`
5. Return updated entry

**Response (Success - 200):**
```json
{
  "id": "entry-uuid",
  "isLocked": false,
  "failedAttempts": 0,
  "message": "Entry unlocked successfully"
}
```

### 5.15 Admin - View Audit Log

**Endpoint:** `GET /api/admin/logs`

**Authentication:** Required (Django admin)

**Query Parameters:**
- `page` (optional, default: 1)
- `limit` (optional, default: 50)
- `action` (optional filter: "CREATE", "UPDATE", "DELETE", "UNLOCK")

**Response (Success - 200):**
```json
{
  "logs": [
    {
      "id": "log-uuid",
      "action": "UNLOCK",
      "targetTable": "url_entries",
      "targetId": "entry-uuid",
      "oldValue": {"isLocked": true, "failedAttempts": 3},
      "newValue": {"isLocked": false, "failedAttempts": 0},
      "performedAt": "2026-05-06T03:44:10Z"
    }
  ],
  "pagination": {
    "page": 1,
    "limit": 50,
    "total": 200
  }
}
```

---

## 6. Rate Limiting

| Action | Limit | Window | Scope | Lockout Rule |
|--------|-------|--------|-------|--------------|
| URL Submission | 1 request | 5 minutes | Per user account (JWT) | None |
| Deobfuscation | 1 request | 5 minutes | Per user account (JWT) | 3 wrong passkeys → entry locked |
| Admin actions | Unlimited | N/A | N/A | N/A |

**Implementation:**
- Rate limits tracked in `sessions` table via `last_submit_at` and `last_deobfuscate_at`
- Each authenticated request checks timestamp difference
- If < 5 minutes, return HTTP 429 with `retryAfter` seconds
- Admin requests bypass all rate limit checks

---
## 7. Obfuscation Pipeline

The pipeline is deterministic and fully reversible, applied server-side in C#. It is NOT cryptographically secure and is designed for obfuscation, not encryption.

### 7.1 Layer 1 - Caesar Cipher

**Obfuscation:**
- Each ASCII character is shifted forward by a fixed integer key (e.g., shift = 3)
- Example: 'A' → 'D', 'B' → 'E'

**Deobfuscation:**
- Shift backward by the same key

### 7.2 Layer 2 - XOR Encoding

**Obfuscation:**
- Convert string to byte array
- XOR each byte with a repeating secret key
- Example: `byte[i] ^ key[i % key.Length]`

**Deobfuscation:**
- XOR is self-inverse: apply the same operation

### 7.3 Layer 3 - CarlSuello Algorithm

**Obfuscation:**
- Custom proprietary algorithm (implementation-specific)
- Reversible character transformation

**Deobfuscation:**
- Apply reverse transformation

### 7.4 Layer 4 - Base64 Encoding

**Obfuscation:**
- Encode byte array to Base64 string

**Deobfuscation:**
- Decode Base64 string to byte array

### 7.5 Complete Pipeline Flow

```
OBFUSCATE                              DEOBFUSCATE
─────────────────────────────────────────────────────────────
Original URL                           Obfuscated String
    ↓                                      ↑
Layer 1: Caesar Cipher                 Layer 1: Caesar Decipher
    ↓                                      ↑
Layer 2: XOR Encoding                  Layer 2: XOR Decode
    ↓                                      ↑
Layer 3: CarlSuello Algorithm          Layer 3: CarlSuello Reverse
    ↓                                      ↑
Layer 4: Base64 Encode                 Layer 4: Base64 Decode
    ↓                                      ↑
Obfuscated Output                      Original URL
```

---

## 8. External APIs

### 8.1 ipinfo.io

**Purpose:** Fetch IP geolocation metadata on URL submission

**Endpoint:** `GET https://ipinfo.io/{ip}?token={IPINFO_TOKEN}`

**When Called:** Every URL submission (ASP.NET Core backend)

**Response Example:**
```json
{
  "ip": "8.8.8.8",
  "city": "Mountain View",
  "region": "California",
  "country": "US",
  "org": "AS15169 Google LLC",
  "timezone": "America/Los_Angeles"
}
```

**Fields Stored:**
- IP address
- City
- Region
- Country
- Organization (ISP)
- Timezone

**Visibility:**
- Admin dashboard only
- NOT included in public feed or deobfuscation responses

### 8.2 Render Email Service

**Purpose:** Send password reset emails

**Service:** Render (https://render.com)

**Email Endpoint:** Configured via Render dashboard

**When Called:** Forgot password flow (`POST /api/auth/forgot-password`)

**Email Template:**
```
From: noreply@ghostlynk.com
To: {user.email}
Subject: GhostLynk - Password Reset Request

Hello,

You requested a password reset for your GhostLynk account.

Click the link below to reset your password:
https://ghostlynk.com/reset-password?token={plaintext_token}

This link expires in 15 minutes.

If you didn't request this, please ignore this email.

- GhostLynk Team
```

**Configuration:**
- Render API key stored in environment variable: `RENDER_API_KEY`
- Sender email: `noreply@ghostlynk.com`
- Email tracking ID stored in `password_reset_tokens.resend_email_id`

---

## 9. System Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    Browser (React + Vite)                   │
│                                                             │
│  - User Registration/Login                                  │
│  - URL Submission Form                                      │
│  - Public Feed (Obfuscated URLs)                            │
│  - Deobfuscation Panel                                      │
│  - Forgot/Reset Password                                    │
└──────────────────────┬──────────────────────────────────────┘
                       │ HTTP/REST (JSON)
                       ↓
┌─────────────────────────────────────────────────────────────┐
│                         Nginx Reverse Proxy                 │
│                                                             │
│  /                    → React static build                  │
│  /api/auth/*          → ASP.NET Core :5000                  │
│  /api/urls/*          → ASP.NET Core :5000                  │
│  /api/admin/*         → Django :8000                        │
└──────────────┬──────────────────────────┬───────────────────┘
               │                          │
               ↓                          ↓
┌──────────────────────────┐   ┌──────────────────────────┐
│   ASP.NET Core 8 (C#)    │   │   Python Django          │
│   Public API Backend     │   │   Admin API Backend      │
│                          │   │                          │
│  - User Auth (JWT)       │   │  - Admin Auth            │
│  - URL Submission        │   │  - Dashboard CRUD        │
│  - Deobfuscation         │   │  - Entry Management      │
│  - Rate Limiting         │   │  - Unlock Entries        │
│  - Obfuscation Pipeline  │   │  - Audit Logging         │
│  - Password Reset        │   │                          │
└──────────┬───────────────┘   └───────────┬──────────────┘
           │                               │
           └───────────┬───────────────────┘
                       ↓
           ┌───────────────────────┐
           │   PostgreSQL Database │
           │                       │
           │  - users              │
           │  - url_entries        │
           │  - ip_metadata        │
           │  - sessions           │
           │  - password_reset_    │
           │    tokens             │
           │  - admin_log          │
           └───────────────────────┘

External Services:
┌──────────────┐         ┌──────────────┐
│  ipinfo.io   │         │    Render    │
│  (IP Geo)    │         │   (Email)    │
└──────────────┘         └──────────────┘
```

---
## 10. API Request Flows

### 10.1 User Registration Flow

```
1. User fills registration form
   ↓
2. Frontend validates input
   ↓
3. POST /api/auth/register
   ↓
4. ASP.NET validates uniqueness
   ↓
5. BCrypt hash password
   ↓
6. Insert into users table
   ↓
7. Return success + user ID
   ↓
8. Frontend redirects to login
```

### 10.2 User Login Flow

```
1. User enters email/username + password
   ↓
2. POST /api/auth/login
   ↓
3. ASP.NET finds user by email OR username
   ↓
4. BCrypt verify password
   ↓
5. Generate JWT token (24h expiry)
   ↓
6. Create session record
   ↓
7. Return JWT token
   ↓
8. Frontend stores token in localStorage
   ↓
9. Include in Authorization header for all requests
```

### 10.3 Forgot Password Flow

```
1. User clicks "Forgot Password"
   ↓
2. Enters email address
   ↓
3. POST /api/auth/forgot-password
   ↓
4. ASP.NET finds user by email
   ↓
5. Generate random 32-byte token
   ↓
6. SHA-256 hash token for storage
   ↓
7. Insert into password_reset_tokens (15min expiry)
   ↓
8. Trigger invalidates old tokens
   ↓
9. Send email via Render with plaintext token
   ↓
10. Store Render email ID
   ↓
11. Return success (always)
   ↓
12. User receives email
   ↓
13. Clicks reset link with token
   ↓
14. Frontend shows reset password form
   ↓
15. POST /api/auth/reset-password
   ↓
16. ASP.NET hashes token, finds record
   ↓
17. Validates expiry and usage
   ↓
18. BCrypt hash new password
   ↓
19. Update users.password_hash
   ↓
20. Mark token as used
   ↓
21. Delete all user sessions
   ↓
22. Return success
   ↓
23. Frontend redirects to login
```

### 10.4 URL Submission Flow

```
1. User enters URL + nickname + passkey
   ↓
2. Frontend validates URL format
   ↓
3. POST /api/urls/submit (with JWT)
   ↓
4. ASP.NET verifies JWT
   ↓
5. Check rate limit (last_submit_at)
   ↓
6. If < 5min, return 429
   ↓
7. BCrypt hash passkey
   ↓
8. Resolve client IP
   ↓
9. Call ipinfo.io API
   ↓
10. Run obfuscation pipeline
   ↓
11. Insert into url_entries
   ↓
12. Insert into ip_metadata
   ↓
13. Update sessions.last_submit_at
   ↓
14. Return obfuscated URL + metadata
   ↓
15. Frontend appends to feed
```

### 10.5 Deobfuscation Flow

```
1. User enters obfuscated text + nickname + passkey
   ↓
2. POST /api/urls/deobfuscate (with JWT)
   ↓
3. ASP.NET verifies JWT
   ↓
4. Check rate limit (last_deobfuscate_at)
   ↓
5. If < 5min, return 429
   ↓
6. Query url_entries by obfuscated_url + nickname
   ↓
7. If no match, return 401 (generic error)
   ↓
8. If is_locked = TRUE, return 423
   ↓
9. BCrypt verify passkey
   ↓
10. If wrong:
    - Increment failed_attempts
    - If >= 3, set is_locked = TRUE
    - Return 401 (generic error)
   ↓
11. If correct:
    - Reset failed_attempts = 0
    - Update sessions.last_deobfuscate_at
    - Run reverse pipeline
    - Return original URL
   ↓
12. Frontend displays original URL
```

### 10.6 Admin Dashboard Flow

```
1. Admin logs in via Django
   ↓
2. POST /api/admin/login
   ↓
3. Django verifies credentials
   ↓
4. Create session cookie
   ↓
5. GET /api/admin/urls
   ↓
6. Django queries url_entries + ip_metadata
   ↓
7. Return all entries with plaintext URLs
   ↓
8. Admin views/edits/deletes entries
   ↓
9. All actions logged in admin_log
```

---

## 11. Security Considerations

### 11.1 Password Security

- **Hashing Algorithm:** BCrypt with cost factor 12
- **Password Requirements:**
  - Minimum 8 characters
  - At least 1 uppercase letter
  - At least 1 lowercase letter
  - At least 1 digit
  - At least 1 special character
- **Storage:** Only hashed passwords stored, never plaintext

### 11.2 JWT Security

- **Algorithm:** HS256 (HMAC with SHA-256)
- **Secret Key:** Stored in environment variable, min 256 bits
- **Expiry:** 24 hours
- **Claims:** sub (user ID), email, username, iat, exp
- **Validation:** Signature and expiry checked on every request

### 11.3 Password Reset Security

- **Token Generation:** Cryptographically secure random (32 bytes)
- **Token Storage:** SHA-256 hashed, never plaintext
- **Token Expiry:** 15 minutes
- **Single Use:** Marked as used after successful reset
- **Session Invalidation:** All user sessions deleted on password reset
- **Email Enumeration Prevention:** Always return success, even if email doesn't exist

### 11.4 Rate Limiting

- **Purpose:** Prevent brute force and abuse
- **Scope:** Per user account (JWT-based)
- **Enforcement:** Database-backed (sessions table)
- **Bypass:** Admin accounts only

### 11.5 Entry Locking

- **Trigger:** 3 consecutive wrong passkey attempts
- **Effect:** Entry becomes inaccessible to all users
- **Unlock:** Admin-only via PATCH /api/admin/urls/{id}/unlock
- **Counter Reset:** On successful deobfuscation

### 11.6 SQL Injection Prevention

- **ORM Usage:** Entity Framework Core (ASP.NET), Django ORM
- **Parameterized Queries:** All database queries use parameters
- **Input Validation:** All user inputs validated before processing

### 11.7 XSS Prevention

- **Frontend:** React auto-escapes by default
- **API Responses:** JSON only, no HTML rendering
- **Content-Type:** application/json enforced

### 11.8 CORS Configuration

- **Allowed Origins:** Frontend domain only (e.g., https://ghostlynk.com)
- **Allowed Methods:** GET, POST, PUT, PATCH, DELETE
- **Allowed Headers:** Authorization, Content-Type
- **Credentials:** Allowed for admin session cookies

### 11.9 HTTPS Enforcement

- **Production:** All traffic over HTTPS
- **HSTS:** Strict-Transport-Security header enabled
- **Certificate:** Valid SSL/TLS certificate required

### 11.10 Environment Variables

Required environment variables:

**ASP.NET Core (.env):**
```
DATABASE_URL=postgresql://user:pass@host:5432/ghostlynk
JWT_SECRET=your-256-bit-secret-key
IPINFO_TOKEN=your-ipinfo-token
RENDER_API_KEY=your-render-api-key
FRONTEND_URL=https://ghostlynk.com
CAESAR_SHIFT=3
XOR_KEY=your-xor-secret-key
```

**Django (.env):**
```
DATABASE_URL=postgresql://user:pass@host:5432/ghostlynk
SECRET_KEY=your-django-secret-key
DEBUG=False
ALLOWED_HOSTS=admin.ghostlynk.com
```

---
## 12. Project Structure

```
ghostlynk/
├── backend_deob/                       # ASP.NET Core 8 Web API
│   ├── Controllers/
│   │   ├── AuthController.cs           # Registration, login, forgot/reset password
│   │   └── UrlsController.cs           # Submit, deobfuscate, feed
│   ├── Services/
│   │   ├── ObfuscationService.cs       # 4-layer pipeline
│   │   ├── IpInfoService.cs            # ipinfo.io integration
│   │   ├── EmailService.cs             # Render email integration
│   │   ├── JwtService.cs               # JWT generation/validation
│   │   └── RateLimitService.cs         # Rate limit checks
│   ├── Models/
│   │   ├── User.cs
│   │   ├── UrlEntry.cs
│   │   ├── IpMetadata.cs
│   │   ├── Session.cs
│   │   └── PasswordResetToken.cs
│   ├── Data/
│   │   └── AppDbContext.cs             # EF Core DbContext
│   ├── Migrations/                     # EF Core migrations
│   ├── Middleware/
│   │   └── JwtAuthMiddleware.cs        # JWT validation
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   ├── Program.cs
│   └── backend_deob.csproj
│
├── backend_admin/                      # Python Django API
│   ├── manage.py
│   ├── core/                           # Django project settings
│   │   ├── settings.py
│   │   ├── urls.py
│   │   └── wsgi.py
│   ├── api/                            # Django app
│   │   ├── models.py                   # Django models (read-only for shared tables)
│   │   ├── views.py                    # Admin API views
│   │   ├── serializers.py              # DRF serializers
│   │   ├── urls.py                     # API routes
│   │   └── admin.py                    # Django admin config
│   ├── requirements.txt
│   └── .env
│
├── frontend/                           # React + Vite
│   ├── src/
│   │   ├── components/
│   │   │   ├── Auth/
│   │   │   │   ├── Register.jsx
│   │   │   │   ├── Login.jsx
│   │   │   │   ├── ForgotPassword.jsx
│   │   │   │   └── ResetPassword.jsx
│   │   │   ├── Urls/
│   │   │   │   ├── SubmitForm.jsx
│   │   │   │   ├── Feed.jsx
│   │   │   │   └── DeobfuscatePanel.jsx
│   │   │   └── Layout/
│   │   │       ├── Navbar.jsx
│   │   │       └── Footer.jsx
│   │   ├── services/
│   │   │   ├── authService.js          # API calls for auth
│   │   │   └── urlService.js           # API calls for URLs
│   │   ├── utils/
│   │   │   ├── jwtHelper.js            # JWT storage/retrieval
│   │   │   └── validators.js           # Form validation
│   │   ├── App.jsx
│   │   ├── main.jsx
│   │   └── index.css
│   ├── public/
│   ├── package.json
│   ├── vite.config.js
│   └── .env
│
├── nginx/
│   └── nginx.conf                      # Reverse proxy config
│
├── docker-compose.yml                  # Multi-container orchestration
├── .env.example                        # Environment variable template
├── .gitignore
└── README.md                           # Setup and usage guide
```

---

## 13. Database Triggers and Functions

### 13.1 Auto-Update updated_at

**Trigger:** `set_updated_at_users`, `set_updated_at_url_entries`

**Purpose:** Automatically update `updated_at` timestamp on row modification

**Applied to:** `users`, `url_entries`

### 13.2 Invalidate Previous Reset Tokens

**Trigger:** `invalidate_old_reset_tokens`

**Purpose:** Mark all previous unused reset tokens as used when a new token is created

**Applied to:** `password_reset_tokens` (AFTER INSERT)

### 13.3 Auto-Lock Entry on Max Attempts

**Trigger:** `lock_entry_on_max_attempts`

**Purpose:** Automatically set `is_locked = TRUE` when `failed_attempts >= 3`

**Applied to:** `url_entries` (BEFORE UPDATE)

### 13.4 Cleanup Functions

**Function:** `cleanup_expired_sessions()`

**Purpose:** Delete expired sessions (run periodically via cron job)

**Returns:** Count of deleted sessions

**Function:** `cleanup_expired_reset_tokens()`

**Purpose:** Delete expired and used reset tokens (run periodically)

**Returns:** Count of deleted tokens

---

## 14. Deployment

### 14.1 Prerequisites

- PostgreSQL 14+ server
- .NET 8 SDK
- Python 3.11+
- Node.js 18+
- Nginx
- SSL certificate (Let's Encrypt recommended)

### 14.2 Database Setup

```bash
# Create database
createdb ghostlynk

# Run schema
psql -d ghostlynk -f schema.sql

# Verify tables
psql -d ghostlynk -c "\dt"
```

### 14.3 Backend (ASP.NET Core) Setup

```bash
cd backend_deob

# Install dependencies
dotnet restore

# Update connection string in appsettings.json
# Set environment variables

# Run migrations (if using EF Core migrations)
dotnet ef database update

# Build
dotnet build

# Run
dotnet run --urls "http://localhost:5000"
```

### 14.4 Backend (Django) Setup

```bash
cd backend_admin

# Create virtual environment
python -m venv venv
source venv/bin/activate  # On Windows: venv\Scripts\activate

# Install dependencies
pip install -r requirements.txt

# Update DATABASE_URL in .env

# Run migrations
python manage.py migrate

# Create superuser
python manage.py createsuperuser

# Run server
python manage.py runserver 8000
```

### 14.5 Frontend Setup

```bash
cd frontend

# Install dependencies
npm install

# Update API URLs in .env
# VITE_API_URL=https://api.ghostlynk.com

# Build for production
npm run build

# Output in dist/ folder
```

### 14.6 Nginx Configuration

```nginx
server {
    listen 80;
    server_name ghostlynk.com;
    return 301 https://$server_name$request_uri;
}

server {
    listen 443 ssl http2;
    server_name ghostlynk.com;

    ssl_certificate /etc/letsencrypt/live/ghostlynk.com/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/ghostlynk.com/privkey.pem;

    # Frontend
    location / {
        root /var/www/ghostlynk/frontend/dist;
        try_files $uri $uri/ /index.html;
    }

    # Public API (ASP.NET Core)
    location /api/auth/ {
        proxy_pass http://localhost:5000;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
    }

    location /api/urls/ {
        proxy_pass http://localhost:5000;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
    }

    # Admin API (Django)
    location /api/admin/ {
        proxy_pass http://localhost:8000;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
    }
}
```

### 14.7 Docker Deployment (Optional)

```bash
# Build and start all services
docker-compose up -d

# View logs
docker-compose logs -f

# Stop services
docker-compose down
```

### 14.8 Periodic Cleanup Jobs

Add to crontab:

```bash
# Cleanup expired sessions every hour
0 * * * * psql -d ghostlynk -c "SELECT cleanup_expired_sessions();"

# Cleanup expired reset tokens every 30 minutes
*/30 * * * * psql -d ghostlynk -c "SELECT cleanup_expired_reset_tokens();"
```

---

## 15. Testing

### 15.1 Unit Tests

**ASP.NET Core:**
```bash
cd backend_deob.Tests
dotnet test
```

**Test Coverage:**
- ObfuscationService (all 4 layers + reverse)
- JwtService (generation, validation, expiry)
- RateLimitService (submission, deobfuscation)
- EmailService (Render integration)

**Django:**
```bash
cd backend_admin
python manage.py test
```

**Test Coverage:**
- Admin CRUD operations
- Audit logging
- Entry unlock functionality

### 15.2 Integration Tests

**Test Scenarios:**
1. User registration → login → JWT issuance
2. URL submission → obfuscation → database storage
3. Deobfuscation with correct passkey
4. Deobfuscation with wrong passkey → failed_attempts increment
5. Entry locking after 3 failed attempts
6. Rate limit enforcement (submission and deobfuscation)
7. Forgot password → email sent → token validation → password reset
8. Admin login → view entries → unlock locked entry

### 15.3 API Testing (Postman/Insomnia)

**Collection includes:**
- Auth endpoints (register, login, forgot, reset)
- URL endpoints (submit, feed, deobfuscate)
- Admin endpoints (login, CRUD, unlock, logs)

### 15.4 Load Testing

**Tools:** Apache JMeter, k6

**Scenarios:**
- 100 concurrent users submitting URLs
- 100 concurrent deobfuscation attempts
- Rate limit behavior under load

---

## 16. Monitoring and Logging

### 16.1 Application Logs

**ASP.NET Core:**
- Serilog for structured logging
- Log levels: Debug, Info, Warning, Error, Fatal
- Output: Console + File + Database (optional)

**Django:**
- Django logging framework
- Admin actions logged to `admin_log` table

### 16.2 Metrics to Monitor

- Request rate (per endpoint)
- Response times (p50, p95, p99)
- Error rates (4xx, 5xx)
- Database connection pool usage
- JWT validation failures
- Rate limit rejections (429 responses)
- Failed deobfuscation attempts
- Locked entries count

### 16.3 Alerts

- High error rate (> 5% of requests)
- Database connection failures
- ipinfo.io API failures
- Render email delivery failures
- Disk space < 10%
- CPU usage > 80%

---

## 17. API Error Codes

| HTTP Code | Error Code | Description |
|-----------|------------|-------------|
| 200 | - | Success |
| 201 | - | Resource created |
| 400 | ValidationFailed | Invalid input data |
| 400 | InvalidOrExpiredToken | Password reset token invalid/expired |
| 401 | InvalidCredentials | Wrong email/username/password or passkey |
| 401 | Unauthorized | Missing or invalid JWT |
| 403 | Forbidden | Insufficient permissions |
| 423 | EntryLocked | Entry locked due to failed attempts |
| 429 | RateLimitExceeded | Too many requests |
| 500 | InternalServerError | Server error |
| 503 | ServiceUnavailable | External service (ipinfo.io, Render) down |

---

## 18. Future Enhancements

- Email verification on registration
- Two-factor authentication (2FA)
- User profile management
- Entry expiration (auto-delete after X days)
- Custom obfuscation keys per user
- API rate limiting per IP (in addition to per-user)
- WebSocket support for real-time feed updates
- Mobile app (React Native)
- Export entries to CSV (admin)
- Advanced search and filtering (admin)

---

## 19. License and Credits

**License:** MIT

**Credits:**
- ASP.NET Core Team
- Django Software Foundation
- React Team
- ipinfo.io
- Render

---

**End of Specifications**
