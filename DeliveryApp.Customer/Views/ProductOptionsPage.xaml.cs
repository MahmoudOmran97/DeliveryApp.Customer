using DeliveryApp.Customer.ViewModels;

namespace DeliveryApp.Customer.Views;

public partial class ProductOptionsPage : ContentPage
{
    public ProductOptionsPage(ProductOptionsViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;

        // الشيت ميعدّيش 88% من ارتفاع الشاشة؛ المحتوى جواه (مقاسات + اختيارات) بيسكرول
        var info = DeviceDisplay.Current.MainDisplayInfo;
        Sheet.MaximumHeightRequest = info.Height / info.Density * 0.88;
    }
}
