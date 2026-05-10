using CommunityToolkit.Mvvm.ComponentModel;

using CommunityToolkit.Mvvm.Input;

using DeliveryApp.Customer.Models;

using DeliveryApp.Customer.Services;
using DeliveryApp.Customer.Views;

namespace DeliveryApp.Customer.ViewModels;

public partial class ProfileViewModel : BaseViewModel

{

    readonly ApiService _api;

    readonly AuthService _auth;

    readonly LoginPage _loginPage;

    [ObservableProperty] User? _user;

    [ObservableProperty] bool _isEditing;

    [ObservableProperty] string _editName = string.Empty;

    [ObservableProperty] string _editPhone = string.Empty;

    [ObservableProperty] string _editAddress = string.Empty;

    // ── Localization ──────────────────────────────────────────────
    public string LanguageLabel => LocalizationService.Get("ChangeLanguage");

    public string CurrentLangDisplay => LocalizationService.Current.TwoLetterISOLanguageName == "ar"
        ? "العربية 🇸🇦"
        : "English 🇬🇧";

    public ProfileViewModel(ApiService api, AuthService auth, LoginPage loginPage)
    {
        _api = api; _auth = auth; _loginPage = loginPage;
    }

    [RelayCommand]

    async Task LoadAsync()

    {

        IsBusy = true;

        try { User = await _api.GetProfileAsync(); }

        finally { IsBusy = false; }

    }

    [RelayCommand]

    void StartEdit()

    {

        if (User == null) return;

        EditName = User.FullName; EditPhone = User.Phone; EditAddress = User.Address ?? "";

        IsEditing = true;

    }

    [RelayCommand]

    async Task Save()

    {

        IsBusy = true;

        try

        {

            if (await _api.UpdateProfileAsync(EditName, EditPhone, EditAddress))

            { IsEditing = false; await LoadAsync(); }

            else await AlertAsync("Update failed");

        }

        finally { IsBusy = false; }

    }

    [RelayCommand] void CancelEdit() => IsEditing = false;

    // ── Language toggle ───────────────────────────────────────────
    [RelayCommand]
    async Task ToggleLanguage()
    {
        var next = LocalizationService.ToggleLanguage();
        var msg = LocalizationService.Get("LanguageChanged");
        await Shell.Current.DisplayAlert(
            LocalizationService.Get("Notice"), msg, LocalizationService.Get("Ok"));
        // Notify bindings
        OnPropertyChanged(nameof(LanguageLabel));
        OnPropertyChanged(nameof(CurrentLangDisplay));
    }

    [RelayCommand]
    async Task Logout()
    {
        var confirm = LocalizationService.Get("LogoutConfirm");
        if (!await Shell.Current.DisplayAlert(
            LocalizationService.Get("Logout"), confirm,
            LocalizationService.Get("Ok"), LocalizationService.Get("Cancel"))) return;
        _auth.Logout();
        Application.Current!.MainPage = new NavigationPage(_loginPage);
    }

}