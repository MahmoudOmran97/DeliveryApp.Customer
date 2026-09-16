using DeliveryApp.Customer.Services;
using DeliveryApp.Customer.ViewModels;
using System.Globalization;

namespace DeliveryApp.Customer.Views;

public partial class LocationPickerPage : ContentPage
{
    readonly LocationPickerViewModel _vm;
    bool _mapReady;

    public LocationPickerPage(LocationPickerViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _mapReady = false;
        MapWebView.Navigating -= MapWebView_Navigating;
        MapWebView.Navigating += MapWebView_Navigating;
        MapWebView.Source = new HtmlWebViewSource
        {
            Html = OpenFreeMapHtml.Create()
        };
    }

    async void MapWebView_Navigating(object? sender, WebNavigatingEventArgs e)
    {
        if (e.Url == "app://map-ready")
        {
            e.Cancel = true;
            _mapReady = true;
            await SetInitialLocationAsync();
            return;
        }

        if (e.Url.StartsWith("app://map-click", StringComparison.OrdinalIgnoreCase))
        {
            e.Cancel = true;
            if (WebViewMapQuery.TryGetCoordinates(e.Url, out var lat, out var lng))
            {
                _vm.SelectedLat = lat;
                _vm.SelectedLng = lng;
                await SetMarkerAsync(lng, lat);
                UpdateLabel(lat, lng);
            }
        }
    }

    async Task SetInitialLocationAsync()
    {
        double lat = 30.0444;
        double lng = 31.2357;

        try
        {
            var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            if (status == PermissionStatus.Granted)
            {
                var loc = await Geolocation.Default.GetLocationAsync(
                    new GeolocationRequest(GeolocationAccuracy.Medium)
                    {
                        Timeout = TimeSpan.FromSeconds(8)
                    });

                if (loc != null)
                {
                    lat = loc.Latitude;
                    lng = loc.Longitude;
                    _vm.SelectedLat = lat;
                    _vm.SelectedLng = lng;
                }
            }
        }
        catch
        {
            // GPS failure: keep Cairo as the fallback.
        }

        await ExecuteMapScriptAsync($"centerOn({lng.ToString(CultureInfo.InvariantCulture)},{lat.ToString(CultureInfo.InvariantCulture)},15);setMarker('selected',{lng.ToString(CultureInfo.InvariantCulture)},{lat.ToString(CultureInfo.InvariantCulture)},'#FF5722','📍');");
        UpdateLabel(lat, lng);
    }

    async Task SetMarkerAsync(double lng, double lat)
    {
        await ExecuteMapScriptAsync($"setMarker('selected',{lng.ToString(CultureInfo.InvariantCulture)},{lat.ToString(CultureInfo.InvariantCulture)},'#FF5722','📍');");
    }

    Task<string> ExecuteMapScriptAsync(string script) =>
        _mapReady ? MapWebView.EvaluateJavaScriptAsync(script) : Task.FromResult(string.Empty);

    void UpdateLabel(double lat, double lng)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            CoordinatesLabel.Text = $"📍 {lat:F5}, {lng:F5}";
        });
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _mapReady = false;
        MapWebView.Navigating -= MapWebView_Navigating;
    }
}
