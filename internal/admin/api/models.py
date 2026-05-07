from django.db import models
import uuid
# Create your models here.



class AppUser(models.Model):
    """ERD: Users table"""
    id            = models.UUIDField(primary_key=True, default=uuid.uuid4, db_column="Id")
    email         = models.CharField(max_length=255, unique=True, db_column="Email")
    username      = models.CharField(max_length=255, unique=True, db_column="Username")
    first_name    = models.CharField(max_length=255, db_column="FirstName")
    last_name     = models.CharField(max_length=255, db_column="LastName")
    address       = models.TextField(db_column="Address")
    password_hash = models.CharField(max_length=255, db_column="PasswordHash")
    created_at    = models.DateTimeField(db_column="CreatedAt")
    updated_at    = models.DateTimeField(db_column="UpdatedAt")

    class Meta:
        managed  = False
        db_table = "Users"
        verbose_name        = "App User"
        verbose_name_plural = "App Users"


class UrlEntry(models.Model):
    """ERD: URL_ENTRIES table"""
    id             = models.UUIDField(primary_key=True, default=uuid.uuid4, db_column="Id")
    user           = models.ForeignKey(
                         AppUser, on_delete=models.CASCADE,
                         db_column="UserId", related_name="url_entries")
    original_url   = models.TextField(db_column="OriginalUrl")
    obfuscated_url = models.TextField(unique=True, db_column="ObfuscatedUrl")
    nickname       = models.CharField(max_length=255, db_column="Nickname")
    passkey_hash   = models.CharField(max_length=255, db_column="PasskeyHash")
    failed_attempts = models.IntegerField(default=0, db_column="FailedAttempts")
    is_locked      = models.BooleanField(default=False, db_column="IsLocked")
    created_at     = models.DateTimeField(db_column="CreatedAt")
    updated_at     = models.DateTimeField(db_column="UpdatedAt")

    class Meta:
        managed  = False
        db_table = "URL_ENTRIES"
        verbose_name = "URL Entry"             
        verbose_name_plural = "URL Entries"


class Session(models.Model):
    """ERD: SESSIONS table"""
    id                   = models.UUIDField(primary_key=True, default=uuid.uuid4, db_column="Id")
    user                 = models.ForeignKey(
                               AppUser, on_delete=models.CASCADE,
                               db_column="UserId", related_name="sessions")
    session_token        = models.CharField(max_length=255, unique=True, db_column="SessionToken")
    last_submit_at       = models.DateTimeField(null=True, blank=True, db_column="LastSubmitAt")
    last_deobfuscate_at = models.DateTimeField(null=True, blank=True, db_column="LastDeobfuscateAt")
    created_at           = models.DateTimeField(db_column="CreatedAt")
    expires_at           = models.DateTimeField(db_column="ExpiresAt")

    class Meta:
        managed  = False
        db_table = "SESSIONS"
        verbose_name        = "Session"
        verbose_name_plural = "Sessions"


class AdminLog(models.Model):
    """ERD: ADMIN_LOG table — logs every admin mutation"""
    id            = models.UUIDField(primary_key=True, default=uuid.uuid4)
    user_id       = models.CharField(max_length=255)
    session_token = models.CharField(max_length=255)
    last_submit_at = models.UUIDField(null=True, blank=True)
    old_value     = models.JSONField(null=True, blank=True)
    new_value     = models.JSONField(null=True, blank=True)
    performed_at  = models.DateTimeField(auto_now_add=True)

    class Meta:
        managed  = True
        db_table = "ADMIN_LOG"
        verbose_name        = "Admin Log"
        verbose_name_plural = "Admin Logs"


class PasswordResetToken(models.Model):
    """ERD: PASSWORD_RESET_TOKENS table"""
    id              = models.UUIDField(primary_key=True, default=uuid.uuid4, db_column="Id")
    user            = models.ForeignKey(
                          AppUser, on_delete=models.CASCADE,
                          db_column="UserId", related_name="reset_tokens")
    token_hash      = models.CharField(max_length=255, unique=True, db_column="TokenHash")
    resend_email_id = models.CharField(max_length=255, null=True, blank=True, db_column="ResendEmailId")
    expires_at      = models.DateTimeField(db_column="ExpiresAt")
    used_at         = models.DateTimeField(null=True, blank=True, db_column="UsedAt")
    created_at      = models.DateTimeField(db_column="CreatedAt")

    class Meta:
        managed  = False
        db_table = "PASSWORD_RESET_TOKENS"
        verbose_name        = "Password Reset Token"
        verbose_name_plural = "Password Reset Tokens"


class IpMetadata(models.Model):
    """ERD: IP_METADATA table"""
    id          = models.UUIDField(primary_key=True, default=uuid.uuid4, db_column="Id")
    url_entry   = models.OneToOneField(
                      UrlEntry, on_delete=models.CASCADE,
                      db_column="UrlEntryId", related_name="ip_metadata")
    ip_address  = models.CharField(max_length=100, db_column="IpAddress")
    city        = models.CharField(max_length=100, db_column="City")
    region      = models.CharField(max_length=100, db_column="Region")
    country     = models.CharField(max_length=100, db_column="Country")
    org         = models.CharField(max_length=255, db_column="Org")
    timezone    = models.CharField(max_length=100, db_column="Timezone")
    fetched_at  = models.DateTimeField(db_column="FetchedAt")

    class Meta:
        managed  = False
        db_table = "IP_METADATA"