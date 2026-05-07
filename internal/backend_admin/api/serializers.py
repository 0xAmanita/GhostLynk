from rest_framework import serializers
from .models import UrlEntry, IpMetadata, AppUser, AdminLog


class IpMetadataSerializer(serializers.ModelSerializer):
    class Meta:
        model  = IpMetadata
        fields = [
            "ip_address", "city", "region",
            "country", "org", "timezone", "fetched_at"
        ]


class AppUserSerializer(serializers.ModelSerializer):
    class Meta:
        model  = AppUser
        fields = ["id", "username", "email", "first_name", "last_name"]


class UrlEntrySerializer(serializers.ModelSerializer):
    ip_metadata = IpMetadataSerializer(read_only=True)
    user        = AppUserSerializer(read_only=True)

    class Meta:
        model  = UrlEntry
        fields = [
            "id", "user", "original_url", "obfuscated_url",
            "nickname", "failed_atempts", "is_locked",
            "created_at", "updated_at", "ip_metadata",
        ]
        # passkey_hash intentionally excluded — never expose it


class UrlEntryEditSerializer(serializers.ModelSerializer):
    """Admin may only edit original_url and nickname per spec."""
    class Meta:
        model  = UrlEntry
        fields = ["original_url", "nickname"]


class AdminLogSerializer(serializers.ModelSerializer):
    class Meta:
        model  = AdminLog
        fields = [
            "id", "user_id", "session_token",
            "old_value", "new_value", "performed_at"
        ]