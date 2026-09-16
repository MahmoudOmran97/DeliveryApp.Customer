
using DeliveryApp.Customer.Models;
using DeliveryApp.Customer.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Storage;

namespace DeliveryApp.Customer.Views;

public partial class OnboardingPage : ContentPage
{
    // ✅ نفس المفتاح المستخدم في SplashPage.xaml.cs عشان نعرف هل العميل
    // شاف شاشات الـ Onboarding قبل كده ولا دي أول مرة يفتح فيها التطبيق
    // (زي وقت التسطيب). بيتسجل مرة واحدة بس مدى عمر التطبيق.
    public const string HasSeenOnboardingKey = "has_seen_onboarding";

    readonly List<OnboardingSlide> _slides;

    public OnboardingPage()
    {
        InitializeComponent();

        FlowDirection = LocalizationService.Flow;

        _slides =
        [
            new OnboardingSlide
            {
                Image = "screenshot1.png",
                Title = LocalizationService.Get("Onboarding_Title1"),
                Subtitle = LocalizationService.Get("Onboarding_Subtitle1")
            },
            new OnboardingSlide
            {
                Image = "screenshot2.png",
                Title = LocalizationService.Get("Onboarding_Title2"),
                Subtitle = LocalizationService.Get("Onboarding_Subtitle2")
            },
            new OnboardingSlide
            {
                Image = "screenshot3.png",
                Title = LocalizationService.Get("Onboarding_Title3"),
                Subtitle = LocalizationService.Get("Onboarding_Subtitle3")
            }
        ];

        Carousel.ItemsSource = _slides;
    }

    void Carousel_PositionChanged(object? sender, PositionChangedEventArgs e)
    {
        bool isLast = e.CurrentPosition >= _slides.Count - 1;
        NextButtonLabel.Text = isLast
            ? LocalizationService.Get("Onboarding_Start")
            : LocalizationService.Get("Onboarding_Next");
    }

    void OnNextTapped(object? sender, TappedEventArgs e)
    {
        int last = _slides.Count - 1;
        if (Carousel.Position < last)
        {
            Carousel.Position++;
            return;
        }

        CompleteOnboarding();
    }

    static void CompleteOnboarding()
    {
        // ✅ يتسجل مرة واحدة بس — بعد كده SplashPage مش هيرجع يعرض
        // شاشات الـ Onboarding تاني حتى لو العميل عمل logout ودخل تاني.
        Preferences.Default.Set(HasSeenOnboardingKey, true);

        var loginPage = IPlatformApplication.Current!.Services.GetRequiredService<LoginPage>();
        Application.Current!.MainPage = new NavigationPage(loginPage);
    }
}
