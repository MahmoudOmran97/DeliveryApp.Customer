using DeliveryApp.Customer.ViewModels;
using Mapsui;
using Mapsui.Layers;
using Mapsui.Projections;
using Mapsui.Styles;
using Mapsui.Tiling;
using System.Text.Json;
using System.Diagnostics;

namespace DeliveryApp.Customer.Views;

public partial class OrderTrackingPage : ContentPage
{
    readonly OrderTrackingViewModel _vm;
    MemoryLayer? _driverLayer;
    MemoryLayer? _customerLayer;
    MemoryLayer? _restaurantLayer;
    MemoryLayer? _routeLayer;
    MemoryLayer? _driverRouteLayer;
    bool _staticPinsDrawn = false;

    // ── SVG sources (بيتبنوا مرة واحدة) ──────────────────────────────────────
    static readonly string _customerSvg = BuildPinSvg("#2196F3", "#1565C0");
    static readonly string _restaurantSvg = BuildPinSvg("#4CAF50", "#2E7D32");
    static readonly string _driverSvg = BuildCircleSvg("#FF5722", "#BF360C");

    public OrderTrackingPage(OrderTrackingViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
        SetupMap();
        vm.MapUpdated += OnMapUpdated;
    }

    // ─── SVG: شكل teardrop pin للمواقع الثابتة ───────────────────────────────
    static string BuildPinSvg(string fill, string dark) =>
        $"svg-content://<svg xmlns='http://www.w3.org/2000/svg' width='56' height='72' viewBox='0 0 56 72'>" +
        $"<defs><filter id='sh'><feDropShadow dx='0' dy='2' stdDeviation='2.5' flood-color='#00000055'/></filter></defs>" +
        $"<g filter='url(#sh)'>" +
        $"<path d='M28 4C14.7 4 4 14.7 4 28C4 44 28 68 28 68C28 68 52 44 52 28C52 14.7 41.3 4 28 4Z' fill='{fill}' stroke='white' stroke-width='2'/>" +
        $"</g>" +
        $"<circle cx='28' cy='28' r='10' fill='white' opacity='0.9'/>" +
        $"<circle cx='28' cy='28' r='7' fill='{dark}'/>" +
        $"</svg>";

    // ─── SVG: دائرة للدرايفر (بيتحرك) ────────────────────────────────────────
    static string BuildCircleSvg(string fill, string dark) =>
        $"svg-content://<svg xmlns='http://www.w3.org/2000/svg' width='60' height='60' viewBox='0 0 60 60'>" +
        $"<defs><filter id='sh'><feDropShadow dx='0' dy='2' stdDeviation='3' flood-color='#00000060'/></filter></defs>" +
        $"<circle cx='30' cy='30' r='26' fill='{fill}' stroke='white' stroke-width='3' filter='url(#sh)'/>" +
        $"<circle cx='30' cy='30' r='16' fill='white' opacity='0.2'/>" +
        $"<text x='30' y='38' text-anchor='middle' font-size='22' fill='white'>🛵</text>" +
        $"</svg>";

    // ─── إعداد الخريطة ────────────────────────────────────────────────────────
    void SetupMap()
    {
        Mapsui.Logging.Logger.LogDelegate = (level, msg, ex) =>
            Debug.WriteLine($"[Mapsui/{level}] {msg} {ex?.Message}");

        MapControl.Map.Layers.Add(OpenStreetMap.CreateTileLayer());
        var (x, y) = SphericalMercator.FromLonLat(31.2357, 30.0444);
        MapControl.Map.Navigator.CenterOnAndZoomTo(
            new MPoint(x, y), MapControl.Map.Navigator.Resolutions[13]);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        MapControl.Refresh();
    }

    // ─── يتنادى لما الـ VM يجيب بيانات أو يتحدث موقع الدرايفر ───────────────
    void OnMapUpdated()
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            bool hasCustomer = _vm.CustomerLat != 0 && _vm.CustomerLng != 0;
            bool hasRestaurant = _vm.RestaurantLat != 0 && _vm.RestaurantLng != 0;

            if (!_staticPinsDrawn && hasCustomer && hasRestaurant)
            {
                _staticPinsDrawn = true;

                DrawImagePin(ref _customerLayer, "CustomerLayer",
                    _vm.CustomerLat, _vm.CustomerLng, _customerSvg, 1.0);

                DrawImagePin(ref _restaurantLayer, "RestaurantLayer",
                    _vm.RestaurantLat, _vm.RestaurantLng, _restaurantSvg, 1.0);

                var routeLayer = _routeLayer;
                await DrawRouteAsync(
                    _vm.RestaurantLat, _vm.RestaurantLng,
                    _vm.CustomerLat, _vm.CustomerLng,
                    "#FF5722", 5, "RouteLayer",
                    onComplete: layer => _routeLayer = layer);

                FitBounds(
                    _vm.RestaurantLat, _vm.RestaurantLng,
                    _vm.CustomerLat, _vm.CustomerLng);
            }
            else if (!_staticPinsDrawn && (hasCustomer || hasRestaurant))
            {
                double lat = hasCustomer ? _vm.CustomerLat : _vm.RestaurantLat;
                double lng = hasCustomer ? _vm.CustomerLng : _vm.RestaurantLng;
                var (cx, cy) = SphericalMercator.FromLonLat(lng, lat);
                MapControl.Map.Navigator.CenterOnAndZoomTo(
                    new MPoint(cx, cy), MapControl.Map.Navigator.Resolutions[15]);
            }

