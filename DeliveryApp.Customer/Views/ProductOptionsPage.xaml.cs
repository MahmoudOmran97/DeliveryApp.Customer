using DeliveryApp.Customer.ViewModels;

namespace DeliveryApp.Customer.Views;

public partial class ProductOptionsPage : ContentPage
{
    public ProductOptionsPage(ProductOptionsViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
