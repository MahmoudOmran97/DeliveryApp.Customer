using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeliveryApp.Customer.Models;
using DeliveryApp.Customer.Services;

namespace DeliveryApp.Customer.ViewModels;

public partial class RewardsViewModel : BaseViewModel
{
    readonly ApiService _api;
    readonly AuthService _auth;

    [ObservableProperty] bool _isRefreshing;
    [ObservableProperty] string _userName = string.Empty;

    public ObservableCollection<Deal> Deals { get; } = new();

    // Group deals by restaurant
    public ObservableCollection<DealGroup> DealGroups { get; } = new();

    public RewardsViewModel(ApiService api, AuthService auth)
    {
        _api = api;
        _auth = auth;
        UserName = auth.GetUserName().Split(' ')[0];
    }

    [RelayCommand]
    async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            var list = await _api.GetDealsAsync();
            Deals.Clear();
            DealGroups.Clear();

            var allDeals = list ?? new();
            foreach (var d in allDeals) Deals.Add(d);

            // Group by restaurant
            var grouped = allDeals
                .GroupBy(d => d.RestaurantName ?? "عروض عامة")
                .Select(g => new DealGroup(g.Key, g.ToList()));

            foreach (var g in grouped) DealGroups.Add(g);
        }
        finally { IsBusy = false; IsRefreshing = false; }
    }

    [RelayCommand]
    async Task RefreshAsync() { IsRefreshing = true; await LoadAsync(); }

    [RelayCommand]
    static Task OpenRestaurant(Deal d)
    {
        if (d.RestaurantId.HasValue)
            return Shell.Current.GoToAsync($"RestaurantPage?id={d.RestaurantId}");
        return Task.CompletedTask;
    }
}

public class DealGroup : List<Deal>
{
    public string RestaurantName { get; }

    public DealGroup(string name, List<Deal> deals) : base(deals)
        => RestaurantName = name;
}
