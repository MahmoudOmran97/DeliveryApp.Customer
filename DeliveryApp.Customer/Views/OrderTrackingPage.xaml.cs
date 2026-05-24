using DeliveryApp.Customer.ViewModels;
using Mapsui;
using Mapsui.Layers;
using Mapsui.Projections;
using Mapsui.Styles;
using Mapsui.Tiling;
using Mapsui.UI.Maui;
using System.Text.Json;

namespace DeliveryApp.Customer.Views;

public partial class OrderTrackingPage : ContentPage
{
    readonly OrderTrackingViewModel _vm;

    MemoryLayer? _driverLayer;
    MemoryLayer? _customerLayer;
    MemoryLayer? _restaurantLayer;
    MemoryLayer? _routeLayer;

    bool _staticPinsDrawn = false;

    public OrderTrackingPage(OrderTrackingViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;

        SetupMap();
        vm.MapUpdated += OnMapUpdated;
    }

    void SetupMap()
    {
        // ── Logging عشان نشوف أي errors في تحميل الـ tiles ──
        Mapsui.Logging.Logger.LogDelegate = (level, msg, ex) =>
            System.Diagnostics.Debug.WriteLine($"[Mapsui/{level}] {msg} {ex?.Message}");

        // MapControl اتعرّف في الـ XAML مباشرة باسم MapControl
        MapControl.Map.Layers.Add(OpenStreetMap.CreateTileLayer());

        // Center على القاهرة كـ fallback
        var (x, y) = SphericalMercator.FromLonLat(31.2357, 30.0444);
        MapControl.Map.Navigator.CenterOnAndZoomTo(
            new MPoint(x, y),
            MapControl.Map.Navigator.Resolutions[13]);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        MapControl.Refresh();
    }

    // ─── بيتنادى لما الـ VM يلود الأوردر أو يتحدث موقع الدرايفر ─────────────
    void OnMapUpdated()
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            bool hasCustomer = _vm.CustomerLat != 0 && _vm.CustomerLng != 0;
            bool hasRestaurant = _vm.RestaurantLat != 0 && _vm.RestaurantLng != 0;

            // ── Debug ────────────────────────────────────────────────────────
            System.Diagnostics.Debug.WriteLine(
                $"[Map] Customer: {_vm.CustomerLat},{_vm.CustomerLng} | " +
                $"Restaurant: {_vm.RestaurantLat},{_vm.RestaurantLng} | " +
                $"Driver: {_vm.DriverLat},{_vm.DriverLng} HasDriver={_vm.HasDriver}");

            DebugLabel.Text =
                $"C:{_vm.CustomerLat:F4},{_vm.CustomerLng:F4}\n" +
                $"R:{_vm.RestaurantLat:F4},{_vm.RestaurantLng:F4}";

            // ── Static Pins: بس لما يكون عندنا الاتنين ──────────────────────
            if (!_staticPinsDrawn)
            {
                if (hasCustomer && hasRestaurant)
                {
                    _staticPinsDrawn = true;

                    DrawCustomerPin(_vm.CustomerLat, _vm.CustomerLng);
                    DrawRestaurantPin(_vm.RestaurantLat, _vm.RestaurantLng);

                    await DrawRouteAsync(
                        _vm.RestaurantLat, _vm.RestaurantLng,
                        _vm.CustomerLat, _vm.CustomerLng);

                    FitBounds(
                        _vm.RestaurantLat, _vm.RestaurantLng,
                        _vm.CustomerLat, _vm.CustomerLng);
                }
                else if (hasCustomer || hasRestaurant)
                {
                    // سنتر على اللي موجود بدون ما نغلق الـ flag
                    double lat = hasCustomer ? _vm.CustomerLat : _vm.RestaurantLat;
                    double lng = hasCustomer ? _vm.CustomerLng : _vm.RestaurantLng;
                    var (cx, cy) = SphericalMercator.FromLonLat(lng, lat);
                    MapControl.Map.Navigator.CenterOnAndZoomTo(
                        new MPoint(cx, cy),
                        MapControl.Map.Navigator.Resolutions[15]);
                }
            }

            // ── Driver Pin (بيتحرك) ──────────────────────────────────────────
            if (_vm.HasDriver)
                DrawDriverPin(_vm.DriverLat, _vm.DriverLng);

