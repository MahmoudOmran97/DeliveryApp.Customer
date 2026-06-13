using DeliveryApp.Customer.ViewModels;

namespace DeliveryApp.Customer.Views;

public partial class HomePage : ContentPage
{
    readonly HomeViewModel _vm;
    IDispatcherTimer? _bannerTimer;

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
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _bannerTimer?.Stop();
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
}
