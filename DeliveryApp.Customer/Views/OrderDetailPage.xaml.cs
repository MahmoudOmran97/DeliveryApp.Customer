using DeliveryApp.Customer.ViewModels;

namespace DeliveryApp.Customer.Views;

public partial class OrderDetailPage : ContentPage

{

    public OrderDetailPage(OrderDetailViewModel vm) { InitializeComponent(); BindingContext = vm; }

}