            MapControl.Refresh();
        });
    }

    // ─── Pin العميل (أزرق) ───────────────────────────────────────────────────
    void DrawCustomerPin(double lat, double lng)
    {
        if (_customerLayer != null) MapControl.Map.Layers.Remove(_customerLayer);
        var (x, y) = SphericalMercator.FromLonLat(lng, lat);
        _customerLayer = new MemoryLayer
        {
            Name = "CustomerLayer",
            Features = new[] { new PointFeature(new MPoint(x, y)) },
            Style = new SymbolStyle
            {
                SymbolScale = 1.0,
                Fill = new Mapsui.Styles.Brush(Mapsui.Styles.Color.FromString("#2196F3")),
                Outline = new Pen(Mapsui.Styles.Color.White, 2)
            }
        };
        MapControl.Map.Layers.Add(_customerLayer);
    }

    // ─── Pin المطعم (أخضر) ───────────────────────────────────────────────────
    void DrawRestaurantPin(double lat, double lng)
    {
        if (_restaurantLayer != null) MapControl.Map.Layers.Remove(_restaurantLayer);
        var (x, y) = SphericalMercator.FromLonLat(lng, lat);
        _restaurantLayer = new MemoryLayer
        {
            Name = "RestaurantLayer",
            Features = new[] { new PointFeature(new MPoint(x, y)) },
            Style = new SymbolStyle
            {
                SymbolScale = 1.1,
                Fill = new Mapsui.Styles.Brush(Mapsui.Styles.Color.FromString("#4CAF50")),
                Outline = new Pen(Mapsui.Styles.Color.White, 2)
            }
        };
        MapControl.Map.Layers.Add(_restaurantLayer);
    }

    // ─── Pin الدرايفر (برتقالي) ──────────────────────────────────────────────
    void DrawDriverPin(double lat, double lng)
    {
        if (_driverLayer != null) MapControl.Map.Layers.Remove(_driverLayer);
        var (x, y) = SphericalMercator.FromLonLat(lng, lat);
        _driverLayer = new MemoryLayer
        {
            Name = "DriverLayer",
            Features = new[] { new PointFeature(new MPoint(x, y)) },
            Style = new SymbolStyle
            {
                SymbolScale = 0.9,
                Fill = new Mapsui.Styles.Brush(Mapsui.Styles.Color.FromString("#FF5722")),
                Outline = new Pen(Mapsui.Styles.Color.White, 2)
            }
        };
        MapControl.Map.Layers.Add(_driverLayer);
    }

    // ─── Route من OSRM (مجاني بدون API key) ─────────────────────────────────
    async Task DrawRouteAsync(double fromLat, double fromLng, double toLat, double toLng)
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            var url = $"https://router.project-osrm.org/route/v1/driving/" +
                      $"{fromLng},{fromLat};{toLng},{toLat}" +
                      $"?overview=full&geometries=geojson";

            var json = await http.GetStringAsync(url);
            var doc = JsonDocument.Parse(json);

            var coords = doc.RootElement
                            .GetProperty("routes")[0]
                            .GetProperty("geometry")
                            .GetProperty("coordinates");

            var points = new List<MPoint>();
            foreach (var c in coords.EnumerateArray())
            {
                var (mx, my) = SphericalMercator.FromLonLat(c[0].GetDouble(), c[1].GetDouble());
                points.Add(new MPoint(mx, my));
            }

            if (points.Count < 2) return;
            if (_routeLayer != null) MapControl.Map.Layers.Remove(_routeLayer);

            var lineString = new NetTopologySuite.Geometries.LineString(
                points.Select(p =>
                    new NetTopologySuite.Geometries.Coordinate(p.X, p.Y)).ToArray());

            _routeLayer = new MemoryLayer
            {
                Name = "RouteLayer",
                Features = new[] { new Mapsui.Nts.GeometryFeature(lineString) },
                Style = new VectorStyle
                {
                    Line = new Pen(Mapsui.Styles.Color.FromString("#FF5722"), 4)
                }
            };

            MapControl.Map.Layers.Insert(1, _routeLayer); // تحت الـ pins وفوق الـ tiles
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Route] Failed: {ex.Message}");
        }
    }

    // ─── اضبط الـ viewport يشمل نقطتين ──────────────────────────────────────
    void FitBounds(double lat1, double lng1, double lat2, double lng2)
    {
        var (x1, y1) = SphericalMercator.FromLonLat(lng1, lat1);
        var (x2, y2) = SphericalMercator.FromLonLat(lng2, lat2);

        var dist = Math.Sqrt(Math.Pow(x2 - x1, 2) + Math.Pow(y2 - y1, 2));
        int zoom = dist switch
        {
            < 2000 => 16,
            < 5000 => 15,
            < 10000 => 14,
            < 20000 => 13,
            < 50000 => 12,
            _ => 11
        };

        MapControl.Map.Navigator.CenterOnAndZoomTo(
            new MPoint((x1 + x2) / 2, (y1 + y2) / 2),
            MapControl.Map.Navigator.Resolutions[zoom]);
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _vm.MapUpdated -= OnMapUpdated;
        _vm.Cleanup();
    }
}