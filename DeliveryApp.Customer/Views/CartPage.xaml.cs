using DeliveryApp.Customer.ViewModels;

namespace DeliveryApp.Customer.Views;

public partial class CartPage : ContentPage

{

    public CartPage(CartViewModel vm) { InitializeComponent(); BindingContext = vm; }

}

