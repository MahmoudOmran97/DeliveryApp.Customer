using CommunityToolkit.Mvvm.Input;

namespace DeliveryApp.Customer.ViewModels;

public partial class AboutViewModel : BaseViewModel
{
    [RelayCommand]
    private async Task OpenSocialLink(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return;
        try
        {
            await Launcher.Default.OpenAsync(url);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to open social link: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task OpenLegal(string type)
    {
        // Placeholder for legal pages or external links
        string url = type switch
        {
            "privacy" => "https://tawseela-app.com/privacy",
            "terms" => "https://tawseela-app.com/terms",
            _ => "https://tawseela-app.com"
        };
        await OpenSocialLink(url);
    }
}
