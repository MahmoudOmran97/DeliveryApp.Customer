using System.ComponentModel;
using System.Runtime.CompilerServices;
using DeliveryApp.Customer.Services;

namespace DeliveryApp.Customer.Converters;

/// <summary>
/// XAML markup extension – use in XAML like:
///   Text="{loc:Loc Key=MyCart}"
/// </summary>
[ContentProperty(nameof(Key))]
public class LocExtension : IMarkupExtension<string>
{
    public string Key { get; set; } = string.Empty;

    public string ProvideValue(IServiceProvider serviceProvider)
        => LocalizationService.Get(Key);

    object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider)
        => ProvideValue(serviceProvider);
}

/// <summary>
/// Observable wrapper – lets ViewModels expose localizable strings that update
/// when the language changes. Use as a static resource or DI singleton.
/// </summary>
public class LocaleStrings : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    // ── Convenience indexer ───────────────────────────────────
    public string this[string key] => LocalizationService.Get(key);

    // ── Call this after SetLanguage() to notify all bindings ──
    public void Refresh() =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));

    // ── Tab properties ────────────────────────────────────────
    public string Tab_Home     => LocalizationService.Get(nameof(Tab_Home));
    public string Tab_Orders   => LocalizationService.Get(nameof(Tab_Orders));
    public string Tab_Alerts   => LocalizationService.Get(nameof(Tab_Alerts));
    public string Tab_Profile  => LocalizationService.Get(nameof(Tab_Profile));
    public string Tab_Settings => LocalizationService.Get(nameof(Tab_Settings));
    public string MyCart => LocalizationService.Get(nameof(MyCart));
    public string MyOrders => LocalizationService.Get(nameof(MyOrders));
    public string Notifications => LocalizationService.Get(nameof(Notifications));
    public string Profile => LocalizationService.Get(nameof(Profile));
    public string Login => LocalizationService.Get(nameof(Login));
    public string Register => LocalizationService.Get(nameof(Register));
    public string Logout => LocalizationService.Get(nameof(Logout));
    public string ChangeLanguage => LocalizationService.Get(nameof(ChangeLanguage));
    public FlowDirection Flow => LocalizationService.Flow;
}