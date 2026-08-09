// ═══════════════════════════════════════════════════════════════
// DeliveryApp.Customer / Views / StoreCategoryProductsPage.xaml.cs
// ═══════════════════════════════════════════════════════════════
using DeliveryApp.Customer.ViewModels;

namespace DeliveryApp.Customer.Views;

public partial class StoreCategoryProductsPage : ContentPage
{
    public StoreCategoryProductsPage(StoreCategoryProductsViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
