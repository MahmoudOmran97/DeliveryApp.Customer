using DeliveryApp.Customer.Services;
using DeliveryApp.Customer.Views;

namespace DeliveryApp.Customer;

public partial class App : Application
{
    public App(SplashPage splash, ChatNotificationService chatNotif, FcmTokenService fcmToken, AuthService auth)
    {
        InitializeComponent();
        _ = chatNotif;

        fcmToken.ListenForTokenRefresh();
        fcmToken.ListenForMessages();

        // Register FCM token only when user is already logged in (needs JWT for API)
        _ = Task.Run(async () =>
        {
            await Task.Delay(1500);
            if (auth.IsLoggedIn)
                await fcmToken.RegisterAsync();
        });

        MainPage = splash;
    }
}