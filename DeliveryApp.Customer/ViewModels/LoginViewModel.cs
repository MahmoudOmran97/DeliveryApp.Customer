using CommunityToolkit.Mvvm.ComponentModel;

using CommunityToolkit.Mvvm.Input;

using DeliveryApp.Customer.Services;
using DeliveryApp.Customer.Views;
using static DeliveryApp.Customer.Services.ApiService;

namespace DeliveryApp.Customer.ViewModels;

public partial class LoginViewModel : BaseViewModel

{

    readonly ApiService _api;

    readonly AuthService _auth;

    readonly FcmTokenService _fcm;

    [ObservableProperty] string _email = string.Empty;

    [ObservableProperty] string _password = string.Empty;

    // ── Language toggle (top of Login page) ────────────────────
    // Shows the *other* language's name, since tapping switches to it.
    public string OtherLanguageLabel =>
        LocalizationService.Current.TwoLetterISOLanguageName == LocalizationService.Arabic
            ? "English"
            : "العربية";

    public LoginViewModel(ApiService api, AuthService auth, FcmTokenService fcm)

    { _api = api; _auth = auth; _fcm = fcm; }

    [RelayCommand]
    void ToggleLanguage()
    {
        // {loc:Loc} markup extension resolves the string once at page-build time,
        // it doesn't auto-refresh on language change — so we rebuild the Login page
        // after switching, same approach SettingsViewModel uses for the rest of the app.
        LocalizationService.ToggleLanguage();

        var loginPage = IPlatformApplication.Current!.Services.GetService<LoginPage>()!;
        Application.Current!.MainPage = new NavigationPage(loginPage);
    }

    [RelayCommand]

    async Task LoginAsync()

    {

        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))

        { await AlertAsync(LocalizationService.Get("LoginFillFields")); return; }

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

            else await AlertAsync(LocalizationService.Get("InvalidCredentials"));

        }

        // ✅ الفيكس: ده كان سبب الجليتش/الكراش لما اليوزر يدخل إيميل أو باسورد غلط.
        // الـ API بيرجع 401 مع رسالة، وPostAsync كان بيرمي ApiException، لكن هنا
        // ما كانش في catch ليها، فالاستثناء كان بيطلع Unhandled ويوقف الشاشة.
        catch (ApiException ex)
        {
            await AlertAsync(ex.Message);
        }
        catch (Exception)
        {
            await AlertAsync(LocalizationService.Get("UnexpectedError"));
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

    // ✅ الجديد: نسيت كلمة المرور
    [RelayCommand]
    async Task GoToForgotPassword()
    {
        await Application.Current!.MainPage!.Navigation.PushAsync(
            IPlatformApplication.Current!.Services.GetService<ForgotPasswordPage>()!);
    }

}