from django.db import models
import uuid
# Create your models here.



class AppUser(models.Model):
    """ERD: Users table"""
    id            = models.UUIDField(primary_key=True, default=uuid.uuid4)
    email         = models.CharField(max_length=255, unique=True)
    username      = models.CharField(max_length=255, unique=True)
    first_name    = models.CharField(max_length=255)
    last_name     = models.CharField(max_length=255)
    address       = models.TextField()
    password_hash = models.CharField(max_length=255)
    created_at    = models.DateTimeField()
    updated_at    = models.DateTimeField()

    class Meta:
        managed  = False
        db_table = "Users"
        verbose_name        = "App User"
        verbose_name_plural = "App Users"


class UrlEntry(models.Model):
    """ERD: URL_ENTRIES table"""
    id             = models.UUIDField(primary_key=True, default=uuid.uuid4)
    user           = models.ForeignKey(
                         AppUser, on_delete=models.CASCADE,
                         db_column="user_id", related_name="url_entries")
    original_url   = models.TextField()
    obfuscated_url = models.TextField(unique=True)
    nickname       = models.CharField(max_length=255)
    passkey_hash   = models.CharField(max_length=255)
    failed_attempts = models.IntegerField(default=0)
    is_locked      = models.BooleanField(default=False)
    created_at     = models.DateTimeField()
    updated_at     = models.DateTimeField()

    class Meta:
        managed  = False
        db_table = "URL_ENTRIES"
        verbose_name = "URL Entry"             
        verbose_name_plural = "URL Entries"


class Session(models.Model):
    """ERD: SESSIONS table"""
    id                   = models.UUIDField(primary_key=True, default=uuid.uuid4)
    user                 = models.ForeignKey(
                               AppUser, on_delete=models.CASCADE,
                               db_column="user_id", related_name="sessions")
    session_token        = models.CharField(max_length=255, unique=True)
    last_submit_at       = models.DateTimeField(null=True, blank=True)
    last_deobfuscate_at = models.DateTimeField(null=True, blank=True)
    created_at           = models.DateTimeField()
    expires_at           = models.DateTimeField()

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
        managed  = False
        db_table = "ADMIN_LOG"
        verbose_name        = "Admin Log"
        verbose_name_plural = "Admin Logs"


class PasswordResetToken(models.Model):
    """ERD: PASSWORD_RESET_TOKENS table"""
    id              = models.UUIDField(primary_key=True, default=uuid.uuid4)
    user            = models.ForeignKey(
                          AppUser, on_delete=models.CASCADE,
                          db_column="user_id", related_name="reset_tokens")
    token_hash      = models.CharField(max_length=255, unique=True)
    resend_email_id = models.CharField(max_length=255, null=True, blank=True)
    expires_at      = models.DateTimeField()
    used_at         = models.DateTimeField(null=True, blank=True)
    created_at      = models.DateTimeField()

    class Meta:
        managed  = False
        db_table = "PASSWORD_RESET_TOKENS"
        verbose_name        = "Password Reset Token"
        verbose_name_plural = "Password Reset Tokens"


class IpMetadata(models.Model):
    """ERD: IP_METADATA table"""
    id          = models.UUIDField(primary_key=True, default=uuid.uuid4)
    url_entry   = models.OneToOneField(
                      UrlEntry, on_delete=models.CASCADE,
                      db_column="url_entry_id", related_name="ip_metadata")
    ip_address  = models.CharField(max_length=100)
    city        = models.CharField(max_length=100)
    region      = models.CharField(max_length=100)
    country     = models.CharField(max_length=100)
    org         = models.CharField(max_length=255)
    timezone    = models.CharField(max_length=100)
    fetched_at  = models.DateTimeField()

    class Meta:
        managed  = False
        db_table = "IP_METADATA"