using DeliveryApp.Customer.ViewModels;
using System.Globalization;

namespace DeliveryApp.Customer.Views;

public partial class LoginPage : ContentPage
{
    public LoginPage(LoginViewModel vm)
    {
        InitializeComponent();

        // ✅ الصفحة دي بتتحط كـ root (جوه NavigationPage) قبل ما اليوزر يعمل
        // Login، فلازم هي كمان تحدد اتجاهها بنفسها بدل ما تستنى AppShell.
        FlowDirection = DeliveryApp.Customer.Services.LocalizationService.Flow;

        string lang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

        imgLogo.Source = lang == "ar"
            ? "logo_ar.png"
            : "logo_en.png";
        BindingContext = vm;
    }
}