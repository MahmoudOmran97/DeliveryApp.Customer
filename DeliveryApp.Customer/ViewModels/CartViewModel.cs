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

        Items.Clear();

        foreach (var i in _cart.Items) Items.Add(i);

        Total = _cart.TotalPrice;

        IsEmpty = _cart.IsEmpty;

    }

    [RelayCommand] void Inc(CartItem i) => _cart.UpdateQuantity(i.Product.Id, i.Quantity + 1);

    [RelayCommand] void Dec(CartItem i) => _cart.UpdateQuantity(i.Product.Id, i.Quantity - 1);

    [RelayCommand] void Remove(CartItem i) => _cart.RemoveItem(i.Product.Id);

    [RelayCommand]

    async Task Clear()

    {

        if (await Shell.Current.DisplayAlert("Clear cart?", "Remove all items?", "Yes", "No"))

            _cart.Clear();

    }

    [RelayCommand]

    Task Checkout() => IsEmpty

        ? AlertAsync("Your cart is empty")

        : Shell.Current.GoToAsync("CheckoutPage");

}

