using DeliveryApp.Customer.ViewModels;

namespace DeliveryApp.Customer.Views;

public partial class HomePage : ContentPage

{

    readonly HomeViewModel _vm;

    public HomePage(HomeViewModel vm) { InitializeComponent(); _vm = vm; BindingContext = vm; }

    protected override void OnAppearing() { base.OnAppearing(); if (!_vm.IsBusy) _vm.LoadCommand.Execute(null); }

}

