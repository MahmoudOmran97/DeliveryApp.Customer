// ═══════════════════════════════════════════════════════════════
// DeliveryApp.Customer / ViewModels / RegisterViewModel.cs
// ═══════════════════════════════════════════════════════════════
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeliveryApp.Customer.Services;
using static DeliveryApp.Customer.Services.ApiService;

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

    // ✅ الجديد: إظهار/إخفاء كلمة السر (علامة العين) - لحقلي Password و Confirm Password
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPasswordHidden))]
    [NotifyPropertyChangedFor(nameof(PasswordEyeIcon))]
    bool _isPasswordVisible;

    public bool IsPasswordHidden => !IsPasswordVisible;

    public string PasswordEyeIcon => IsPasswordVisible ? "icon_eye_off.png" : "icon_eye.png";

    [RelayCommand]
    void TogglePasswordVisibility() => IsPasswordVisible = !IsPasswordVisible;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsConfirmPasswordHidden))]
    [NotifyPropertyChangedFor(nameof(ConfirmPasswordEyeIcon))]
    bool _isConfirmPasswordVisible;

    public bool IsConfirmPasswordHidden => !IsConfirmPasswordVisible;

    public string ConfirmPasswordEyeIcon => IsConfirmPasswordVisible ? "icon_eye_off.png" : "icon_eye.png";

    [RelayCommand]
    void ToggleConfirmPasswordVisibility() => IsConfirmPasswordVisible = !IsConfirmPasswordVisible;

    // ✅ الجديد: خطوة كود التحقق (OTP)
    [ObservableProperty] string _otp = string.Empty;

    // لسه في خطوة إدخال البيانات (false) ولا وصل لخطوة إدخال الكود (true)
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotOtpStep))]
    bool _isOtpStep;

    public bool IsNotOtpStep => !IsOtpStep;

    // ✅ الجديد: زرار "إعادة إرسال الكود" — متعطّل لمدة 60 ثانية بعد كل إرسال
    const int ResendCooldownSeconds = 60;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ResendLabel))]
    bool _canResend;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ResendLabel))]
    int _resendSecondsLeft;

    public string ResendLabel => CanResend
        ? LocalizationService.Get("ResendCode")
        : string.Format(LocalizationService.Get("ResendCodeIn"), ResendSecondsLeft);

    public RegisterViewModel(ApiService api, AuthService auth)
    { _api = api; _auth = auth; }

    // ─────────────────────────────────────────────────────────
    // ✅ الجديد: الخطوة الأولى — تحقق من البيانات وابعت كود OTP على الإيميل
    // ─────────────────────────────────────────────────────────
    [RelayCommand]
    async Task SendOtpAsync()
    {
        if (string.IsNullOrWhiteSpace(FullName) || string.IsNullOrWhiteSpace(Email) ||
            string.IsNullOrWhiteSpace(Phone) || string.IsNullOrWhiteSpace(Password))
        { await AlertAsync(LocalizationService.Get("LoginFillFields")); return; }

        if (Password != ConfirmPassword)
        { await AlertAsync(LocalizationService.Get("PasswordsNotMatch")); return; }

        IsBusy = true;
        try
        {
            await _api.SendOtpAsync(Email, "Register");
            IsOtpStep = true;
            StartResendCountdown();
            await AlertAsync(LocalizationService.Get("OtpSent"));
        }
        catch (ApiException ex)
        {
            // ← رسالة الخطأ الحقيقية من السيرفر (مثلاً: الإيميل مسجل بالفعل)
            await AlertAsync(ex.Message);
        }
        catch (Exception)
        {
            await AlertAsync(LocalizationService.Get("UnexpectedError"));
        }
        finally { IsBusy = false; }
    }

    // ─────────────────────────────────────────────────────────
    // ✅ الجديد: إعادة إرسال الكود (نفس السيرفر call بتاع الإرسال الأول)
    // ─────────────────────────────────────────────────────────
    [RelayCommand]
    async Task ResendOtpAsync()
    {
        if (!CanResend) return;

        IsBusy = true;
        try
        {
            await _api.SendOtpAsync(Email, "Register");
            Otp = string.Empty;
            StartResendCountdown();
            await AlertAsync(LocalizationService.Get("OtpSent"));
        }
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

    void StartResendCountdown()
    {
        CanResend = false;
        StartCountdown(ResendCooldownSeconds,
            onTick: remaining => ResendSecondsLeft = remaining,
            onFinished: () => CanResend = true);
    }

    // ─────────────────────────────────────────────────────────
    // ✅ الجديد: الخطوة الثانية — تأكيد الكود وإنشاء الحساب فعليًا
    // ─────────────────────────────────────────────────────────
    [RelayCommand]
    async Task RegisterAsync()
    {
        if (string.IsNullOrWhiteSpace(Otp))
        { await AlertAsync(LocalizationService.Get("EnterOtpCode")); return; }

        IsBusy = true;
        try
        {
            var r = await _api.RegisterAsync(FullName, Email, Password, Phone, Otp);
            if (r != null)
            {
                await _auth.SaveUserAsync(r.Token, r.Id, r.FullName, r.Email, r.Role);
                var shell = IPlatformApplication.Current!.Services.GetService<AppShell>()!;
                Application.Current!.MainPage = shell;
            }
            else await AlertAsync(LocalizationService.Get("RegisterFailed"));
        }
        // ✅ الفيكس: كان في كراش (Unhandled Exception) هنا لما الـ API يرجع خطأ
        // (زي كود OTP غلط أو الإيميل مسجل بالفعل) لأن الاستثناء ما كانش بيتمسك.
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

    // ✅ الجديد: رجوع لتعديل البيانات وإعادة إرسال كود جديد لو احتاج
    [RelayCommand]
    void EditDetails()
    {
        IsOtpStep = false;
        Otp = string.Empty;
    }

    [RelayCommand]
    async Task GoBack()
    {
        await Application.Current!.MainPage!.Navigation.PopAsync();
    }
}