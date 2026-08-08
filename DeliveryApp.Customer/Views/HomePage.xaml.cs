using DeliveryApp.Customer.ViewModels;

namespace DeliveryApp.Customer.Views;

public partial class HomePage : ContentPage
{
    readonly HomeViewModel _vm;
    IDispatcherTimer? _bannerTimer;
    CancellationTokenSource? _logoAnimCts;
    // ✅ FIX: كان فيه bool flag بيترجع false في finally على طول (sync)، لكن
    // CarouselView بيطلق PositionChanged بعد ما الأنيميشن يخلص (async)، فالـ flag
    // كان يبقى false قبل ما الـ event يوصل أصلاً → RestartBannerTimer() بيتنادى
    // غلط ويعمل تعارض/تعليق. دلوقتي بنقارن بالـ index الفعلي بدل التوقيت.
    int? _programmaticBannerIndex;

    public HomePage(HomeViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        // ✅ FIX: قبل كده كان بيعيد تحميل كل حاجة (Clear + إعادة API call) في كل
        // مرة ترجع للصفحة، وده كان بيسبب "قفشة"/تعليق محسوس خصوصًا مع الكاروسيل.
        // أول مرة بس بنعمل تحميل عادي (بيبين الـ Spinner)، وبعد كده بنعمل تحديث
        // هادئ في الخلفية من غير ما نلمس المحتوى الظاهر أو نوقف الكاروسيل.
        if (_vm.Restaurants.Count == 0 && !_vm.IsBusy)
            _vm.LoadCommand.Execute(null);
        else
            _ = _vm.RefreshSilentlyAsync();

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
                _programmaticBannerIndex = next;
                _vm.CurrentBannerIndex = next;
            }
            catch
            {
                // تجاهل أي استثناء عشان التايمر مايوقفش التطبيق
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
        // ✅ FIX: بنقارن بالـ index اللي التايمر نفسه ضبطه بدل flag بتوقيت غلط.
        // لو ده نفس التغيير اللي التايمر عمله، منعملش Restart. أي تغيير تاني
        // (سحب المستخدم يدوي) بيعمل Restart عادي.
        if (_programmaticBannerIndex.HasValue && _programmaticBannerIndex.Value == e.CurrentPosition)
        {
            _programmaticBannerIndex = null;
            return;
        }
        _programmaticBannerIndex = null;
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