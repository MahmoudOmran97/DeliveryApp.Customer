using DeliveryApp.Customer.Services;
using DeliveryApp.Customer.Views;
using Plugin.Firebase.CloudMessaging;

namespace DeliveryApp.Customer;

public partial class App : Application
{
    public App(SplashPage splash, ChatNotificationService chatNotif, FcmTokenService fcmToken)
    {
        InitializeComponent();
        _ = chatNotif; // Singleton — start listening for chat messages

        // استمع لتحديثات الـ token والرسائل الجاية
        fcmToken.ListenForTokenRefresh();
        fcmToken.ListenForMessages();

        MainPage = splash;
    }
}
