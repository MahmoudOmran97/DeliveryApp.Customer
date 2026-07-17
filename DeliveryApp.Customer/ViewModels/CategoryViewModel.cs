using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeliveryApp.Customer.Models;
using DeliveryApp.Customer.Services;

namespace DeliveryApp.Customer.ViewModels;

[QueryProperty(nameof(CategoryName), "category")]
[QueryProperty(nameof(SearchText), "search")]
public partial class CategoryViewModel : BaseViewModel
{
    readonly ApiService _api;
    readonly LocationService _location;

    [ObservableProperty] string _categoryName = string.Empty;
    [ObservableProperty] string _displayTitle = string.Empty;
    [ObservableProperty] string _searchText = string.Empty;

    /// <summary>نفس الـ Placeholder المستخدم في صفحة الرئيسية</summary>
    public string SearchHint => LocalizationService.Get("SearchPlaceholder");

    public ObservableCollection<Restaurant> Items { get; } = new();

    /// <summary>True لما تبقى القايمة فاضية بعد التحميل (مش وانت لسه بتحمل)</summary>
    public bool HasNoResults => !IsBusy && Items.Count == 0;

    public CategoryViewModel(ApiService api, LocationService location)
    {
        _api = api;
        _location = location;
        Items.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasNoResults));
    }

    partial void OnCategoryNameChanged(string value) => RefreshTitle();

    partial void OnSearchTextChanged(string value) => RefreshTitle();

    /// <summary>
    /// العنوان بيتحدد حسب الكاتيجوري، أو "نتائج البحث" لو جاي من السيرش من غير كاتيجوري محددة
    /// </summary>
    void RefreshTitle()
    {
        // نستخدم LocalizationService عشان يدعم Arabic/English
        var locKey = CategoryName switch
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

        DisplayTitle = locKey != null
            ? LocalizationService.Get(locKey)
            : !string.IsNullOrWhiteSpace(SearchText)
                ? LocalizationService.Get("SearchResults")
                : LocalizationService.Get("Cat_All");
    }

    [RelayCommand]
    static Task GoBack() => Shell.Current.GoToAsync("..");

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
                search: SearchText,
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
        finally { IsBusy = false; OnPropertyChanged(nameof(HasNoResults)); }
    }

    /// <summary>بيتنفذ لما المستخدم يدوس Enter/بحث في مربع السيرش اللي فوق</summary>
    [RelayCommand]
    Task SearchAsync() => LoadAsync();

    [RelayCommand]
    static Task OpenRestaurant(Restaurant r)
        => Shell.Current.GoToAsync($"RestaurantPage?id={r.Id}");
}