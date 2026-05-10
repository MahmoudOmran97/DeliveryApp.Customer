using DeliveryApp.Customer.ViewModels;

namespace DeliveryApp.Customer.Views;

public partial class OrdersPage : ContentPage

{

    readonly OrdersViewModel _vm;

    public OrdersPage(OrdersViewModel vm) { InitializeComponent(); _vm = vm; BindingContext = vm; }

    protected override void OnAppearing() { base.OnAppearing(); _vm.LoadCommand.Execute(null); }

}

