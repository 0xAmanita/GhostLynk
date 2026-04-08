# GhostLynk

Vanish your URLs through layered ciphers. Anonymous submissions, IP intelligence, admin control.

Version 1.0.0

---
## 1. Project Overview
GhostLynk is a full-stack web application where anonymous users can submit a website URL with a nickname and a secret passkey. The backend stores the URL, enriches it with IP geolocation via ipinfo.io, and applies a custom multi-layer obfuscation pipeline (Caesar Cipher → XOR → Custom Algorithm → Base64). The public feed shows only obfuscated output. A user who knows the correct nickname and passkey for an entry can deobfuscate it to reveal the original URL.

A single privileged Admin account can view all entries in plaintext via a protected dashboard and has full CRUD control. Per-session rate limiting is enforced on both submission and deobfuscation without requiring user accounts.

**Architecture:** The system operates on a dual-backend microservice-style architecture. The high-traffic public API is powered by **ASP.NET Core 8 (C#)**, while the secure Admin dashboard and administrative REST APIs are managed by **Python Django**. Both backend services share the underlying SQLite database.

## 2. Setup
### 2.1 Initialize the Public Backend (.NET) & Database
```bash
cd backend_deob

# 2. Restore .NET dependencies
dotnet restore

# 3. Apply database migrations (This creates the SQLite file and URL tables)
dotnet ef database update

# 4. Start the server
dotnet run
```
### 2.2 Initialize the Admin Backend (Python Django)
```bash
cd backend_admin

# Create and activate a virtual environment
python -m venv venv

# Install Python dependencies
pip install -r requirements.txt

# Apply Django migrations (Adds admin/auth tables to the SQLite DB)
python manage.py migrate

# Create your initial admin account
python manage.py createsuperuser

# Start the server
python manage.py runserver 8000
```

### 2.3 Initialize React Frontend
```bash
cd frontend

# Install Node dependencies
npm install

# Start the Vite development server
npm run dev
```

## 3. User Roles

### PUBLIC USER (Anonymous, no login required)
* Submits a URL with a nickname and a passkey per entry.
* Rate limited: 1 submission per 5 minutes per session.
* Views all entries as obfuscated text with nickname and timestamp.
* Can deobfuscate an entry using the correct obfuscated text, nickname, and passkey.
* Deobfuscation is rate limited: 1 attempt per 5 minutes; max 3 wrong passkey attempts before entry locks.
* Cannot edit or delete any entry.

### ADMIN (Single account, login required)
* Logs in via Django `/api/admin/login` or the built-in Django Admin portal.
* Dashboard shows all entries: plaintext URL, obfuscated text, nickname, timestamp, ipinfo.io metadata, and lock status.
* Full CRUD: create, read, update, and delete any entry.
* Can unlock any passkey-locked entry.
* Bypasses all rate limiting.

## 4. Feature Specification

### 4.1 URL Submission (Public)
* User fills in three required fields: URL, nickname, and passkey.
* Frontend validates URL format before submission.
* Passkey is bcrypt-hashed server-side before storage — plaintext passkey is never persisted.
* Rate limit: 1 submission per 5-minute window per session; form is disabled with a live countdown when limited.
* On success, obfuscated URL + nickname + timestamp are returned and appended to the public feed.

### 4.2 Public Feed (Public)
* Displays all submitted entries, newest first.
* Each card shows: obfuscated URL text, nickname, and timestamp.
* Original URL is never displayed in the public feed.

### 4.3 Deobfuscation (Public)
* A dedicated Deobfuscate panel sits below the public feed on the frontend.
* The panel contains exactly three input fields with no hints beyond their labels: Obfuscated Text, Nickname, and Passkey.
* Backend looks up the entry by ObfuscatedUrl + Nickname, then bcrypt-verifies the passkey.
* On success: returns plaintext URL, nickname, and timestamp.
* On any failure (no match OR wrong passkey): returns a single generic error — no field hint is given.
* Rate limit: 1 deobfuscation attempt per 5-minute window per session.
* Passkey lockout: after 3 consecutive wrong passkey attempts across any sessions, the entry is locked; only Admin can unlock it.

### 4.4 Admin Dashboard
* Login via Django authentication; session cookie or JWT issued.
* Full entry table: plaintext URL, obfuscated text, nickname, timestamp, IP, city, region, country, org, timezone, lock status.
* Create entry manually (no rate limit).
* Edit any entry's URL or nickname.
* Delete single entry or bulk-delete by ID list.
* Unlock any passkey-locked entry (resets FailedAttempts and IsLocked).
* Logout clears the auth cookie.

### 4.5 Rate Limiting Summary

| Action | Limit | Window | Scope | Lockout Rule |
| :--- | :--- | :--- | :--- | :--- |
| URL Submission | 1 request | 5 minutes | Per session | None |
| Deobfuscation | 1 request | 5 minutes | Per session | 3 wrong passkeys → entry locked |
| Admin actions | Unlimited | N/A | N/A | N/A |

## 5. Obfuscation Pipeline

The pipeline is deterministic and fully reversible, applied server-side in C#. It is NOT cryptographically secure. Deobfuscation runs the same pipeline in reverse.

```text
OBFUSCATE                              DEOBFUSCATE
Original URL                           Obfuscated String
↓  Layer 1: Caesar Cipher                 ↑  Layer 4: Base64 Decode
↓  Layer 2: XOR Encoding                  ↑  Layer 3: CarlSuello Reverse
↓  Layer 3: CarlSuello Algorithm          ↑  Layer 2: XOR Decode
↓  Layer 4: Base64 Encode                 ↑  Layer 1: Caesar Decipher
Obfuscated Output                      Original URL
```
### 5.1 Layer 1 - Caesar Cipher
Each ASCII character is shifted forward by a fixed integer key. Reversed for deobfuscation.
### 5.2 Layer 2 - XOR Encoding
Bytes XOR'd against a repeating secret key. XOR is self-inverse: applying it twice returns the original.
### 5.3 Layer 3 - CarlSuello Algorithm
### 5.4 Layer 4 - Base64

## 6. Deobfuscation - Detailed Flow
```text
Obfuscated Text
[   paste obfuscated string here  ]
            ↓
Nickname
[   enter nickname  ]
            ↓
Passkey
[   enter passkey   ]
            ↓
[   Deobfuscate    ]
```
No hints, tooltips, or additional labels shown.
### 6.1 Backend Flow
1. Receive **POST** `/api/urls/deobfuscate` with `{ obfuscatedText, nickname, passkey }`.
2. Check session deobfuscation rate limit — reject 429 if within 5-min window.
3. Query DB: find entry where `ObfuscatedUrl = obfuscatedText` AND `Nickname = nickname`.
4. If no match — return 401 `{ error: 'InvalidCredentials' }` (identical response to wrong passkey).
5. If `IsLocked = true` on matched entry — return 423 `{ error: 'EntryLocked' }`.
6. `BCrypt.Verify(passkey, entry.PasskeyHash)`.
7. If wrong passkey: increment FailedAttempts; if FailedAttempts >= 3 set IsLocked = true; return 401.
8. If correct passkey: reset FailedAttempts = 0; update session LastDeobfuscateAt.
9. Run reverse pipeline: Base64 decode → Custom reverse → XOR decode → Caesar decipher.
10. Return 200 `{ originalUrl, nickname, createdAt }`.

## 7. External API - ipinfo.io
On every URL submission the .NET backend resolves the hostname to an IP and calls ipinfo.io. The returned metadata is stored in the database and is visible only in the Django Admin dashboard. It is never included in public feed responses or deobfuscation responses.

- **Fields Retrieved**: IP, City, Region, Country, Org, Timezone.
- **API Call**: `GET https://ipinfo.io/{ip}?token={IPINFO_TOKEN}`

## 8. Tech Stack

Backend (Public): ASP.NET Core 8 Web API (C#)

Backend (Admin): Python Django & Django REST

Frontend: React + Vite

Database: SQLite

External API: ipinfo.io

Server: Nginx

## 9. System Architecture

```text
[Browser: React + Vite SPA]
        |  HTTP/REST (JSON)
        v
[Nginx] -- /                -> React static build
        -- /api/urls/*      -> ASP.NET Core :5000 (Public) 
        -- /api/admin/*     -> Django :8000 (Admin) 
        | 
        v 
[Shared SQLite DB] <-- Both backends read/write here 
        |
[ipinfo.io] (called by ASP.NET on submission only)
```

### 9.1 Flow - URL Submission (Handled by ASP.NET Core)
1. User submits URL + nickname + passkey.
2. Backend checks session submission rate limit.
3. Validates URL, bcrypt-hashes passkey.
4. Resolves IP, calls ipinfo.io.
5. Runs obfuscation.
6. Saves full record to shared SQLite DB.

### 9.2 Flow - Deobfuscation (Handled by ASP.NET Core)
1. Checks DB for ObfuscatedUrl + Nickname.
2. BCrypt.Verify passkey.
3. Runs reverse pipeline and returns original URL.

### 9.3 Flow - Admin (Handled by Python Django)
1. Admin POSTs credentials to `/api/admin/login` (handled by Django).
2. Dashboard fetches `GET /api/admin/urls`.
3. Admin can PUT, DELETE, bulk DELETE, or PATCH unlock directly via Django APIs connected to the shared database.

## 10. Project Structure
```text
ghostlynk/
├── backend_deob/                       # ASP.NET Core 8 Web API
│   ├── Controllers/
│   ├── Services/
│   ├── Models/
│   ├── Data/
│   └── Program.cs
├── backend_admin/                        # Python Django API
│   ├── manage.py
│   ├── core/                             # Django settings
│   ├── api/                              # Django REST endpoints
│   └── requirements.txt
├── frontend/                             # React + Vite
│   ├── src/
│   └── vite.config.js
├── nginx/
│   └── nginx.conf
├── docker-compose.yml
├── .env.example
└── README.md
```