using DeliveryApp.Customer.Helpers;
using DeliveryApp.Customer.Services;
using DeliveryApp.Customer.ViewModels;
using System.Globalization;

namespace DeliveryApp.Customer.Views;

public partial class HomeLocationPickerPage : ContentPage
{
    readonly HomeLocationPickerViewModel _vm;
    bool _mapReady;

    public HomeLocationPickerPage(HomeLocationPickerViewModel vm)
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
            }
        }
    }

    async Task SetInitialLocationAsync()
    {
        double lat = _vm.SelectedLat;
        double lng = _vm.SelectedLng;

        if (lat == LocationService.ZoneCenterLat && lng == LocationService.ZoneCenterLng)
        {
            // ✅ FIX: نفس إصلاح صفحة الشيك أوت — DeviceLocationHelper بدل المحاولة الواحدة الضعيفة
            var (loc, _) = await DeviceLocationHelper.GetCurrentAsync();
            if (loc != null)
            {
                lat = loc.Latitude;
                lng = loc.Longitude;
                _vm.SelectedLat = lat;
                _vm.SelectedLng = lng;
            }
        }

        await ExecuteMapScriptAsync($"centerOn({lng.ToString(CultureInfo.InvariantCulture)},{lat.ToString(CultureInfo.InvariantCulture)},15);setMarker('selected',{lng.ToString(CultureInfo.InvariantCulture)},{lat.ToString(CultureInfo.InvariantCulture)},'#FF5722','pin');");
    }

    Task SetMarkerAsync(double lng, double lat) =>
        ExecuteMapScriptAsync($"setMarker('selected',{lng.ToString(CultureInfo.InvariantCulture)},{lat.ToString(CultureInfo.InvariantCulture)},'#FF5722','pin');");

    Task<string> ExecuteMapScriptAsync(string script) =>
        _mapReady ? MapWebView.EvaluateJavaScriptAsync(script) : Task.FromResult(string.Empty);

    // ✅ FEATURE: زرار "موقعي" — يحدد موقع العميل الحالي قبل التأكيد
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
