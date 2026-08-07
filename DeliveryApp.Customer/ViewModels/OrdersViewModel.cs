using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;

using CommunityToolkit.Mvvm.Input;

using DeliveryApp.Customer.Models;

using DeliveryApp.Customer.Services;

namespace DeliveryApp.Customer.ViewModels;

public partial class OrdersViewModel : BaseViewModel

{

    readonly ApiService _api;

    [ObservableProperty] bool _isRefreshing;

    [ObservableProperty] bool _isEmpty;

    // ── Pagination (تحميل تدريجي زي التطبيقات الكبيرة) ──────────
    int _currentPage = 1;
    bool _hasMore = true;
    [ObservableProperty] bool _isLoadingMore;

    public ObservableCollection<Order> Orders { get; } = new();

    public OrdersViewModel(ApiService api) { _api = api; }

    [RelayCommand]

    async Task LoadAsync()

    {

        IsBusy = true;

        _currentPage = 1;

        _hasMore = true;

        try

        {

            var r = await _api.GetMyOrdersAsync(_currentPage);

            Orders.Clear();

            foreach (var o in r?.Data ?? new()) Orders.Add(o);

            IsEmpty = !Orders.Any();

            _hasMore = r != null && (r.TotalPages.HasValue ? _currentPage < r.TotalPages.Value : r.Data.Count > 0);

        }

        finally { IsBusy = false; IsRefreshing = false; }

    }

    /// <summary>بتتنفذ لما اليوزر يقرب من آخر أوردر في القايمة (RemainingItemsThreshold)</summary>
    [RelayCommand]

    async Task LoadMoreAsync()

    {

        if (IsBusy || IsLoadingMore || !_hasMore) return;

        IsLoadingMore = true;

        try

        {

            var nextPage = _currentPage + 1;

            var r = await _api.GetMyOrdersAsync(nextPage);

            if (r?.Data is { Count: > 0 })
            {
                foreach (var o in r.Data) Orders.Add(o);
                _currentPage = nextPage;
            }

            _hasMore = r != null && (r.TotalPages.HasValue ? _currentPage < r.TotalPages.Value : r.Data.Count > 0);

        }

        finally { IsLoadingMore = false; }

    }

    [RelayCommand] async Task Refresh() { IsRefreshing = true; await LoadAsync(); }

    [RelayCommand]

    static Task OpenOrder(Order o) => o.IsActive

        ? Shell.Current.GoToAsync($"OrderTrackingPage?orderId={o.Id}")

        : Shell.Current.GoToAsync($"OrderDetailPage?orderId={o.Id}");

}