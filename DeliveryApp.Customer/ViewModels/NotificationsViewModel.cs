using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeliveryApp.Customer.Models;
using DeliveryApp.Customer.Services;
using System.Collections.ObjectModel;

namespace DeliveryApp.Customer.ViewModels;

public partial class NotificationsViewModel : BaseViewModel

{

    readonly ApiService _api;

    [ObservableProperty] bool _isRefreshing;

    [ObservableProperty] bool _isEmpty;

    [ObservableProperty] int _unread;

    // ── Pagination (تحميل تدريجي زي التطبيقات الكبيرة) ──────────
    int _currentPage = 1;
    bool _hasMore = true;
    [ObservableProperty] bool _isLoadingMore;

    public ObservableCollection<Notification> Notifications { get; } = new();

    public NotificationsViewModel(ApiService api) { _api = api; }

    [RelayCommand]

    async Task LoadAsync()

    {

        IsBusy = true;

        _currentPage = 1;

        _hasMore = true;

        try

        {

            var r = await _api.GetNotificationsAsync(_currentPage);

            Notifications.Clear();

            if (r != null)

            {

                foreach (var n in r.Data) Notifications.Add(n);

                Unread = r.Data.Count(n => !n.IsRead);

                _hasMore = r.TotalPages.HasValue ? _currentPage < r.TotalPages.Value : r.Data.Count > 0;

            }
            else
            {
                _hasMore = false;
            }

            IsEmpty = !Notifications.Any();

        }

        finally { IsBusy = false; IsRefreshing = false; }

    }

    /// <summary>بتتنفذ لما اليوزر يقرب من آخر إشعار في القايمة (RemainingItemsThreshold)</summary>
    [RelayCommand]

    async Task LoadMoreAsync()

    {

        if (IsBusy || IsLoadingMore || !_hasMore) return;

        IsLoadingMore = true;

        try

        {

            var nextPage = _currentPage + 1;

            var r = await _api.GetNotificationsAsync(nextPage);

            if (r?.Data is { Count: > 0 })
            {
                foreach (var n in r.Data) Notifications.Add(n);
                Unread += r.Data.Count(n => !n.IsRead);
                _currentPage = nextPage;
            }

            _hasMore = r != null && (r.TotalPages.HasValue ? _currentPage < r.TotalPages.Value : r.Data.Count > 0);

        }

        finally { IsLoadingMore = false; }

    }

    [RelayCommand] async Task Refresh() { IsRefreshing = true; await LoadAsync(); }

    [RelayCommand]

    async Task MarkAllRead() { await _api.MarkAllReadAsync(); await LoadAsync(); }

    [RelayCommand]

    async Task Tap(Notification n)

    {

        if (!n.IsRead) await _api.MarkNotificationReadAsync(n.Id);

        try { await NotificationNavigationHelper.NavigateAsync(n.ActionUrl, n.OrderId); }

        catch { /* already on notifications list — nothing else to do */ }

    }

}

