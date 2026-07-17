using DeliveryApp.Customer.ViewModels;

namespace DeliveryApp.Customer.Views;

public partial class CategoryPage : ContentPage
{
	readonly CategoryViewModel _vm;

	public CategoryPage(CategoryViewModel vm)
	{
		InitializeComponent();
		BindingContext = _vm = vm;
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();

		// الـ QueryProperties (category/search) بيتحطوا قبل ما الصفحة تظهر،
		// فنحمّل هنا عشان نضمن إن القيمتين اتحطوا الاتنين قبل النداء على الـ API
		if (_vm.LoadCommand.CanExecute(null))
			_vm.LoadCommand.Execute(null);
	}
}
