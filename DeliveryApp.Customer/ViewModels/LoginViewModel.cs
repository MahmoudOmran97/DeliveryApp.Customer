using CommunityToolkit.Mvvm.ComponentModel;

using CommunityToolkit.Mvvm.Input;

using DeliveryApp.Customer.Services;
using DeliveryApp.Customer.Views;

namespace DeliveryApp.Customer.ViewModels;

public partial class LoginViewModel : BaseViewModel

{

    readonly ApiService _api;

    readonly AuthService _auth;

    readonly FcmTokenService _fcm;

    [ObservableProperty] string _email = string.Empty;

    [ObservableProperty] string _password = string.Empty;

    public LoginViewModel(ApiService api, AuthService auth, FcmTokenService fcm)

    { _api = api; _auth = auth; _fcm = fcm; }

    [RelayCommand]

    async Task LoginAsync()

    {

        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))

        { await AlertAsync("Please fill in all fields"); return; }

        IsBusy = true;

        try

        {

            var r = await _api.LoginAsync(Email, Password);

            if (r != null)

            {

                _auth.SaveUser(r.Token, r.Id, r.FullName, r.Email, r.Role);

                // ← بعت الـ FCM token للـ API بعد اللوجين
                _ = Task.Run(() => _fcm.RegisterAsync());

                var shell = IPlatformApplication.Current!.Services.GetService<AppShell>()!;
                Application.Current!.MainPage = shell;

            }

            else await AlertAsync("Invalid email or password");

        }

        finally { IsBusy = false; }

    }

    [RelayCommand]
    async Task GoToRegister()
    {
        // ✅ Navigation عادي
        await Application.Current!.MainPage!.Navigation.PushAsync(
            IPlatformApplication.Current!.Services.GetService<RegisterPage>()!);
    }

}


