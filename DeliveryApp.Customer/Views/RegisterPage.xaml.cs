using DeliveryApp.Customer.ViewModels;

namespace DeliveryApp.Customer.Views;

public partial class RegisterPage : ContentPage

{

    public RegisterPage(RegisterViewModel vm) { InitializeComponent(); BindingContext = vm; }

}

