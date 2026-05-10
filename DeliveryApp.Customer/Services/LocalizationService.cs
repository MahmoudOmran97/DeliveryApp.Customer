using System.Globalization;
using System.Resources;

namespace DeliveryApp.Customer.Services;

/// <summary>
/// Provides runtime Arabic / English localization.
/// Call SetLanguage("ar") or SetLanguage("en") then restart the app (or call RefreshUI).
/// </summary>
public static class LocalizationService
{
    // ── Constants ────────────────────────────────────────────
    public const string LangKey = "app_language";
    public const string Arabic = "ar";
    public const string English = "en";

    // ── ResourceManager pointing at our .resx files ──────────
    //    Namespace: DeliveryApp.Customer  |  BaseName: AppResources
    private static readonly ResourceManager _rm =
        new("DeliveryApp.Customer.Resources.Strings.AppResources",
            typeof(LocalizationService).Assembly);

    // ── Current culture ───────────────────────────────────────
    public static CultureInfo Current { get; private set; } = GetSavedCulture();

    // ── RTL helper ────────────────────────────────────────────
    public static bool IsRtl => Current.TextInfo.IsRightToLeft;

    // ── FlowDirection for XAML ────────────────────────────────
    public static FlowDirection Flow =>
        IsRtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

    // ── Get a string by key ───────────────────────────────────
    public static string Get(string key)
    {
        try { return _rm.GetString(key, Current) ?? key; }
        catch { return key; }
    }

    // ── Persist and apply a new language ─────────────────────
    public static void SetLanguage(string langCode)
    {
        Preferences.Set(LangKey, langCode);
        Apply(langCode);
    }

    // ── Apply without persisting (call on startup) ────────────
    public static void Apply(string langCode)
    {
        Current = new CultureInfo(langCode);
        CultureInfo.DefaultThreadCurrentCulture = Current;
        CultureInfo.DefaultThreadCurrentUICulture = Current;
        Thread.CurrentThread.CurrentCulture = Current;
        Thread.CurrentThread.CurrentUICulture = Current;
    }

    // ── Toggle between ar ↔ en ────────────────────────────────
    public static string ToggleLanguage()
    {
        var next = Current.TwoLetterISOLanguageName == Arabic ? English : Arabic;
        SetLanguage(next);
        return next;
    }

    // ── Read persisted preference (default: English) ──────────
    private static CultureInfo GetSavedCulture()
    {
        var saved = Preferences.Get(LangKey, English);
        return new CultureInfo(saved);
    }
}