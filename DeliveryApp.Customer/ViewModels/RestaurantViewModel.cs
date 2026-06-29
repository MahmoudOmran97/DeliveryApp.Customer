// ═══════════════════════════════════════════════════════════════
// DeliveryApp.Customer / ViewModels / RestaurantViewModel.cs
// ═══════════════════════════════════════════════════════════════
using System.Collections.ObjectModel;
using System.Text.Json;
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
    [ObservableProperty] bool _isPharmacy;
    [ObservableProperty] string? _prescriptionPreview;
    [ObservableProperty] string _prescriptionNotes = "";

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
            IsPharmacy = Restaurant?.StoreType.Equals("Pharmacy", StringComparison.OrdinalIgnoreCase) == true;
            Menu.Clear();
            foreach (var c in t2.Result ?? new()) Menu.Add(c);
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    async Task ProductTapped(Product p)
    {
        if (p.HasVariants)
        {
            var json = Uri.EscapeDataString(JsonSerializer.Serialize(p));
            await Shell.Current.GoToAsync(
                $"ProductOptionsPage?product={json}&restaurantId={RestaurantId}&deliveryFee={Restaurant?.DeliveryFee ?? 15m}");
            return;
        }
        await AddToCart(p);
    }

    [RelayCommand]
    async Task AddToCart(Product p)
    {
        var ok = _cart.AddItem(RestaurantId, p, deliveryFee: Restaurant?.DeliveryFee ?? 15m);
        if (!ok)
        {
            bool clear = await Shell.Current.DisplayAlert(
                LocalizationService.Get("DifferentRestaurant"),
                LocalizationService.Get("DifferentRestaurantMsg"),
                LocalizationService.Get("YesClear"),
                LocalizationService.Get("Cancel"));

            if (clear)
            {
                _cart.Clear();
                _cart.AddItem(RestaurantId, p, deliveryFee: Restaurant?.DeliveryFee ?? 15m);
            }
        }
    }

    [RelayCommand]
    async Task UploadPrescriptionAsync()
    {
        try
        {
            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = LocalizationService.Get("UploadPrescription"),
                FileTypes = FilePickerFileType.Images
            });
            if (result == null) return;

            IsBusy = true;
            var url = await _api.UploadPrescriptionAsync(result);
            if (string.IsNullOrEmpty(url))
            {
                await AlertAsync(LocalizationService.Get("UploadFailed"));
                return;
            }

            PrescriptionPreview = url.StartsWith("http") ? url : $"https://deliveryappapi.runasp.net{url}";
            _cart.SetPrescription(RestaurantId, url, PrescriptionNotes, Restaurant?.DeliveryFee ?? 15m);
            await AlertAsync(LocalizationService.Get("PrescriptionAdded"));
        }
        catch (Exception ex)
        {
            await AlertAsync(ex.Message);
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    async Task OrderPrescriptionAsync()
    {
        if (string.IsNullOrEmpty(_cart.PrescriptionImageUrl))
        {
            await AlertAsync(LocalizationService.Get("UploadPrescriptionFirst"));
            return;
        }
        await Shell.Current.GoToAsync("CheckoutPage");
    }

    [RelayCommand]
    static Task OpenCart() => Shell.Current.GoToAsync("CartPage");
}
