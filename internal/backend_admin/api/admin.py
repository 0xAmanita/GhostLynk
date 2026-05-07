from django.contrib import admin

# Register your models here.
from django.contrib import admin
from .models import UrlEntry, AppUser, IpMetadata, AdminLog, Session, PasswordResetToken


class IpMetadataInline(admin.StackedInline):
    model           = IpMetadata
    extra           = 0
    readonly_fields = [
        "ip_address", "city", "region",
        "country", "org", "timezone", "fetched_at"
    ]
    can_delete      = False


@admin.register(UrlEntry)
class UrlEntryAdmin(admin.ModelAdmin):
    list_display = [
        "nickname", "original_url", "obfuscated_url",
        "get_user_id", "get_ip", "get_city", "get_region",
        "get_country", "get_org", "get_timezone",
        "is_locked", "failed_attempts", "created_at",
    ]
    list_filter   = ["is_locked"]
    search_fields = ["nickname", "original_url", "obfuscated_url"]
    readonly_fields = [
        "id", "obfuscated_url", "passkey_hash",
        "failed_attempts", "created_at", "updated_at",
    ]
    inlines = [IpMetadataInline]
    actions = ["unlock_entries"]

    def get_user_id(self, obj):
        try: return str(obj.user_id)
        except: return "—"
    get_user_id.short_description = "User ID"

    def get_ip(self, obj):
        try: return obj.ip_metadata.ip_address
        except: return "—"
    get_ip.short_description = "IP"

    def get_city(self, obj):
        try: return obj.ip_metadata.city
        except: return "—"
    get_city.short_description = "City"

    def get_region(self, obj):
        try: return obj.ip_metadata.region
        except: return "—"
    get_region.short_description = "Region"

    def get_country(self, obj):
        try: return obj.ip_metadata.country
        except: return "—"
    get_country.short_description = "Country"

    def get_org(self, obj):
        try: return obj.ip_metadata.org
        except: return "—"
    get_org.short_description = "Org"

    def get_timezone(self, obj):
        try: return obj.ip_metadata.timezone
        except: return "—"
    get_timezone.short_description = "Timezone"

    def unlock_entries(self, request, queryset):
        queryset.update(is_locked=False, failed_attempts=0)
        self.message_user(request, f"{queryset.count()} entry/entries unlocked.")
    unlock_entries.short_description = "Unlock selected entries"


@admin.register(IpMetadata)
class IpMetadataAdmin(admin.ModelAdmin):
    list_display  = [
        "url_entry", "ip_address", "city",
        "region", "country", "org", "timezone", "fetched_at"
    ]
    search_fields = ["ip_address", "city", "country"]
    readonly_fields = [
        "url_entry", "ip_address", "city",
        "region", "country", "org", "timezone", "fetched_at"
    ]


@admin.register(AppUser)
class AppUserAdmin(admin.ModelAdmin):
    list_display  = [
        "id", "username", "email",
        "first_name", "last_name", "created_at"
    ]
    search_fields = ["username", "email"]
    readonly_fields = [
        "id", "email", "username",
        "password_hash", "created_at", "updated_at"
    ]


@admin.register(AdminLog)
class AdminLogAdmin(admin.ModelAdmin):
    list_display  = ["user_id", "session_token", "performed_at"]
    readonly_fields = [
        "id", "user_id", "session_token",
        "old_value", "new_value", "performed_at"
    ]
    def has_add_permission(self, request): return False
    def has_change_permission(self, request, obj=None): return False
    def has_delete_permission(self, request, obj=None): return False


@admin.register(Session)
class SessionAdmin(admin.ModelAdmin):
    list_display  = [
        "user", "session_token",
        "last_submit_at", "last_deobfuscate_at",
        "created_at", "expires_at"
    ]
    readonly_fields = [
        "id", "user", "session_token",
        "last_submit_at", "last_deobfuscate_at",
        "created_at", "expires_at"
    ]


@admin.register(PasswordResetToken)
class PasswordResetTokenAdmin(admin.ModelAdmin):
    list_display  = [
        "user", "token_hash",
        "expires_at", "used_at", "created_at"
    ]
    readonly_fields = [
        "id", "user", "token_hash",
        "resend_email_id", "expires_at", "used_at", "created_at"
    ]