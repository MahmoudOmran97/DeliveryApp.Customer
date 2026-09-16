namespace DeliveryApp.Customer.Models;

/// <summary>
/// عنصر واحد من شاشات الـ Onboarding (اللي بتظهر للعميل أول ما يسطّب التطبيق).
/// Kind بيحدد أنهي بلوك رسمة يتعرض جوه الـ DataTemplate بتاع الـ CarouselView
/// (Categories / Delivery / Easy) عن طريق StringEquals converter في الـ XAML.
/// </summary>
public class OnboardingSlide
{
    public string Image { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
}
