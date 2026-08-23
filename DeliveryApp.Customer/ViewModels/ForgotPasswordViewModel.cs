using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeliveryApp.Customer.Services;
using static DeliveryApp.Customer.Services.ApiService;

namespace DeliveryApp.Customer.ViewModels;

public partial class ForgotPasswordViewModel : BaseViewModel
{
    readonly ApiService _api;

    [ObservableProperty] string _email = string.Empty;
    [ObservableProperty] string _otp = string.Empty;
    [ObservableProperty] string _newPassword = string.Empty;
    [ObservableProperty] string _confirmNewPassword = string.Empty;

    // false = لسه في خطوة إدخال الإيميل، true = وصل لخطوة إدخال الكود وكلمة المرور الجديدة
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

    public ForgotPasswordViewModel(ApiService api)
    {
        _api = api;
    }

    // ─────────────────────────────────────────────────────────
    // الخطوة الأولى: بعت كود الـ OTP على الإيميل
    // ─────────────────────────────────────────────────────────
    [RelayCommand]
    async Task SendCodeAsync()
    {
        if (string.IsNullOrWhiteSpace(Email))
        { await AlertAsync(LocalizationService.Get("EnterEmailFirst")); return; }

        IsBusy = true;
        try
        {
            await _api.SendOtpAsync(Email, "ResetPassword");
            IsOtpStep = true;
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

    // ─────────────────────────────────────────────────────────
    // ✅ الجديد: إعادة إرسال الكود (نفس السيرفر call بتاع الإرسال الأول)
    // ─────────────────────────────────────────────────────────
    [RelayCommand]
    async Task ResendCodeAsync()
    {
        if (!CanResend) return;

        IsBusy = true;
        try
        {
            await _api.SendOtpAsync(Email, "ResetPassword");
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
    // الخطوة الثانية: تأكيد الكود وتغيير كلمة المرور
    // ─────────────────────────────────────────────────────────
    [RelayCommand]
    async Task ResetPasswordAsync()
    {
        if (string.IsNullOrWhiteSpace(Otp) || string.IsNullOrWhiteSpace(NewPassword))
        { await AlertAsync(LocalizationService.Get("LoginFillFields")); return; }

        if (NewPassword != ConfirmNewPassword)
        { await AlertAsync(LocalizationService.Get("PasswordsNotMatch")); return; }

        IsBusy = true;
        try
        {
            await _api.ResetPasswordAsync(Email, Otp, NewPassword);
            await AlertAsync(LocalizationService.Get("PasswordResetSuccess"));
            await Application.Current!.MainPage!.Navigation.PopAsync();
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

    [RelayCommand]
    void EditEmail()
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