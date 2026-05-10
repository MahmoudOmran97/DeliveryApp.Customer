using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;

using CommunityToolkit.Mvvm.Input;

using DeliveryApp.Customer.Models;

using DeliveryApp.Customer.Services;

namespace DeliveryApp.Customer.ViewModels;

[QueryProperty(nameof(RestaurantId), "id")]

public partial class RestaurantViewModel : BaseViewModel

{

    readonly ApiService _api;

    readonly CartService _cart;

    [ObservableProperty] int _restaurantId;

    [ObservableProperty] Restaurant? _restaurant;

    [ObservableProperty] int _cartCount;

    public ObservableCollection<Category> Menu { get; } = new();

    public RestaurantViewModel(ApiService api, CartService cart)

    {

        _api = api;

        _cart = cart;

        _cart.CartChanged += () => CartCount = _cart.TotalCount;

    }

    partial void OnRestaurantIdChanged(int value) => LoadCommand.Execute(null);

    [RelayCommand]

    async Task LoadAsync()

    {

        if (RestaurantId == 0) return;

        IsBusy = true;

        try

        {

            var t1 = _api.GetRestaurantAsync(RestaurantId);

            var t2 = _api.GetMenuAsync(RestaurantId);

            await Task.WhenAll(t1, t2);

            Restaurant = t1.Result;

            Menu.Clear();

            foreach (var c in t2.Result ?? new()) Menu.Add(c);

        }

        finally { IsBusy = false; }

    }

    [RelayCommand]

    async Task AddToCart(Product p)

    {

        var ok = _cart.AddItem(RestaurantId, p);

        if (!ok)

        {

            bool clear = await Shell.Current.DisplayAlert(

                "Different Restaurant",

                "Your cart has items from another restaurant. Clear and add?",

                "Yes, clear", "Cancel");

            if (clear) { _cart.Clear(); _cart.AddItem(RestaurantId, p); }

        }

    }

    [RelayCommand]

    static Task OpenCart() => Shell.Current.GoToAsync("CartPage");

}

