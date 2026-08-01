using DeliveryApp.Customer.ViewModels;

namespace DeliveryApp.Customer.Views;

public partial class HomePage : ContentPage
{
    readonly HomeViewModel _vm;
    IDispatcherTimer? _bannerTimer;
    CancellationTokenSource? _logoAnimCts;
    bool _suppressPositionChangedRestart;

    public HomePage(HomeViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (!_vm.IsBusy) _vm.LoadCommand.Execute(null);
        StartBannerTimer();
        StartLogoAnimation();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _bannerTimer?.Stop();
        StopLogoAnimation();
    }

    // ══════════════════════════════════════════════
    //  Banner Auto-Scroll Timer
    //  - بيغيّر CurrentBannerIndex بس (مش بيعمل ScrollTo يدوي)
    //  - الـ CarouselView + IndicatorView متبطتين على نفس الـ Property
    //    (Position TwoWay) فبيتحركوا لوحدهم تلقائي
    //  - try/catch عشان أي استثناء ميوقفش التطبيق
    // ══════════════════════════════════════════════
    void StartBannerTimer()
    {
        _bannerTimer?.Stop();
        _bannerTimer = Dispatcher.CreateTimer();
        _bannerTimer.Interval = TimeSpan.FromSeconds(3);
        _bannerTimer.Tick += (_, _) =>
        {
            try
            {
                if (_vm.Banners == null || _vm.Banners.Count <= 1) return;

                var next = (_vm.CurrentBannerIndex + 1) % _vm.Banners.Count;

                // منع الـ PositionChanged من إعادة تشغيل التايمر وهو أصلاً شغال من التايمر نفسه
                _suppressPositionChangedRestart = true;
                _vm.CurrentBannerIndex = next;
            }
            catch
            {
                // تجاهل أي استثناء عشان التايمر مايوقفش التطبيق
            }
            finally
            {
                _suppressPositionChangedRestart = false;
            }
        };
        _bannerTimer.Start();
    }

    void RestartBannerTimer()
    {
        try
        {
            _bannerTimer?.Stop();
            _bannerTimer?.Start();
        }
        catch { /* تجاهل */ }
    }

    // بيتنادى لما الـ Position يتغيّر - سواء من التايمر أو من قلب المستخدم يدوي
    void BannerCarousel_PositionChanged(object? sender, PositionChangedEventArgs e)
    {
        // لو المستخدم هو اللي قلّب يدوي (مش التايمر) نعيد ضبط العداد
        // عشان مايجيش يقلب من تحت إيده بعد نص ثانية
        if (!_suppressPositionChangedRestart)
            RestartBannerTimer();
    }

    // ══════════════════════════════════════════════
    //  Logo Animation (Header)
    // ══════════════════════════════════════════════
    void StartLogoAnimation()
    {
        StopLogoAnimation();
        _logoAnimCts = new CancellationTokenSource();
        var token = _logoAnimCts.Token;

        _ = AnimatePinLoopAsync(token);
        _ = AnimateSpeedWaveLoopAsync(token);
        _ = AnimateSpeedLineLoopAsync(token);
    }

    void StopLogoAnimation()
    {
        _logoAnimCts?.Cancel();
        _logoAnimCts?.Dispose();
        _logoAnimCts = null;
    }

    async Task AnimatePinLoopAsync(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                await LogoPin.TranslateTo(0, -10, 1100, Easing.SinInOut);
                if (token.IsCancellationRequested) break;
                await LogoPin.TranslateTo(0, 0, 1100, Easing.SinInOut);
            }
        }
        catch (ObjectDisposedException) { }
        catch (TaskCanceledException) { }
    }

    async Task AnimateSpeedWaveLoopAsync(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                await LogoSpeedWave.TranslateTo(-14, 0, 550, Easing.SinInOut);
                if (token.IsCancellationRequested) break;
                await LogoSpeedWave.TranslateTo(8, 0, 550, Easing.SinInOut);
                if (token.IsCancellationRequested) break;
                await LogoSpeedWave.TranslateTo(0, 0, 450, Easing.SinInOut);
                if (token.IsCancellationRequested) break;
                await Task.Delay(300, token);
            }
        }
        catch (ObjectDisposedException) { }
        catch (TaskCanceledException) { }
    }

    async Task AnimateSpeedLineLoopAsync(CancellationToken token)
    {
        try
        {
            await Task.Delay(150, token);
            while (!token.IsCancellationRequested)
            {
                await LogoSpeedLine.TranslateTo(-20, 0, 500, Easing.SinInOut);
                if (token.IsCancellationRequested) break;
                await LogoSpeedLine.TranslateTo(8, 0, 500, Easing.SinInOut);
                if (token.IsCancellationRequested) break;
                await LogoSpeedLine.TranslateTo(0, 0, 400, Easing.SinInOut);
                if (token.IsCancellationRequested) break;
                await Task.Delay(300, token);
            }
        }
        catch (ObjectDisposedException) { }
        catch (TaskCanceledException) { }
    }
}