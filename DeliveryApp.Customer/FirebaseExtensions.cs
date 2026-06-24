using Plugin.Firebase.CloudMessaging;

namespace DeliveryApp.Customer;

/// <summary>
/// Extension لتسجيل Firebase في المؤيد .UseMauiApp()
/// Plugin.Firebase.Core بتعمل الـ init أوتوماتيك عند أول استخدام
/// — مش محتاج كود إضافي في الـ Android/iOS
/// </summary>
public static class FirebaseExtensions
{
    public static MauiAppBuilder UseFirebase(this MauiAppBuilder builder)
    {
        // Plugin.Firebase بتعمل auto-init من google-services.json / GoogleService-Info.plist
        // — فقط نسجّل الـ service في الـ DI
        builder.Services.AddSingleton(CrossFirebaseCloudMessaging.Current);
        return builder;
    }
}
