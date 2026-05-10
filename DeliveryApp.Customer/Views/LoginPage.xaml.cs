using DeliveryApp.Customer.ViewModels;

namespace DeliveryApp.Customer.Views;

public partial class LoginPage : ContentPage
{
    public LoginPage(LoginViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}