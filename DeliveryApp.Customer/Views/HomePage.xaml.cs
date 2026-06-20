using DeliveryApp.Customer.ViewModels;

namespace DeliveryApp.Customer.Views;

public partial class HomePage : ContentPage
{
    readonly HomeViewModel _vm;
    IDispatcherTimer? _bannerTimer;
    CancellationTokenSource? _logoAnimCts;

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

    void StartBannerTimer()
    {
        _bannerTimer?.Stop();
        _bannerTimer = Dispatcher.CreateTimer();
        _bannerTimer.Interval = TimeSpan.FromSeconds(3);
        _bannerTimer.Tick += (_, _) =>
        {
            // guard: only scroll if we have banners
            if (_vm.Banners == null || _vm.Banners.Count <= 1) return;

            var next = (_vm.CurrentBannerIndex + 1) % _vm.Banners.Count;
            _vm.CurrentBannerIndex = next;

            try { BannerCarousel.ScrollTo(next, animate: true); }
            catch { /* ignore if carousel not ready */ }
        };
        _bannerTimer.Start();
    }

    // ══════════════════════════════════════════════
    //  Logo Animation (Header)
    //  - LogoPin: يطفو لأعلى وأسفل (Float)
    //  - LogoSpeedWave + LogoSpeedLine: يتحركوا يمين/شمال بإحساس سرعة
    //  - LogoBase: ثابت تمامًا (مرجع بصري للوجو)
    // ══════════════════════════════════════════════
    void StartLogoAnimation()
    {
        StopLogoAnimation(); // تأمين عدم تكرار اللووب لو الصفحة ظهرت أكتر من مرة
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
        catch (ObjectDisposedException) { /* الصفحة راحت قبل ما اللووب يخلص */ }
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
            // تأخير بسيط في البداية عن LogoSpeedWave يعطي إحساس عمق (الخطوط مش بتتحرك مع بعض بنفس التوقيت بالظبط)
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