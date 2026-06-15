using DeliveryApp.Customer.ViewModels;

namespace DeliveryApp.Customer.Views;

public partial class CategoryPage : ContentPage
{
	public CategoryPage(CategoryViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}