            // ── Driver ───────────────────────────────────────────────────────
            if (_vm.HasDriver && _vm.DriverLat != 0)
            {
                DrawImagePin(ref _driverLayer, "DriverLayer",
                    _vm.DriverLat, _vm.DriverLng, _driverSvg, 0.9);

                if (hasCustomer)
                {
                    await DrawRouteAsync(
                        _vm.DriverLat, _vm.DriverLng,
                        _vm.CustomerLat, _vm.CustomerLng,
                        "#FF9800", 3, "DriverRouteLayer",
                        onComplete: layer => _driverRouteLayer = layer);
                }
            }

            MapControl.Refresh();
        });
    }

    // ─── رسم Pin بـ ImageStyle (Mapsui 5 API الصح) ────────────────────────────
    void DrawImagePin(ref MemoryLayer? existing, string name,
                      double lat, double lng,
                      string svgSource, double scale)
    {
        if (existing != null) MapControl.Map.Layers.Remove(existing);

        var (x, y) = SphericalMercator.FromLonLat(lng, lat);
        var feature = new PointFeature(new MPoint(x, y));
        feature.Styles = new List<IStyle>
        {
            new ImageStyle
            {
                Image       = svgSource,   // "svg-content://..." → Mapsui 5 API
                SymbolScale = scale,
                // Offset للـ teardrop: نحرك الـ pin لفوق عشان الـ tip يبقى على الإحداثي
               RelativeOffset = new RelativeOffset(0.0, 0.5)
            }
        };

        existing = new MemoryLayer
        {
            Name = name,
            Features = new[] { feature },
            Style = null
        };

        MapControl.Map.Layers.Add(existing);
    }

    // ─── Route من OSRM (بدون ref في async) ───────────────────────────────────
    async Task DrawRouteAsync(
        double fromLat, double fromLng,
        double toLat, double toLng,
        string colorHex, double width,
        string layerName,
        Action<MemoryLayer> onComplete)
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            var url = $"https://router.project-osrm.org/route/v1/driving/" +
                      $"{fromLng},{fromLat};{toLng},{toLat}" +
                      $"?overview=full&geometries=geojson";

            var json = await http.GetStringAsync(url);
            var doc = JsonDocument.Parse(json);
            var route = doc.RootElement.GetProperty("routes")[0];

            if (layerName == "RouteLayer")
            {
                _vm.Distance = $"{route.GetProperty("distance").GetDouble() / 1000:F1} km";
                _vm.TravelTime = $"{Math.Ceiling(route.GetProperty("duration").GetDouble() / 60):F0} min";
            }

            var coords = route.GetProperty("geometry").GetProperty("coordinates");
            var points = new List<MPoint>();
            foreach (var c in coords.EnumerateArray())
            {
                var (mx, my) = SphericalMercator.FromLonLat(c[0].GetDouble(), c[1].GetDouble());
                points.Add(new MPoint(mx, my));
            }
            if (points.Count < 2) return;

            // احذف الـ layer القديم لو موجود
            var existingByName = MapControl.Map.Layers
                .FirstOrDefault(l => l.Name == layerName);
            if (existingByName != null) MapControl.Map.Layers.Remove(existingByName);

            var line = new NetTopologySuite.Geometries.LineString(
                points.Select(p =>
                    new NetTopologySuite.Geometries.Coordinate(p.X, p.Y)).ToArray());
            var feature = new Mapsui.Nts.GeometryFeature(line);
            feature.Styles = new List<IStyle>
            {
                // Shadow تحت الـ route
                new VectorStyle
                {
                    Line = new Pen(Mapsui.Styles.Color.FromArgb(50, 0, 0, 0), width + 4)
                },
                // Main route
                new VectorStyle
                {
                    Line = new Pen(Mapsui.Styles.Color.FromString(colorHex), width)
                },
                // Dashes بيضاء فوق
                new VectorStyle
                {
                    Line = new Pen(Mapsui.Styles.Color.White, 1.5f)
                    {
                        PenStyle = PenStyle.Dash
                    }
                }
            };

            var newLayer = new MemoryLayer
            {
                Name = layerName,
                Features = new[] { feature },
                Style = null
            };

            int insertIdx = Math.Min(1, Math.Max(0, MapControl.Map.Layers.Count - 1));
            MapControl.Map.Layers.Insert(insertIdx, newLayer);

            // نرجع الـ layer بـ callback بدل ref
            onComplete(newLayer);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Route:{layerName}] {ex.Message}");
        }
    }

    // ─── Fit Viewport ─────────────────────────────────────────────────────────
    void FitBounds(double lat1, double lng1, double lat2, double lng2)
    {
        var (x1, y1) = SphericalMercator.FromLonLat(lng1, lat1);
        var (x2, y2) = SphericalMercator.FromLonLat(lng2, lat2);

        double dist = Math.Sqrt(Math.Pow(x2 - x1, 2) + Math.Pow(y2 - y1, 2));
        int zoom = dist switch
        {
            < 3000 => 16,
            < 7000 => 15,
            < 15000 => 14,
            < 30000 => 13,
            _ => 12
        };

        MapControl.Map.Navigator.CenterOnAndZoomTo(
            new MPoint((x1 + x2) / 2, (y1 + y2) / 2),
            MapControl.Map.Navigator.Resolutions[Math.Max(zoom - 1, 0)]);
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _vm.MapUpdated -= OnMapUpdated;
        _vm.Cleanup();
    }
}