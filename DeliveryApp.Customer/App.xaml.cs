using DeliveryApp.Customer.Services;
using DeliveryApp.Customer.Views;

namespace DeliveryApp.Customer;

public partial class App : Application
{
    public App(SplashPage splash, ChatNotificationService chatNotif)
    {
        InitializeComponent();
        // تهيئة الـ ChatNotificationService عشان يبدأ يستمع للرسائل من أول ما التطبيق يشتغل
        _ = chatNotif; // Singleton — just resolve it so it starts listening
        MainPage = splash;
    }
}