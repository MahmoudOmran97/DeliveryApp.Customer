using DeliveryApp.Customer.ViewModels;

namespace DeliveryApp.Customer.Views;

public partial class RestaurantPage : ContentPage

{

    public RestaurantPage(RestaurantViewModel vm) { InitializeComponent(); BindingContext = vm; }

}

