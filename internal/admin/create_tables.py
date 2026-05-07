import django
import os

os.environ.setdefault("DJANGO_SETTINGS_MODULE", "core.settings")
django.setup()

from django.db import connection

tables_sql = [
    """
    CREATE TABLE IF NOT EXISTS "Users" (
        "id" TEXT PRIMARY KEY,
        "email" TEXT UNIQUE NOT NULL,
        "username" TEXT UNIQUE NOT NULL,
        "first_name" TEXT NOT NULL,
        "last_name" TEXT NOT NULL,
        "address" TEXT NOT NULL,
        "password_hash" TEXT NOT NULL,
        "created_at" DATETIME NOT NULL,
        "updated_at" DATETIME NOT NULL
    );
    """,
    """
    CREATE TABLE IF NOT EXISTS "URL_ENTRIES" (
        "id" TEXT PRIMARY KEY,
        "user_id" TEXT NOT NULL REFERENCES "Users"("id"),
        "original_url" TEXT NOT NULL,
        "obfuscated_url" TEXT UNIQUE NOT NULL,
        "nickname" TEXT NOT NULL,
        "passkey_hash" TEXT NOT NULL,
        "failed_atempts" INTEGER DEFAULT 0,
        "is_locked" BOOLEAN DEFAULT 0,
        "created_at" DATETIME NOT NULL,
        "updated_at" DATETIME NOT NULL
    );
    """,
    """
    CREATE TABLE IF NOT EXISTS "IP_METADATA" (
        "id" TEXT PRIMARY KEY,
        "url_entry_id" TEXT UNIQUE NOT NULL REFERENCES "URL_ENTRIES"("id"),
        "ip_address" TEXT,
        "city" TEXT,
        "region" TEXT,
        "country" TEXT,
        "org" TEXT,
        "timezone" TEXT,
        "fetched_at" DATETIME
    );
    """,
    """
    CREATE TABLE IF NOT EXISTS "SESSIONS" (
        "id" TEXT PRIMARY KEY,
        "user_id" TEXT NOT NULL REFERENCES "Users"("id"),
        "session_token" TEXT UNIQUE NOT NULL,
        "last_submit_at" DATETIME,
        "last_deobfuscated_at" DATETIME,
        "created_at" DATETIME NOT NULL,
        "expires_at" DATETIME NOT NULL
    );
    """,
    """
    CREATE TABLE IF NOT EXISTS "ADMIN_LOG" (
        "id" TEXT PRIMARY KEY,
        "user_id" TEXT NOT NULL,
        "session_token" TEXT NOT NULL,
        "last_submit_at" TEXT,
        "old_value" TEXT,
        "new_value" TEXT,
        "performed_at" DATETIME NOT NULL
    );
    """,
    """
    CREATE TABLE IF NOT EXISTS "PASSWORD_RESET_TOKENS" (
        "id" TEXT PRIMARY KEY,
        "user_id" TEXT NOT NULL REFERENCES "Users"("id"),
        "token_hash" TEXT UNIQUE NOT NULL,
        "resend_email_id" TEXT,
        "expires_at" DATETIME NOT NULL,
        "used_at" DATETIME,
        "created_at" DATETIME NOT NULL
    );
    """,
]

with connection.cursor() as cursor:
    for sql in tables_sql:
        cursor.execute(sql)
        print(f"Table created successfully")

print("\nAll tables created successfully")