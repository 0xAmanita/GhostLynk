from django.urls import path
from .views import (
    AdminLoginView, AdminLogoutView,
    UrlEntryListView, UrlEntryDetailView,
    BulkDeleteView, UnlockEntryView,
    AdminLogListView,
)

urlpatterns = [
    # Auth
    path("admin/login",                  AdminLoginView.as_view()),
    path("admin/logout",                 AdminLogoutView.as_view()),

    # Entries
    path("admin/urls",                   UrlEntryListView.as_view()),
    path("admin/urls/bulk-delete",       BulkDeleteView.as_view()),   # must be before <uuid:pk>
    path("admin/urls/<uuid:pk>",         UrlEntryDetailView.as_view()),
    path("admin/urls/<uuid:pk>/unlock",  UnlockEntryView.as_view()),

    # Logs
    path("admin/logs",                   AdminLogListView.as_view()),
]