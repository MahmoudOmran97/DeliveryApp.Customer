namespace DeliveryApp.Customer;

public static class FirebaseExtensions
{
    public static MauiAppBuilder UseFirebase(this MauiAppBuilder builder)
    {
#if ANDROID || IOS || MACCATALYST
        builder.Services.AddSingleton(
            Plugin.Firebase.CloudMessaging.CrossFirebaseCloudMessaging.Current);
#endif
        return builder;
    }
}