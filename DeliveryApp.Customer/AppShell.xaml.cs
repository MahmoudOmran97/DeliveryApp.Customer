using DeliveryApp.Customer.Views;

namespace DeliveryApp.Customer;

public partial class AppShell : Shell

{

    public AppShell()

    {

        InitializeComponent();

        Routing.RegisterRoute(nameof(RestaurantPage), typeof(RestaurantPage));

        Routing.RegisterRoute(nameof(CartPage), typeof(CartPage));

        Routing.RegisterRoute(nameof(CheckoutPage), typeof(CheckoutPage));

        Routing.RegisterRoute(nameof(OrderTrackingPage), typeof(OrderTrackingPage));

        Routing.RegisterRoute(nameof(OrderDetailPage), typeof(OrderDetailPage));

        Routing.RegisterRoute(nameof(LoginPage), typeof(LoginPage));

        Routing.RegisterRoute(nameof(RegisterPage), typeof(RegisterPage));
        Routing.RegisterRoute(nameof(SupportChatPage), typeof(SupportChatPage));
        Routing.RegisterRoute(nameof(LocationPickerPage), typeof(LocationPickerPage));
    }

}

