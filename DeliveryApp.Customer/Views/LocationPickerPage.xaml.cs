using DeliveryApp.Customer.Helpers;
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

        // ✅ FIX: الكشف التلقائي بقى بيستخدم DeviceLocationHelper (دقة عالية + آخر موقع معروف
        // كـ fallback). لو فشل بنكمل بالقاهرة بصمت، والعميل عنده زرار "موقعي" يجرب بنفسه ويشوف السبب.
        var (loc, _) = await DeviceLocationHelper.GetCurrentAsync();
        if (loc != null)
        {
            lat = loc.Latitude;
            lng = loc.Longitude;
            _vm.SelectedLat = lat;
            _vm.SelectedLng = lng;
            _vm.IsLocationSelected = true;
        }

        await ExecuteMapScriptAsync($"centerOn({lng.ToString(CultureInfo.InvariantCulture)},{lat.ToString(CultureInfo.InvariantCulture)},15);setMarker('selected',{lng.ToString(CultureInfo.InvariantCulture)},{lat.ToString(CultureInfo.InvariantCulture)},'#FF5722','pin');");
        UpdateLabel(lat, lng);
    }

    async Task SetMarkerAsync(double lng, double lat)
    {
        await ExecuteMapScriptAsync($"setMarker('selected',{lng.ToString(CultureInfo.InvariantCulture)},{lat.ToString(CultureInfo.InvariantCulture)},'#FF5722','pin');");
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

    // ✅ FEATURE: زرار "موقعي" — بيحدد موقع العميل الحالي بنفسه قبل التأكيد،
    // عشان لو الكشف التلقائي فشل (أو العميل حرّك الدبوس بالغلط) يقدر يرجّعه بضغطة.
    bool _locating;

    async void OnMyLocationClicked(object? sender, EventArgs e)
    {
        if (_locating) return;
        _locating = true;
        MyLocationButton.IsEnabled = false;
        try
        {
            var (loc, failure) = await DeviceLocationHelper.GetCurrentAsync();
            if (loc == null)
            {
                await DeviceLocationHelper.ShowFailureAsync(this, failure);
                return;
            }

            _vm.SelectedLat = loc.Latitude;
            _vm.SelectedLng = loc.Longitude;
            _vm.IsLocationSelected = true;

            var lat = loc.Latitude.ToString(CultureInfo.InvariantCulture);
            var lng = loc.Longitude.ToString(CultureInfo.InvariantCulture);
            await ExecuteMapScriptAsync($"centerOn({lng},{lat},16);setMarker('selected',{lng},{lat},'#FF5722','pin');");
            UpdateLabel(loc.Latitude, loc.Longitude);
        }
        finally
        {
            _locating = false;
            MyLocationButton.IsEnabled = true;
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _mapReady = false;
        MapWebView.Navigating -= MapWebView_Navigating;
    }
}
