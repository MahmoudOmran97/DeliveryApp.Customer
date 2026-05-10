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

    public ObservableCollection<Order> Orders { get; } = new();

    public OrdersViewModel(ApiService api) { _api = api; }

    [RelayCommand]

    async Task LoadAsync()

    {

        IsBusy = true;

        try

        {

            var r = await _api.GetMyOrdersAsync();

            Orders.Clear();

            foreach (var o in r?.Data ?? new()) Orders.Add(o);

            IsEmpty = !Orders.Any();

        }

        finally { IsBusy = false; IsRefreshing = false; }

    }

    [RelayCommand] async Task Refresh() { IsRefreshing = true; await LoadAsync(); }

    [RelayCommand]

    static Task OpenOrder(Order o) => o.IsActive

        ? Shell.Current.GoToAsync($"OrderTrackingPage?orderId={o.Id}")

        : Shell.Current.GoToAsync($"OrderDetailPage?orderId={o.Id}");

}

