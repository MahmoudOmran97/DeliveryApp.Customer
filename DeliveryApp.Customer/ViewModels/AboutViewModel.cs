using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeliveryApp.Customer.Models;
using DeliveryApp.Customer.Services;
using Microsoft.Maui.ApplicationModel;

namespace DeliveryApp.Customer.ViewModels;

public partial class AboutViewModel : BaseViewModel
{
    private readonly ApiService _api;

    [ObservableProperty]
    private string _websiteUrl = "https://Taly-app.com";

    [ObservableProperty]
    private string _facebookUrl = "https://facebook.com/Taly";

    [ObservableProperty]
    private string _instagramUrl = "https://instagram.com/Taly";

    [ObservableProperty]
    private string _xUrl = "https://x.com/Taly";

    [ObservableProperty]
    private string _tikTokUrl = "https://www.tiktok.com/@Taly";

    public AboutViewModel(ApiService api)
    {
        _api = api;
        _ = LoadSiteLinksAsync();
    }

    private async Task LoadSiteLinksAsync()
    {
        try
        {
            var links = await _api.GetSiteLinksAsync();
            if (links == null || links.Count == 0)
                return;

            foreach (var link in links)
            {
                if (string.IsNullOrWhiteSpace(link.Url))
                    continue;

                switch (link.Key.Trim().ToLowerInvariant())
                {
                    case "website":
                    case "site":
                        WebsiteUrl = link.Url;
                        break;
                    case "facebook":
                        FacebookUrl = link.Url;
                        break;
                    case "instagram":
                        InstagramUrl = link.Url;
                        break;
                    case "x":
                    case "twitter":
                        XUrl = link.Url;
                        break;
                    case "tiktok":
                        TikTokUrl = link.Url;
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load site links: {ex.Message}");
        }
    }

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
        var website = string.IsNullOrWhiteSpace(WebsiteUrl)
            ? "https://Taly-app.com"
            : WebsiteUrl.TrimEnd('/');

        var url = type switch
        {
            "privacy" => $"{website}/Home/Privacy",
            "terms" => $"{website}/Home/Terms",
            _ => website
        };

        await OpenSocialLink(url);
    }
}
