// ═══════════════════════════════════════════════════════════════
// DeliveryApp.Customer / ViewModels / ProfileViewModel.cs
// ═══════════════════════════════════════════════════════════════
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

    public ProfileViewModel(ApiService api, AuthService auth, LoginPage loginPage)
    {
        _api = api; _auth = auth; _loginPage = loginPage;
    }

    [RelayCommand]
    async Task Load()
    {
        IsBusy = true;
        try
        {
            User = await _api.GetProfileAsync();

            // ✅ لو المستخدم راجع من اختيار الموقع على الخريطة وهو لسه في وضع التعديل،
            // نحدّث حقل العنوان بس من غير ما نلمس الاسم/التليفون اللي ممكن يكون عدّلهم
            // ولسه ما حفظش، عشان ميحصلش overwrite للعنوان الجديد لو ضغط Save بعدين.
            if (IsEditing) EditAddress = User?.Address ?? "";
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    void StartEdit()
    {
        if (User == null) return;
        EditName = User.FullName; EditPhone = User.Phone; EditAddress = User.Address ?? "";
        IsEditing = true;
    }

    // ── اختيار العنوان من الخريطة (نفس صفحة الهوم) ─────────────
    // الصفحة دي بتحفظ العنوان مباشرة في البروفايل عبر UpdateProfileAsync،
    // وبمجرد الرجوع، الـ OnAppearing بيستدعي Load تاني فيحدّث EditAddress تلقائيًا.
    [RelayCommand]
    static Task PickAddressFromMap() => Shell.Current.GoToAsync("HomeLocationPickerPage");

    [RelayCommand]
    async Task Save()
    {
        IsBusy = true;
        try
        {
            if (await _api.UpdateProfileAsync(EditName, EditPhone, EditAddress))
            { IsEditing = false; await Load(); }
            // ✅ ترجمة رسالة فشل التحديث
            else await AlertAsync(LocalizationService.Get("UpdateFailed"));
        }
        finally { IsBusy = false; }
    }

    [RelayCommand] void CancelEdit() => IsEditing = false;

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
