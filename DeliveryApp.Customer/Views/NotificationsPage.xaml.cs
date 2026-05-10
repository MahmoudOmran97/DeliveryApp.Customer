using DeliveryApp.Customer.ViewModels;

namespace DeliveryApp.Customer.Views;

public partial class NotificationsPage : ContentPage

{

    readonly NotificationsViewModel _vm;

    public NotificationsPage(NotificationsViewModel vm) { InitializeComponent(); _vm = vm; BindingContext = vm; }

    protected override void OnAppearing() { base.OnAppearing(); _vm.LoadCommand.Execute(null); }

}

