using DeliveryApp.Customer.ViewModels;

namespace DeliveryApp.Customer.Views;

public partial class CheckoutPage : ContentPage

{

    public CheckoutPage(CheckoutViewModel vm) { InitializeComponent(); BindingContext = vm; }

}

