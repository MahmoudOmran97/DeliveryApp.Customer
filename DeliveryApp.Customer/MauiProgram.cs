using CommunityToolkit.Maui;
using FFImageLoading.Maui;

using DeliveryApp.Customer.Services;
using DeliveryApp.Customer.ViewModels;
using DeliveryApp.Customer.Views;

using Microsoft.Extensions.Logging;
using SkiaSharp.Views.Maui.Controls.Hosting;

namespace DeliveryApp.Customer;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        // ── Apply persisted language before anything renders ──
        LocalizationService.Apply(
            Preferences.Get(LocalizationService.LangKey, LocalizationService.English));

        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .UseSkiaSharp()              // ← كده صح بدون arguments
            .UseFFImageLoading()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("Cairo-Regular.ttf", "CairoRegular");
                fonts.AddFont("Cairo-Bold.ttf", "CairoBold");
            });

        // ── Services ─────────────────────────────
        builder.Services.AddSingleton<AuthService>();
        builder.Services.AddSingleton<CartService>();
        builder.Services.AddSingleton<ApiService>();
        builder.Services.AddSingleton<SignalRService>();
        builder.Services.AddSingleton<ChatNotificationService>();
        builder.Services.AddSingleton<Converters.LocaleStrings>();

        // ── ViewModels ────────────────────────────
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<RegisterViewModel>();
        builder.Services.AddTransient<HomeViewModel>();
        builder.Services.AddTransient<RestaurantViewModel>();
        builder.Services.AddTransient<CartViewModel>();
        builder.Services.AddTransient<CheckoutViewModel>();
        builder.Services.AddTransient<OrderTrackingViewModel>();
        builder.Services.AddTransient<OrdersViewModel>();
        builder.Services.AddTransient<OrderDetailViewModel>();
        builder.Services.AddTransient<NotificationsViewModel>();
        builder.Services.AddTransient<ProfileViewModel>();
        builder.Services.AddTransient<SupportChatViewModel>();
        builder.Services.AddTransient<DriverChatViewModel>();
        builder.Services.AddTransient<SettingsViewModel>();
        builder.Services.AddTransient<LocationPickerViewModel>();
        builder.Services.AddTransient<CouponsViewModel>();
        builder.Services.AddTransient<RewardsViewModel>();
        // ── Pages ─────────────────────────────────
        builder.Services.AddSingleton<AppShell>();
        builder.Services.AddSingleton<SplashPage>();
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<RegisterPage>();
        builder.Services.AddTransient<HomePage>();
        builder.Services.AddTransient<RestaurantPage>();
        builder.Services.AddTransient<CartPage>();
        builder.Services.AddTransient<CheckoutPage>();
        builder.Services.AddTransient<OrderTrackingPage>();
        builder.Services.AddTransient<OrdersPage>();
        builder.Services.AddTransient<OrderDetailPage>();
        builder.Services.AddTransient<NotificationsPage>();
        builder.Services.AddTransient<ProfilePage>();
        builder.Services.AddTransient<SupportChatPage>();
        builder.Services.AddTransient<DriverChatPage>();
        builder.Services.AddTransient<SettingsPage>();
        builder.Services.AddTransient<LocationPickerPage>();
        builder.Services.AddTransient<CouponsPage>();
        builder.Services.AddTransient<RewardsPage>();
#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}