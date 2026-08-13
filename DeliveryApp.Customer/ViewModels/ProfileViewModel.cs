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
    readonly CartService _cart;
    readonly LoginPage _loginPage;

    [ObservableProperty] User? _user;
    [ObservableProperty] bool _isEditing;
    [ObservableProperty] string _editName = string.Empty;
    [ObservableProperty] string _editPhone = string.Empty;
    [ObservableProperty] string _editAddress = string.Empty;

    [ObservableProperty] bool _isDeletingAccount;
    [ObservableProperty] string _deletePassword = string.Empty;

    public ProfileViewModel(ApiService api, AuthService auth, CartService cart, LoginPage loginPage)
    {
        _api = api; _auth = auth; _cart = cart; _loginPage = loginPage;
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
        _cart.Clear();
        _auth.Logout();
        Application.Current!.MainPage = new NavigationPage(_loginPage);
    }

    // ── حذف الحساب ──────────────────────────────────────────────
    // بيتطلب تأكيد أول، وبعدين كلمة السر عشان نتأكد إن اليوزر نفسه
    // اللي بيطلب الحذف. لو الـ API رجع نجاح، بنعمل نفس خطوات اللوج آوت
    // (مسح السلة + مسح التوكن + رجوع لصفحة اللوجين).
    [RelayCommand]
    async Task StartDeleteAccount()
    {
        var confirm = LocalizationService.Get("DeleteAccountConfirm");
        if (!await Shell.Current.DisplayAlert(
            LocalizationService.Get("DeleteAccount"), confirm,
            LocalizationService.Get("Ok"), LocalizationService.Get("Cancel"))) return;

        DeletePassword = string.Empty;
        IsDeletingAccount = true;
    }

    [RelayCommand]
    void CancelDeleteAccount()
    {
        IsDeletingAccount = false;
        DeletePassword = string.Empty;
    }

    [RelayCommand]
    async Task ConfirmDeleteAccount()
    {
        if (string.IsNullOrWhiteSpace(DeletePassword))
        {
            await AlertAsync(LocalizationService.Get("PasswordRequired"));
            return;
        }

        IsBusy = true;
        try
        {
            if (await _api.DeleteAccountAsync(DeletePassword))
            {
                _cart.Clear();
                _auth.Logout();
                Application.Current!.MainPage = new NavigationPage(_loginPage);
            }
            else
            {
                await AlertAsync(LocalizationService.Get("DeleteAccountFailed"));
            }
        }
        finally { IsBusy = false; }
    }
}
