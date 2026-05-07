from django.shortcuts import render
from rest_framework.views import APIView
from rest_framework.response import Response
from rest_framework import status
from rest_framework.permissions import IsAdminUser
from rest_framework_simplejwt.tokens import RefreshToken
from django.contrib.auth import authenticate
from django.utils import timezone
from .models import UrlEntry, AdminLog
from .serializers import (
    UrlEntrySerializer, UrlEntryEditSerializer, AdminLogSerializer
)
# Create your views here.

import uuid


#HELPER 
def log_action(request, old_value=None, new_value=None):
    """Write every admin mutation to ADMIN_LOG."""
    AdminLog.objects.create(
        id            = uuid.uuid4(),
        user_id       = str(request.user.id),
        session_token = str(request.auth),
        old_value     = old_value,
        new_value     = new_value,
        performed_at  = timezone.now(),
    )


#AUTH 

class AdminLoginView(APIView):
    """POST /api/admin/login — public, no auth required."""
    permission_classes = []

    def post(self, request):
        username = request.data.get("username")
        password = request.data.get("password")

        if not username or not password:
            return Response(
                {"message": "Username and password are required."},
                status=status.HTTP_400_BAD_REQUEST,
            )

        user = authenticate(username=username, password=password)

        # Must exist AND be a staff/admin account
        if not user or not user.is_staff:
            return Response(
                {"message": "Invalid credentials or insufficient privileges."},
                status=status.HTTP_401_UNAUTHORIZED,
            )

        refresh = RefreshToken.for_user(user)
        return Response({
            "access":   str(refresh.access_token),
            "refresh":  str(refresh),
            "username": user.username,
        })


class AdminLogoutView(APIView):
    permission_classes = [IsAdminUser]

    def post(self, request):
        try:
            # Blacklist the refresh token
            token = RefreshToken(request.data.get("refresh"))
            token.blacklist()
        except Exception:
            pass

        # Clear the auth cookie in the response
        response = Response({"message": "Logged out successfully."})
        response.delete_cookie("access")
        response.delete_cookie("refresh")
        response.delete_cookie("sessionid")
        return response


#URL ENTRIES

class UrlEntryListView(APIView):
    """GET /api/admin/urls  — full table with IP + user data.
       POST /api/admin/urls — create manually, no rate limit."""
    permission_classes = [IsAdminUser]

    def get(self, request):
        entries = UrlEntry.objects.select_related(
            "user", "ip_metadata"
        ).order_by("-created_at")
        return Response(UrlEntrySerializer(entries, many=True).data)

    def post(self, request):
        serializer = UrlEntrySerializer(data=request.data)
        if serializer.is_valid():
            serializer.save()
            log_action(request, old_value=None, new_value=serializer.data)
            return Response(serializer.data, status=status.HTTP_201_CREATED)
        return Response(serializer.errors, status=status.HTTP_400_BAD_REQUEST)


class UrlEntryDetailView(APIView):
    """GET/PUT/DELETE /api/admin/urls/<id>"""
    permission_classes = [IsAdminUser]

    def get_object(self, pk):
        try:
            return UrlEntry.objects.select_related(
                "user", "ip_metadata"
            ).get(pk=pk)
        except UrlEntry.DoesNotExist:
            return None

    def get(self, request, pk):
        entry = self.get_object(pk)
        if not entry:
            return Response({"message": "Entry not found."}, status=404)
        return Response(UrlEntrySerializer(entry).data)

    def put(self, request, pk):
        """Edit original_url or nickname only."""
        entry = self.get_object(pk)
        if not entry:
            return Response({"message": "Entry not found."}, status=404)

        old_snapshot = UrlEntryEditSerializer(entry).data

        serializer = UrlEntryEditSerializer(entry, data=request.data, partial=True)
        if serializer.is_valid():
            serializer.save(updated_at=timezone.now())
            log_action(request, old_value=old_snapshot, new_value=serializer.data)
            return Response(UrlEntrySerializer(entry).data)
        return Response(serializer.errors, status=400)

    def delete(self, request, pk):
        """Delete a single entry."""
        entry = self.get_object(pk)
        if not entry:
            return Response({"message": "Entry not found."}, status=404)

        old_snapshot = UrlEntrySerializer(entry).data
        entry.delete()
        log_action(request, old_value=old_snapshot, new_value=None)
        return Response(status=status.HTTP_204_NO_CONTENT)


class BulkDeleteView(APIView):
    """DELETE /api/admin/urls/bulk-delete  body: { "ids": ["uuid1", "uuid2"] }"""
    permission_classes = [IsAdminUser]

    def delete(self, request):
        ids = request.data.get("ids", [])
        if not ids:
            return Response({"message": "No IDs provided."}, status=400)

        entries    = UrlEntry.objects.filter(id__in=ids)
        old_values = list(UrlEntrySerializer(entries, many=True).data)

        deleted_count, _ = entries.delete()
        log_action(request, old_value=old_values, new_value=None)
        return Response({"deleted": deleted_count})


class UnlockEntryView(APIView):
    """PATCH /api/admin/urls/<id>/unlock — resets is_locked + failed_atempts."""
    permission_classes = [IsAdminUser]

    def patch(self, request, pk):
        try:
            entry = UrlEntry.objects.get(pk=pk)
        except UrlEntry.DoesNotExist:
            return Response({"message": "Entry not found."}, status=404)

        old_value = {
            "is_locked":      entry.is_locked,
            "failed_atempts": entry.failed_atempts,
        }

        entry.is_locked      = False
        entry.failed_atempts = 0
        entry.updated_at     = timezone.now()
        entry.save(update_fields=["is_locked", "failed_atempts", "updated_at"])

        log_action(request, old_value=old_value, new_value={
            "is_locked":      False,
            "failed_atempts": 0,
        })

        return Response({
            "message": f"Entry '{entry.nickname}' has been unlocked.",
            "entry":   UrlEntrySerializer(entry).data,
        })


#ADMIN LOGS

class AdminLogListView(APIView):
    """GET /api/admin/logs — full audit trail."""
    permission_classes = [IsAdminUser]

    def get(self, request):
        logs = AdminLog.objects.order_by("-performed_at")
        return Response(AdminLogSerializer(logs, many=True).data)