using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeliveryApp.Customer.Models;
using DeliveryApp.Customer.Services;

namespace DeliveryApp.Customer.ViewModels;

public partial class HomeViewModel : BaseViewModel
{
    readonly ApiService _api;
    readonly CartService _cart;

    [ObservableProperty] string _searchText = string.Empty;
    [ObservableProperty] bool _isRefreshing;
    [ObservableProperty] string _userName = string.Empty;
    [ObservableProperty] int _cartCount;
    [ObservableProperty] int _currentBannerIndex;

    public string GreetingPrefix => LocalizationService.Current.TwoLetterISOLanguageName == "ar"
        ? "أهلاً، "
        : "Hey, ";

    public string SearchHint => LocalizationService.Get("SearchPlaceholder");
    public string SectionTitle => LocalizationService.Get("RestaurantsNearYou");

    public ObservableCollection<Restaurant> Restaurants { get; } = new();
    public ObservableCollection<Banner> Banners { get; } = new();

    public HomeViewModel(ApiService api, AuthService auth, CartService cart)
    {
        _api = api; _cart = cart;
        UserName = auth.GetUserName().Split(' ')[0];
        _cart.CartChanged += () => CartCount = _cart.TotalCount;
    }

    [RelayCommand]
    async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            var bannersTask = _api.GetBannersAsync();
            var restaurantsTask = _api.GetRestaurantsAsync(SearchText);

            await Task.WhenAll(bannersTask, restaurantsTask);

            Banners.Clear();
            foreach (var b in bannersTask.Result ?? new()) Banners.Add(b);

            Restaurants.Clear();
            foreach (var x in restaurantsTask.Result?.Data ?? new()) Restaurants.Add(x);
        }
        finally { IsBusy = false; IsRefreshing = false; }
    }

    [RelayCommand]
    async Task RefreshAsync() { IsRefreshing = true; await LoadAsync(); }

    [RelayCommand]
    Task SearchAsync() => LoadAsync();

    [RelayCommand]
    static Task OpenRestaurant(Restaurant r)
        => Shell.Current.GoToAsync($"RestaurantPage?id={r.Id}");

    [RelayCommand]
    static Task OpenCart() => Shell.Current.GoToAsync("CartPage");

    [RelayCommand]
    static Task OpenCoupons() => Shell.Current.GoToAsync(nameof(Views.CouponsPage));

    [RelayCommand]
    static Task OpenRewards() => Shell.Current.GoToAsync(nameof(Views.RewardsPage));
}
