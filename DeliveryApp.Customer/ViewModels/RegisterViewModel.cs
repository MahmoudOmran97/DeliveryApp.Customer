using CommunityToolkit.Mvvm.ComponentModel;

using CommunityToolkit.Mvvm.Input;

using DeliveryApp.Customer.Services;
using static Microsoft.Maui.ApplicationModel.Permissions;

namespace DeliveryApp.Customer.ViewModels;

public partial class RegisterViewModel : BaseViewModel
{
    readonly ApiService _api;
    readonly AuthService _auth;

    [ObservableProperty] string _fullName = string.Empty;
    [ObservableProperty] string _email = string.Empty;
    [ObservableProperty] string _phone = string.Empty;
    [ObservableProperty] string _password = string.Empty;
    [ObservableProperty] string _confirmPassword = string.Empty;

    public RegisterViewModel(ApiService api, AuthService auth)
    { _api = api; _auth = auth; }

    [RelayCommand]
    async Task RegisterAsync()
    {
        if (string.IsNullOrWhiteSpace(FullName) || string.IsNullOrWhiteSpace(Email) ||
            string.IsNullOrWhiteSpace(Phone) || string.IsNullOrWhiteSpace(Password))
        { await AlertAsync("Please fill in all fields"); return; }

        if (Password != ConfirmPassword)
        { await AlertAsync("Passwords do not match"); return; }

        IsBusy = true;
        try
        {
            var r = await _api.RegisterAsync(FullName, Email, Password, Phone);
            if (r != null)
            {
                _auth.SaveUser(r.Token, r.Id, r.FullName, r.Email, r.Role);
                // ✅ من DI
                var shell = IPlatformApplication.Current!.Services.GetService<AppShell>()!;
                Application.Current!.MainPage = shell;
            }
            else await AlertAsync("Registration failed. Email may already exist.");
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    async Task GoBack()
    {
        // ✅ بدل Shell.Current
        await Application.Current!.MainPage!.Navigation.PopAsync();
    }
}
