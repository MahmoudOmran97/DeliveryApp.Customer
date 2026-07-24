using DeliveryApp.Customer.Converters;
using DeliveryApp.Customer.Services;
using DeliveryApp.Customer.Views;

namespace DeliveryApp.Customer;

public partial class AppShell : Shell
{
    readonly LocaleStrings _locale;

    public AppShell(LocaleStrings locale)
    {
        _locale = locale;
        BindingContext = locale;          // ← tabs bind to LocaleStrings properties

        InitializeComponent();

        // ✅ يحدد اتجاه كل صفحات التطبيق (تابات + الصفحات المفتوحة عن طريقها)
        // حسب اللغة الحالية. الصفحات مش بتحدد FlowDirection بتاعها بنفسها بعد
        // كده، فبتورث القيمة دي من هنا تلقائي.
        FlowDirection = LocalizationService.Flow;

        Shell.SetTabBarIsVisible(this, false);
        Navigated += OnShellNavigated;

        Routing.RegisterRoute(nameof(RestaurantPage), typeof(RestaurantPage));
        Routing.RegisterRoute(nameof(ProductOptionsPage), typeof(ProductOptionsPage));
        Routing.RegisterRoute(nameof(CartPage), typeof(CartPage));
        Routing.RegisterRoute(nameof(CheckoutPage), typeof(CheckoutPage));
        Routing.RegisterRoute(nameof(OrderTrackingPage), typeof(OrderTrackingPage));
        Routing.RegisterRoute(nameof(OrderDetailPage), typeof(OrderDetailPage));
        Routing.RegisterRoute(nameof(LoginPage), typeof(LoginPage));
        Routing.RegisterRoute(nameof(RegisterPage), typeof(RegisterPage));
        Routing.RegisterRoute(nameof(SupportChatPage), typeof(SupportChatPage));
        Routing.RegisterRoute(nameof(DriverChatPage), typeof(DriverChatPage));
        Routing.RegisterRoute(nameof(LocationPickerPage), typeof(LocationPickerPage));
        Routing.RegisterRoute("HomeLocationPickerPage", typeof(HomeLocationPickerPage));
        Routing.RegisterRoute(nameof(CouponsPage), typeof(CouponsPage));
        Routing.RegisterRoute(nameof(RewardsPage), typeof(RewardsPage));
        Routing.RegisterRoute(nameof(PointsPage), typeof(PointsPage));
        Routing.RegisterRoute(nameof(CategoryPage), typeof(CategoryPage));
        Routing.RegisterRoute(nameof(CallPage), typeof(CallPage));
    }

    void OnShellNavigated(object? sender, ShellNavigatedEventArgs e)
    {
        Shell.SetTabBarIsVisible(this, false);
        if (CurrentPage is Page page)
            Shell.SetTabBarIsVisible(page, false);
    }

    // Call this after changing language so tab titles refresh
    public void RefreshTabTitles() => _locale.Refresh();
}