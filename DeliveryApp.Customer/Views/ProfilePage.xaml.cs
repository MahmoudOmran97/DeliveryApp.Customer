using DeliveryApp.Customer.ViewModels;

namespace DeliveryApp.Customer.Views;

public partial class ProfilePage : ContentPage

{

    readonly ProfileViewModel _vm;

    public ProfilePage(ProfileViewModel vm) { InitializeComponent(); _vm = vm; BindingContext = vm; }

    protected override void OnAppearing() { base.OnAppearing(); _vm.LoadCommand.Execute(null); }

}

