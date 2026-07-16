using CommunityToolkit.Maui;
using FFImageLoading.Maui;
using Microsoft.Maui.LifecycleEvents;
using Plugin.Firebase.CloudMessaging;

#if ANDROID
using Plugin.Firebase.Core.Platforms.Android;
#endif

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
        LocalizationService.Apply(
            Preferences.Get(LocalizationService.LangKey, LocalizationService.English));

        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .UseSkiaSharp()
            .UseFFImageLoading()
            .RegisterFirebaseServices()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("Cairo-Regular.ttf", "CairoRegular");
                fonts.AddFont("Cairo-Bold.ttf", "CairoBold");
            });

        // ── Services ─────────────────────────────
        builder.Services.AddSingleton<AuthService>();
        builder.Services.AddSingleton<LocationService>();
        builder.Services.AddSingleton<CartService>();
        builder.Services.AddSingleton<ApiService>();
        builder.Services.AddSingleton<SignalRService>();
        builder.Services.AddSingleton<ChatNotificationService>();
        builder.Services.AddSingleton<FcmTokenService>();
        builder.Services.AddSingleton<Converters.LocaleStrings>();

        // ── ViewModels ────────────────────────────
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<RegisterViewModel>();
        builder.Services.AddTransient<ForgotPasswordViewModel>();
        builder.Services.AddTransient<HomeViewModel>();
        builder.Services.AddTransient<RestaurantViewModel>();
        builder.Services.AddTransient<ProductOptionsViewModel>();
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
        builder.Services.AddTransient<HomeLocationPickerViewModel>();
        builder.Services.AddTransient<CouponsViewModel>();
        builder.Services.AddTransient<RewardsViewModel>();
        builder.Services.AddTransient<PointsViewModel>();
        builder.Services.AddTransient<CategoryViewModel>();
        builder.Services.AddTransient<CallViewModel>();
        builder.Services.AddTransient<CallAudioService>();
#if ANDROID
        builder.Services.AddSingleton<DeliveryApp.Customer.Services.Call.IPlatformAudioIO, DeliveryApp.Customer.Platforms.Android.AndroidAudioIO>();
#elif IOS
        builder.Services.AddSingleton<DeliveryApp.Customer.Services.Call.IPlatformAudioIO, DeliveryApp.Customer.Platforms.iOS.IosAudioIO>();
#endif

        // ── Pages ─────────────────────────────────
        // ✅ FIX: كانوا Singleton — لما بنعمل RestartApp() (مثلاً بعد تغيير اللغة)
        // كنا بناخد نفس الـ Shell/Splash القديمة اللي أصلاً اتفصلت عن الـ Window
        // (الـ native handlers بتاعتها اتلغت)، وربطها تاني كـ MainPage كان بيرمي
        // NullReferenceException جوا محرك الـ Shell. لازم Instance جديدة كل مرة.
        builder.Services.AddTransient<AppShell>();
        builder.Services.AddTransient<SplashPage>();
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<RegisterPage>();
        builder.Services.AddTransient<ForgotPasswordPage>();
        builder.Services.AddTransient<HomePage>();
        builder.Services.AddTransient<RestaurantPage>();
        builder.Services.AddTransient<ProductOptionsPage>();
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
        builder.Services.AddTransient<HomeLocationPickerPage>();
        builder.Services.AddTransient<CouponsPage>();
        builder.Services.AddTransient<RewardsPage>();
        builder.Services.AddTransient<PointsPage>();
        builder.Services.AddTransient<CategoryPage>();
        builder.Services.AddTransient<CallPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }

    private static MauiAppBuilder RegisterFirebaseServices(this MauiAppBuilder builder)
    {
        builder.ConfigureLifecycleEvents(events =>
        {
#if ANDROID
            events.AddAndroid(android =>
                android.OnCreate((activity, _) =>
                    CrossFirebase.Initialize(activity, () => Platform.CurrentActivity)));
#endif
        });

        // تسجيل FCM service في DI
        builder.Services.AddSingleton(CrossFirebaseCloudMessaging.Current);

        return builder;
    }
}