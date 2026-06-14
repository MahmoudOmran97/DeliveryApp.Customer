using DeliveryApp.Customer.ViewModels;

namespace DeliveryApp.Customer.Views;

public partial class PointsPage : ContentPage
{
	public PointsPage(PointsViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is PointsViewModel vm)
        {
            await vm.LoadCommand.ExecuteAsync(null);
        }
    }
}
