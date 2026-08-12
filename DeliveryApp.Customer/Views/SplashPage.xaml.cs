using System.Globalization;
using DeliveryApp.Customer.Services;

namespace DeliveryApp.Customer.Views;

public partial class SplashPage : ContentPage
{
    readonly AuthService _auth;
    readonly FcmTokenService _fcm;
    readonly ApiService _api;
    readonly CartService _cart;

    public SplashPage(AuthService auth, FcmTokenService fcm, ApiService api, CartService cart)
    {
        InitializeComponent();

        _auth = auth;
        _fcm = fcm;
        _api = api;
        _cart = cart;

        FlowDirection = LocalizationService.Flow;

        string lang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

        imgLogo.Source = lang == "ar"
            ? "logo_ar.png"
            : "logo_en.png";
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await Task.Delay(2000);

        if (_auth.IsLoggedIn)
        {
            await _fcm.RegisterAsync();
            var shell = IPlatformApplication.Current!.Services.GetService<AppShell>()!;
            Application.Current!.MainPage = shell;

            // استعادة آخر محادثة روشتة نشطة بعد إعادة تشغيل التطبيق.
            // الحالة الأساسية محفوظة محلياً في CartService، ونستعين بالـ API
            // كشبكة أمان لو تم إغلاق التطبيق قبل اكتمال الحفظ المحلي.
            try
            {
                var requests = await _api.GetMyPrescriptionRequestsAsync();
                var activeRequest = requests?
                    .Where(r => r.Status is "Pending" or "Priced" or "Confirmed")
                    .OrderByDescending(r => r.CreatedAt)
                    .FirstOrDefault();

                if (activeRequest != null)
                {
                    if (!_cart.PrescriptionRequestId.HasValue && !string.IsNullOrWhiteSpace(activeRequest.ImageUrl))
                    {
                        _cart.SetPrescription(
                            activeRequest.RestaurantId,
                            activeRequest.ImageUrl,
                            activeRequest.Notes);
                        _cart.SetPrescriptionRequestId(activeRequest.Id);
                    }

                    await Task.Delay(250);
                    await shell.GoToAsync($"{nameof(PrescriptionChatPage)}?requestId={activeRequest.Id}");
                }
            }
            catch
            {
                // فشل الاستعادة لا يمنع المستخدم من دخول التطبيق؛ يمكنه المتابعة بشكل طبيعي.
            }
        }
        else
        {
            var loginPage = IPlatformApplication.Current!.Services.GetService<LoginPage>()!;
            Application.Current!.MainPage = new NavigationPage(loginPage);
        }
    }
}