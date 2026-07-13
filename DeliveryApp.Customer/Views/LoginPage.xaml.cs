using DeliveryApp.Customer.ViewModels;
using System.Globalization;

namespace DeliveryApp.Customer.Views;

public partial class LoginPage : ContentPage
{
    public LoginPage(LoginViewModel vm)
    {
        InitializeComponent();
        string lang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

        imgLogo.Source = lang == "ar"
            ? "logo_ar.png"
            : "logo_en.png";
        BindingContext = vm;
    }
}