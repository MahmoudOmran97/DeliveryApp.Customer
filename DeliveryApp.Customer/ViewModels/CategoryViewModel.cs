using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeliveryApp.Customer.Models;
using DeliveryApp.Customer.Services;

namespace DeliveryApp.Customer.ViewModels;

[QueryProperty(nameof(CategoryName), "category")]
public partial class CategoryViewModel : BaseViewModel
{
    readonly ApiService _api;
    readonly LocationService _location;

    [ObservableProperty] string _categoryName = string.Empty;
    [ObservableProperty] string _displayTitle = string.Empty;

    public ObservableCollection<Restaurant> Items { get; } = new();

    public CategoryViewModel(ApiService api, LocationService location)
    {
        _api = api;
        _location = location;
    }

    partial void OnCategoryNameChanged(string value)
    {
        // نستخدم LocalizationService عشان يدعم Arabic/English
        var locKey = value switch
        {
            "Restaurants" => "Cat_Restaurants",
            "Grocery" => "Cat_Grocery",
            "Pharmacy" => "Cat_Pharmacy",
            "Vegetables" => "Cat_Vegetables",
            "Accessories" => "Cat_Accessories",
            "Supermarket" => "Cat_Supermarket",
            "Drinks" => "Cat_Drinks",
            _ => null
        };
        DisplayTitle = locKey != null ? LocalizationService.Get(locKey) : value;
        _ = LoadAsync();
    }

    [RelayCommand]
    async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            // بنبعت lat/lng للـ API مباشرة — هو بيفلتر جوه الـ 10km
            double? lat = _location.HasLocation ? _location.Latitude : null;
            double? lng = _location.HasLocation ? _location.Longitude : null;

            var result = await _api.GetRestaurantsAsync(
                category: CategoryName,
                lat: lat,
                lng: lng,
                radiusKm: 10.0,
                minRating: 0.0,
                sortBy: "rating");

            Items.Clear();
            foreach (var x in result?.Data ?? new())
                Items.Add(x);
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    static Task OpenRestaurant(Restaurant r)
        => Shell.Current.GoToAsync($"RestaurantPage?id={r.Id}");
}