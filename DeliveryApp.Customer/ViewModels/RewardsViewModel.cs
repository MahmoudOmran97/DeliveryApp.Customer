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
    readonly LocationService _location; // ✅ FIX: عشان نقدر نبعت lat/lng للسيرفر ونفلتر العروض بالزون

    [ObservableProperty] bool _isRefreshing;
    [ObservableProperty] string _userName = string.Empty;
    [ObservableProperty] int _pointsBalance;
    [ObservableProperty] int _couponsCount;

    public string GreetingPrefix => LocalizationService.Current.TwoLetterISOLanguageName == "ar"
        ? "أهلاً، " : "Hi, ";

    public ObservableCollection<Deal> Deals { get; } = new();
    public ObservableCollection<DealGroup> DealGroups { get; } = new();

    public RewardsViewModel(ApiService api, AuthService auth, CartService cart, LocationService location)
    {
        _api = api;
        _auth = auth;
        _cart = cart;
        _location = location;
        UserName = auth.GetUserName().Split(' ')[0];
    }

    [RelayCommand]
    async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            // ✅ نحدّث الزون من السيرفر الأول (لو الأدمن غيّره يتطبق فورًا هنا كمان)
            await _location.RefreshZoneAsync(_api);

            double? lat = _location.HasLocation ? _location.Latitude : null;
            double? lng = _location.HasLocation ? _location.Longitude : null;

            var list = await _api.GetDealsAsync(lat, lng, _location.ZoneRadiusKm);
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

            // ✅ FIX: كان بيحط سعر توصيل ثابت 15 جنيه بدل ما يجيب سعر التوصيل
            // الفعلي بتاع المحل (اللي بيتحسب حسب المسافة الحقيقية زي ما هو
            // متعمول في RestaurantViewModel/StoreCategoryProductsViewModel)
            double? lat = _location.HasLocation ? _location.Latitude : null;
            double? lng = _location.HasLocation ? _location.Longitude : null;
            var restaurant = await _api.GetRestaurantAsync(deal.RestaurantId.Value, lat, lng);
            var deliveryFee = restaurant?.DeliveryFee ?? 15m;

            var ok = _cart.AddItem(
                deal.RestaurantId.Value,
                product,
                deliveryFee: deliveryFee,
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
                _cart.AddItem(deal.RestaurantId.Value, product, deliveryFee: deliveryFee,
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
