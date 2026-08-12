using DeliveryApp.Customer.ViewModels;

namespace DeliveryApp.Customer.Views;

public partial class AboutPage : ContentPage
{
	public AboutPage(AboutViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}
