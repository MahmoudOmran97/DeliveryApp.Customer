using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace DeliveryApp.Customer.ViewModels;

public partial class BaseViewModel : ObservableObject

{

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    private bool _isBusy;

    partial void OnIsBusyChanged(bool value)
    {
        Services.LoadingService.Instance.SetBusy(value);
    }

    [ObservableProperty]
    private string _title = string.Empty;

    public bool IsNotBusy => !IsBusy;

    // ── Back navigation (works inside Shell tabs and pushed pages) ──
    [RelayCommand]
    protected static async Task GoBackAsync()
    {
        try
        {
            await Shell.Current.GoToAsync("..");
        }
        catch
        {
            // Fallback for NavigationPage stack
            if (Shell.Current?.Navigation?.NavigationStack?.Count > 1)
                await Shell.Current.Navigation.PopAsync();
        }
    }

    protected static async Task AlertAsync(string msg)
    {
        var page = Shell.Current as Page
                   ?? Application.Current!.MainPage!;

        await page.DisplayAlert(
            Services.LocalizationService.Get("Notice"), msg,
            Services.LocalizationService.Get("Ok"));
    }

    // ── Countdown helper for "resend code" buttons (OTP screens) ──
    // بيعد ثانية بثانية من seconds لحد 0 وبيستدعي onTick بعد كل خطوة
    // (تحديث الـ UI)، وبيستدعي onFinished لما يوصل للصفر (يفتح زرار الإرسال تاني).
    IDispatcherTimer? _countdownTimer;

    protected void StartCountdown(int seconds, Action<int> onTick, Action onFinished)
    {
        _countdownTimer?.Stop();

        var remaining = seconds;
        onTick(remaining);

        var dispatcher = Application.Current?.Dispatcher ?? Dispatcher.GetForCurrentThread()!;
        _countdownTimer = dispatcher.CreateTimer();
        _countdownTimer.Interval = TimeSpan.FromSeconds(1);
        _countdownTimer.Tick += (_, _) =>
        {
            remaining--;
            if (remaining <= 0)
            {
                _countdownTimer?.Stop();
                onFinished();
            }
            else
            {
                onTick(remaining);
            }
        };
        _countdownTimer.Start();
    }
}