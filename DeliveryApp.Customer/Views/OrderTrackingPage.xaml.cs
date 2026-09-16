using DeliveryApp.Customer.Services;
using DeliveryApp.Customer.ViewModels;
using System.Globalization;
using System.Text.Json;
using System.Diagnostics;

namespace DeliveryApp.Customer.Views;

public partial class OrderTrackingPage : ContentPage
{
    readonly OrderTrackingViewModel _vm;
    bool _mapReady;
    bool _staticPinsDrawn;
    double _lastDriverRouteFromLat;
    double _lastDriverRouteFromLng;
    DateTime _lastDriverRouteTime = DateTime.MinValue;

    static readonly HttpClient _routingHttp = new()
    {
        Timeout = TimeSpan.FromSeconds(10)
    };

    static OrderTrackingPage()
    {
        _routingHttp.DefaultRequestHeaders.UserAgent.ParseAdd(
            "TalyCustomerApp/1.0 (+https://your-domain.com)");
    }

    public OrderTrackingPage(OrderTrackingViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
        vm.MapUpdated += OnMapUpdated;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _mapReady = false;
        // ✅ FIX: كان الفلاج ده بيفضل true من آخر مرة الصفحة اتفتحت، فلما ترجع
        // تاني للصفحة الـ WebView بيتعمله reload كامل (خريطة فاضية من غير ماركرز)
        // بس الكود كان بيتخطى رسم ماركر العميل/المطعم لأن الفلاج already true —
        // فكان بيفضل ظاهر بس ماركر الدليفري لأنه مش متحكم بالفلاج ده.
        _staticPinsDrawn = false;
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
            await RefreshMapAsync();
            return;
        }

