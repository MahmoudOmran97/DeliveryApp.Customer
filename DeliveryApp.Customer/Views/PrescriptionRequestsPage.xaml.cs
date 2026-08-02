using DeliveryApp.Customer.ViewModels;

namespace DeliveryApp.Customer.Views;

public partial class PrescriptionRequestsPage : ContentPage
{
    readonly PrescriptionRequestsViewModel _vm;

    public PrescriptionRequestsPage(PrescriptionRequestsViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = _vm.LoadCommand.ExecuteAsync(null);
    }
}
