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

    public ProfileViewModel(ApiService api, AuthService auth, LoginPage loginPage) { _api = api; _auth = auth; _loginPage = loginPage; }

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

    [RelayCommand]
    async Task Logout()
    {
        if (!await Shell.Current.DisplayAlert("Logout", "Are you sure?", "Yes", "Cancel")) return;
        _auth.Logout();
        // ✅ بدل new LoginPage()
        Application.Current!.MainPage = new NavigationPage(_loginPage);
    }

}