        // No navigation is performed: this is only an in-page event bridge.
        if (e.Url.StartsWith("app://", StringComparison.OrdinalIgnoreCase))
            e.Cancel = true;
    }

    async void OnMapUpdated()
    {
        await MainThread.InvokeOnMainThreadAsync(RefreshMapAsync);
    }

    async Task RefreshMapAsync()
    {
        if (!_mapReady) return;

        bool hasCustomer = _vm.CustomerLat != 0 && _vm.CustomerLng != 0;
        bool hasRestaurant = _vm.RestaurantLat != 0 && _vm.RestaurantLng != 0;

        if (!_staticPinsDrawn && hasCustomer && hasRestaurant)
        {
            _staticPinsDrawn = true;
            await SetMarkerAsync("customer", _vm.CustomerLng, _vm.CustomerLat, "#2196F3", "user");
            await SetMarkerAsync("restaurant", _vm.RestaurantLng, _vm.RestaurantLat, "#4CAF50", "shop");

            await DrawRouteAndUpdateEtaAsync(
                _vm.RestaurantLat, _vm.RestaurantLng,
                _vm.CustomerLat, _vm.CustomerLng,
                "#FF5722", 5, "customer-route", true);

            await FitToPointsAsync([
                [_vm.RestaurantLng, _vm.RestaurantLat],
                [_vm.CustomerLng, _vm.CustomerLat]
            ]);
        }
        else if (!_staticPinsDrawn && hasCustomer)
        {
            _staticPinsDrawn = true;
            await SetMarkerAsync("customer", _vm.CustomerLng, _vm.CustomerLat, "#2196F3", "user");
            await CenterOnAsync(_vm.CustomerLng, _vm.CustomerLat, 15);
        }
        else if (!_staticPinsDrawn && hasRestaurant)
        {
            _staticPinsDrawn = true;
            await SetMarkerAsync("restaurant", _vm.RestaurantLng, _vm.RestaurantLat, "#4CAF50", "shop");
            await CenterOnAsync(_vm.RestaurantLng, _vm.RestaurantLat, 15);
        }

        if (_vm.HasDriver && _vm.DriverLat != 0)
        {
            await SetMarkerAsync("driver", _vm.DriverLng, _vm.DriverLat, "#FF5722", "driver");

            if (hasCustomer && ShouldUpdateDriverRoute())
            {
                _lastDriverRouteFromLat = _vm.DriverLat;
                _lastDriverRouteFromLng = _vm.DriverLng;
                _lastDriverRouteTime = DateTime.Now;

                await DrawRouteAndUpdateEtaAsync(
                    _vm.DriverLat, _vm.DriverLng,
                    _vm.CustomerLat, _vm.CustomerLng,
                    "#FF9800", 4, "driver-route", true);
            }
        }
    }

    bool ShouldUpdateDriverRoute()
    {
        if (_lastDriverRouteFromLat == 0) return true;
        if ((DateTime.Now - _lastDriverRouteTime).TotalSeconds > 15) return true;

        double dlat = _vm.DriverLat - _lastDriverRouteFromLat;
        double dlng = _vm.DriverLng - _lastDriverRouteFromLng;
        return Math.Sqrt(dlat * dlat + dlng * dlng) > 0.0005;
    }

    async Task SetMarkerAsync(string id, double lng, double lat, string color, string iconType)
    {
        var script = string.Format(
            CultureInfo.InvariantCulture,
            "setMarker('{0}',{1},{2},'{3}','{4}');",
            id, lng, lat, color, iconType);
        await ExecuteMapScriptAsync(script);
    }

    async Task DrawRouteAndUpdateEtaAsync(
        double fromLat, double fromLng,
        double toLat, double toLng,
        string colorHex, double width,
        string routeId,
        bool updateEta)
    {
        try
        {
            var url = string.Format(
                CultureInfo.InvariantCulture,
                "https://router.project-osrm.org/route/v1/driving/{0},{1};{2},{3}?overview=full&geometries=geojson",
                fromLng, fromLat, toLng, toLat);

            var json = await _routingHttp.GetStringAsync(url);
            using var doc = JsonDocument.Parse(json);
            var routes = doc.RootElement.GetProperty("routes");
            if (routes.GetArrayLength() == 0) return;

            var route = routes[0];
            if (updateEta)
            {
                double distanceM = route.GetProperty("distance").GetDouble();
                double durationS = route.GetProperty("duration").GetDouble();
                UpdateEtaAndDistance(distanceM, durationS, routeId);
            }

            var coords = route.GetProperty("geometry").GetProperty("coordinates")
                .EnumerateArray()
                .Select(c => new[] { c[0].GetDouble(), c[1].GetDouble() })
                .ToList();

            if (coords.Count < 2) return;

            var jsonCoords = JsonSerializer.Serialize(coords);
            var script = $"setRoute('{routeId}',{jsonCoords},'{colorHex}',{width.ToString(CultureInfo.InvariantCulture)});";
            await ExecuteMapScriptAsync(script);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Route:{routeId}] {ex.GetType().Name}: {ex.Message}");
        }
    }

    void UpdateEtaAndDistance(double distanceM, double durationS, string routeId)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (routeId != "driver-route") return;

            _vm.UpdateDeliveryEta(durationS);
            bool isAr = Services.LocalizationService.Current.TwoLetterISOLanguageName == "ar";

            if (distanceM < 1000)
            {
                var meters = distanceM.ToString("F0", CultureInfo.InvariantCulture);
                _vm.Distance = isAr ? $"{meters} م" : $"{meters} m";
            }
            else
            {
                var km = (distanceM / 1000).ToString("F1", CultureInfo.InvariantCulture);
                _vm.Distance = isAr ? $"{km} كم" : $"{km} km";
            }

            if (durationS < 60)
            {
                _vm.TravelTime = isAr ? "< 1 دقيقة" : "< 1 min";
            }
            else
            {
                var mins = Math.Ceiling(durationS / 60)
                    .ToString("F0", CultureInfo.InvariantCulture);
                _vm.TravelTime = isAr ? $"{mins} دقيقة" : $"{mins} min";
            }
        });
    }

    async Task FitToPointsAsync(double[][] points)
    {
        await ExecuteMapScriptAsync($"fitToPoints({JsonSerializer.Serialize(points)});");
    }

    async Task CenterOnAsync(double lng, double lat, int zoom)
    {
        await ExecuteMapScriptAsync(string.Format(
            CultureInfo.InvariantCulture,
            "centerOn({0},{1},{2});", lng, lat, zoom));
    }

    Task<string> ExecuteMapScriptAsync(string script) =>
        _mapReady ? MapWebView.EvaluateJavaScriptAsync(script) : Task.FromResult(string.Empty);

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _mapReady = false;
        MapWebView.Navigating -= MapWebView_Navigating;
        _vm.MapUpdated -= OnMapUpdated;
        _vm.Cleanup();
    }
}
