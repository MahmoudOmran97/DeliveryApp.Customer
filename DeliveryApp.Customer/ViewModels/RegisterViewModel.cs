// ═══════════════════════════════════════════════════════════════
// DeliveryApp.Customer / ViewModels / RegisterViewModel.cs
// ═══════════════════════════════════════════════════════════════
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeliveryApp.Customer.Services;

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
        // ✅ ترجمة رسائل التحقق
        if (string.IsNullOrWhiteSpace(FullName) || string.IsNullOrWhiteSpace(Email) ||
            string.IsNullOrWhiteSpace(Phone) || string.IsNullOrWhiteSpace(Password))
        { await AlertAsync(LocalizationService.Get("LoginFillFields")); return; }

        if (Password != ConfirmPassword)
        { await AlertAsync(LocalizationService.Get("PasswordsNotMatch")); return; }

        IsBusy = true;
        try
        {
            var r = await _api.RegisterAsync(FullName, Email, Password, Phone);
            if (r != null)
            {
                _auth.SaveUser(r.Token, r.Id, r.FullName, r.Email, r.Role);
                var shell = IPlatformApplication.Current!.Services.GetService<AppShell>()!;
                Application.Current!.MainPage = shell;
            }
            // ✅ ترجمة رسالة فشل التسجيل
            else await AlertAsync(LocalizationService.Get("RegisterFailed"));
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    async Task GoBack()
    {
        await Application.Current!.MainPage!.Navigation.PopAsync();
    }
}
