using System.Globalization;
using DeliveryApp.Customer.Services;

namespace DeliveryApp.Customer.Views;

public partial class SplashPage : ContentPage
{
    readonly AuthService _auth;
    readonly FcmTokenService _fcm;

    public SplashPage(AuthService auth, FcmTokenService fcm)
    {
        InitializeComponent();

        _auth = auth;
        _fcm = fcm;

        FlowDirection = LocalizationService.Flow;

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
            await _fcm.RegisterAsync();
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