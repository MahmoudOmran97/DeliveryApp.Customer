using DeliveryApp.Customer.ViewModels;

namespace DeliveryApp.Customer.Views;

public partial class ForgotPasswordPage : ContentPage
{
    public ForgotPasswordPage(ForgotPasswordViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
