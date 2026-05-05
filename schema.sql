-- =============================================================================
-- GhostLynk PostgreSQL Schema
-- Version: 2.0.0
-- Description: Shared database schema accessible by both ASP.NET Core (.NET)
--              and Python Django backends.
-- =============================================================================

-- Enable UUID generation
CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- =============================================================================
-- TABLE: users
-- Managed by: ASP.NET Core (Full CRUD) | Django (Read, Delete)
-- =============================================================================
CREATE TABLE users (
    id            UUID          PRIMARY KEY DEFAULT gen_random_uuid(),
    email         VARCHAR(255)  NOT NULL UNIQUE,
    username      VARCHAR(100)  NOT NULL UNIQUE,
    first_name    VARCHAR(100)  NOT NULL,
    last_name     VARCHAR(100)  NOT NULL,
    address       TEXT          NOT NULL,
    password_hash TEXT          NOT NULL,
    created_at    TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    updated_at    TIMESTAMPTZ   NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_users_email    ON users (email);
CREATE INDEX idx_users_username ON users (username);

-- =============================================================================
-- TABLE: url_entries
-- Managed by: ASP.NET Core (INSERT, READ) | Django (Full CRUD + UNLOCK)
-- =============================================================================
CREATE TABLE url_entries (
    id              UUID          PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id         UUID          NOT NULL REFERENCES users (id) ON DELETE CASCADE,
    original_url    TEXT          NOT NULL,
    obfuscated_url  TEXT          NOT NULL UNIQUE,
    nickname        VARCHAR(50)   NOT NULL,
    passkey_hash    TEXT          NOT NULL,
    failed_attempts INTEGER       NOT NULL DEFAULT 0,
    is_locked       BOOLEAN       NOT NULL DEFAULT FALSE,
    created_at      TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ   NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_url_entries_user_id       ON url_entries (user_id);
CREATE INDEX idx_url_entries_obfuscated    ON url_entries (obfuscated_url);
CREATE INDEX idx_url_entries_is_locked     ON url_entries (is_locked);

-- =============================================================================
-- TABLE: ip_metadata
-- One-to-one with url_entries.
-- Managed by: ASP.NET Core (INSERT, READ) | Django (Read only)
-- =============================================================================
CREATE TABLE ip_metadata (
    id            UUID          PRIMARY KEY DEFAULT gen_random_uuid(),
    url_entry_id  UUID          NOT NULL UNIQUE REFERENCES url_entries (id) ON DELETE CASCADE,
    ip_address    VARCHAR(45),
    city          VARCHAR(100),
    region        VARCHAR(100),
    country       VARCHAR(10),
    org           VARCHAR(255),
    timezone      VARCHAR(100),
    fetched_at    TIMESTAMPTZ   NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_ip_metadata_url_entry_id ON ip_metadata (url_entry_id);

-- =============================================================================
-- TABLE: sessions
-- Tracks per-user rate limit timestamps for submission and deobfuscation.
-- Managed by: ASP.NET Core (Full CRUD) | Django (None)
-- =============================================================================
CREATE TABLE sessions (
    id                    UUID          PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id               UUID          NOT NULL REFERENCES users (id) ON DELETE CASCADE,
    session_token         VARCHAR(255)  NOT NULL UNIQUE,
    last_submit_at        TIMESTAMPTZ,
    last_deobfuscate_at   TIMESTAMPTZ,
    created_at            TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    expires_at            TIMESTAMPTZ   NOT NULL
);

CREATE INDEX idx_sessions_user_id       ON sessions (user_id);
CREATE INDEX idx_sessions_token         ON sessions (session_token);
CREATE INDEX idx_sessions_expires_at    ON sessions (expires_at);

-- =============================================================================
-- TABLE: password_reset_tokens
-- Stores SHA-256 hashed reset tokens for forgot-password flow via Resend.
-- Managed by: ASP.NET Core (Full CRUD) | Django (None)
-- =============================================================================
CREATE TABLE password_reset_tokens (
    id               UUID          PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id          UUID          NOT NULL REFERENCES users (id) ON DELETE CASCADE,
    token_hash       VARCHAR(255)  NOT NULL UNIQUE,
    resend_email_id  VARCHAR(255),
    expires_at       TIMESTAMPTZ   NOT NULL,
    used_at          TIMESTAMPTZ,
    created_at       TIMESTAMPTZ   NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_prt_user_id     ON password_reset_tokens (user_id);
CREATE INDEX idx_prt_token_hash  ON password_reset_tokens (token_hash);
CREATE INDEX idx_prt_expires_at  ON password_reset_tokens (expires_at);

-- =============================================================================
-- TABLE: admin_log
-- Audit trail for all admin actions performed via Django dashboard.
-- Managed by: ASP.NET Core (None) | Django (INSERT, READ)
-- =============================================================================
CREATE TABLE admin_log (
    id            UUID          PRIMARY KEY DEFAULT gen_random_uuid(),
    action        VARCHAR(50)   NOT NULL,
    target_table  VARCHAR(100)  NOT NULL,
    target_id     UUID          NOT NULL,
    old_value     JSONB,
    new_value     JSONB,
    performed_at  TIMESTAMPTZ   NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_admin_log_target_id     ON admin_log (target_id);
CREATE INDEX idx_admin_log_performed_at  ON admin_log (performed_at);
CREATE INDEX idx_admin_log_action        ON admin_log (action);

-- =============================================================================
-- FUNCTION: auto-update updated_at on row change
-- Applied to: users, url_entries
-- =============================================================================
CREATE OR REPLACE FUNCTION trigger_set_updated_at()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER set_updated_at_users
    BEFORE UPDATE ON users
    FOR EACH ROW EXECUTE FUNCTION trigger_set_updated_at();

CREATE TRIGGER set_updated_at_url_entries
    BEFORE UPDATE ON url_entries
    FOR EACH ROW EXECUTE FUNCTION trigger_set_updated_at();

-- =============================================================================
-- FUNCTION: auto-invalidate previous unused reset tokens when a new one is
--           created for the same user (prevents token accumulation).
-- =============================================================================
CREATE OR REPLACE FUNCTION invalidate_previous_reset_tokens()
RETURNS TRIGGER AS $$
BEGIN
    UPDATE password_reset_tokens
    SET    used_at = NOW()
    WHERE  user_id  = NEW.user_id
      AND  used_at  IS NULL
      AND  id      != NEW.id;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER invalidate_old_reset_tokens
    AFTER INSERT ON password_reset_tokens
    FOR EACH ROW EXECUTE FUNCTION invalidate_previous_reset_tokens();

-- =============================================================================
-- FUNCTION: auto-lock url_entry when failed_attempts reaches 3
-- =============================================================================
CREATE OR REPLACE FUNCTION trigger_lock_entry_on_max_attempts()
RETURNS TRIGGER AS $$
BEGIN
    IF NEW.failed_attempts >= 3 THEN
        NEW.is_locked = TRUE;
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER lock_entry_on_max_attempts
    BEFORE UPDATE ON url_entries
    FOR EACH ROW
    WHEN (NEW.failed_attempts IS DISTINCT FROM OLD.failed_attempts)
    EXECUTE FUNCTION trigger_lock_entry_on_max_attempts();

-- =============================================================================
-- FUNCTION: cleanup expired sessions (run periodically via pg_cron or app job)
-- =============================================================================
CREATE OR REPLACE FUNCTION cleanup_expired_sessions()
RETURNS INTEGER AS $$
DECLARE
    deleted_count INTEGER;
BEGIN
    DELETE FROM sessions WHERE expires_at < NOW();
    GET DIAGNOSTICS deleted_count = ROW_COUNT;
    RETURN deleted_count;
END;
$$ LANGUAGE plpgsql;

-- =============================================================================
-- FUNCTION: cleanup expired and used reset tokens (run periodically)
-- =============================================================================
CREATE OR REPLACE FUNCTION cleanup_expired_reset_tokens()
RETURNS INTEGER AS $$
DECLARE
    deleted_count INTEGER;
BEGIN
    DELETE FROM password_reset_tokens
    WHERE expires_at < NOW()
       OR used_at IS NOT NULL;
    GET DIAGNOSTICS deleted_count = ROW_COUNT;
    RETURN deleted_count;
END;
$$ LANGUAGE plpgsql;

-- =============================================================================
-- End of GhostLynk schema
-- =============================================================================
