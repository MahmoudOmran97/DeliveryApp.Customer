// ═══════════════════════════════════════════════════════════════
// DeliveryApp.Customer / ViewModels / CartViewModel.cs
// ═══════════════════════════════════════════════════════════════
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeliveryApp.Customer.Models;
using DeliveryApp.Customer.Services;

namespace DeliveryApp.Customer.ViewModels;

public partial class CartViewModel : BaseViewModel
{
    readonly CartService _cart;

    [ObservableProperty] decimal _total;
    [ObservableProperty] bool _isEmpty;

    public ObservableCollection<CartItem> Items { get; } = new();

    public CartViewModel(CartService cart)
    {
        _cart = cart;
        Sync();
        _cart.CartChanged += Sync;
    }

    void Sync()
    {
        var toRemove = Items.Where(i => !_cart.Items.Any(c => c.Product.Id == i.Product.Id)).ToList();
        foreach (var r in toRemove) Items.Remove(r);

        foreach (var cartItem in _cart.Items)
        {
            var existing = Items.FirstOrDefault(i => i.Product.Id == cartItem.Product.Id);
            if (existing != null)
                existing.Quantity = cartItem.Quantity;
            else
                Items.Add(cartItem);
        }

        Total = _cart.TotalPrice;
        IsEmpty = !Items.Any();
    }

    [RelayCommand] void Inc(CartItem i) => _cart.UpdateQuantity(i.Product.Id, i.Quantity + 1);
    [RelayCommand] void Dec(CartItem i) => _cart.UpdateQuantity(i.Product.Id, i.Quantity - 1);
    [RelayCommand] void Remove(CartItem i) => _cart.RemoveItem(i.Product.Id);

    [RelayCommand]
    async Task Clear()
    {
        // ✅ ترجمة dialog مسح السلة
        var title   = LocalizationService.Get("MyCart");
        var message = LocalizationService.Get("CartEmptySub");
        var yes     = LocalizationService.Get("Yes");
        var no      = LocalizationService.Get("No");

        if (await Shell.Current.DisplayAlert(title, message, yes, no))
            _cart.Clear();
    }

    [RelayCommand]
    Task Checkout() => IsEmpty
        // ✅ ترجمة رسالة السلة الفارغة
        ? AlertAsync(LocalizationService.Get("CartEmpty"))
        : Shell.Current.GoToAsync("CheckoutPage");
}
