using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeliveryApp.Customer.Models;
using DeliveryApp.Customer.Services;
using DeliveryApp.Customer.Views;

namespace DeliveryApp.Customer.ViewModels;

public partial class RewardsViewModel : BaseViewModel
{
    readonly ApiService _api;
    readonly AuthService _auth;
    readonly CartService _cart;

    [ObservableProperty] bool _isRefreshing;
    [ObservableProperty] string _userName = string.Empty;
    [ObservableProperty] int _pointsBalance;
    [ObservableProperty] int _couponsCount;

    public string GreetingPrefix => LocalizationService.Current.TwoLetterISOLanguageName == "ar"
        ? "أهلاً، " : "Hi, ";

    public ObservableCollection<Deal> Deals { get; } = new();
    public ObservableCollection<DealGroup> DealGroups { get; } = new();

    public RewardsViewModel(ApiService api, AuthService auth, CartService cart)
    {
        _api = api;
        _auth = auth;
        _cart = cart;
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

            var pointsResult = await _api.GetPointsAsync();
            PointsBalance = pointsResult.Balance;

            var coupons = await _api.GetMyCouponsAsync();
            if (coupons != null)
                CouponsCount = coupons.Count(c => c.Status == "Available");

            foreach (var g in allDeals.GroupBy(d => d.RestaurantName ?? "عروض عامة")
                         .Select(x => new DealGroup(x.Key, x.ToList())))
                DealGroups.Add(g);
        }
        finally { IsBusy = false; IsRefreshing = false; }
    }

    [RelayCommand]
    async Task RefreshAsync() { IsRefreshing = true; await LoadAsync(); }

    [RelayCommand]
    async Task AddDealToCart(Deal deal)
    {
        if (!deal.ProductId.HasValue || !deal.RestaurantId.HasValue || !deal.DiscountedPrice.HasValue)
        {
            if (deal.RestaurantId.HasValue)
                await Shell.Current.GoToAsync($"RestaurantPage?id={deal.RestaurantId}");
            return;
        }

        IsBusy = true;
        try
        {
            var product = await _api.GetProductAsync(deal.ProductId.Value);
            if (product == null)
            {
                await AlertAsync(LocalizationService.Get("ProductNotFound"));
                return;
            }

            if (product.HasVariants)
            {
                await Shell.Current.GoToAsync($"RestaurantPage?id={deal.RestaurantId}");
                return;
            }

            var ok = _cart.AddItem(
                deal.RestaurantId.Value,
                product,
                deliveryFee: 15m,
                unitPrice: deal.DiscountedPrice.Value,
                dealId: deal.Id,
                notes: deal.Title);

            if (!ok)
            {
                var clear = await Shell.Current.DisplayAlert(
                    LocalizationService.Get("DifferentRestaurant"),
                    LocalizationService.Get("DifferentRestaurantMsg"),
                    LocalizationService.Get("YesClear"),
                    LocalizationService.Get("Cancel"));
                if (!clear) return;
                _cart.Clear();
                _cart.AddItem(deal.RestaurantId.Value, product, deliveryFee: 15m,
                    unitPrice: deal.DiscountedPrice.Value, dealId: deal.Id, notes: deal.Title);
            }

            await Shell.Current.GoToAsync("CartPage");
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    async Task OpenPoints() => await Shell.Current.GoToAsync(nameof(PointsPage));

    [RelayCommand]
    async Task OpenCoupons() => await Shell.Current.GoToAsync(nameof(CouponsPage));
}

public class DealGroup : List<Deal>
{
    public string RestaurantName { get; }
    public DealGroup(string name, List<Deal> deals) : base(deals) => RestaurantName = name;
}
