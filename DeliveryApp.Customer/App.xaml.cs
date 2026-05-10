using DeliveryApp.Customer.Views;

namespace DeliveryApp.Customer;

public partial class App : Application
{
    public App(SplashPage splash)
    {
        InitializeComponent();
        MainPage = splash; // ✅ من DI
    }
}
