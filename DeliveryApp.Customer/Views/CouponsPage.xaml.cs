using DeliveryApp.Customer.ViewModels;

namespace DeliveryApp.Customer.Views;

public partial class CouponsPage : ContentPage
{
    readonly CouponsViewModel _vm;

    public CouponsPage(CouponsViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (!_vm.IsBusy) _vm.LoadCommand.Execute(null);
    }

    private async void OnBackTapped(object sender, TappedEventArgs e)
        => await Shell.Current.GoToAsync("..");
}
