using System.Globalization;
using DeliveryApp.Customer.Services;

namespace DeliveryApp.Customer.Views;

public partial class SplashPage : ContentPage
{
    readonly AuthService _auth;

    public SplashPage(AuthService auth)
    {
        InitializeComponent();

        _auth = auth;

        string lang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

        imgLogo.Source = lang == "ar"
            ? "logo_ar.png"
            : "logo_en.png";
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await Task.Delay(2000);

        if (_auth.IsLoggedIn)
        {
            var shell = IPlatformApplication.Current!.Services.GetService<AppShell>()!;
            Application.Current!.MainPage = shell;
        }
        else
        {
            var loginPage = IPlatformApplication.Current!.Services.GetService<LoginPage>()!;
            Application.Current!.MainPage = new NavigationPage(loginPage);
        }
    }
}