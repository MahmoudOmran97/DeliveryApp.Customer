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

    public ObservableCollection<Restaurant> Restaurants { get; } = new();

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

            var r = await _api.GetRestaurantsAsync(SearchText);

            Restaurants.Clear();

            foreach (var x in r?.Data ?? new()) Restaurants.Add(x);

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

}

