using DeliveryApp.Customer.Services;
using DeliveryApp.Customer.Views;
using Plugin.Firebase.CloudMessaging;

namespace DeliveryApp.Customer;

public partial class App : Application
{
    public App(SplashPage splash, ChatNotificationService chatNotif, FcmTokenService fcmToken)
    {
        InitializeComponent();
        _ = chatNotif;

        // ✅ استدعي RegisterAsync عشان تجيب التوكن
        _ = Task.Run(async () =>
        {
            await Task.Delay(3000); // استنى 3 ثواني لحد ما Firebase يinitialize
            await fcmToken.RegisterAsync();
        });

        fcmToken.ListenForTokenRefresh();
        fcmToken.ListenForMessages();

        MainPage = splash;
    }
}