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

    // حالة الرسم - نتعقب آخر إحداثيات رُسمت عليها الـ pins
    bool _staticPinsDrawn = false;
    double _lastDriverRouteFromLat = 0, _lastDriverRouteFromLng = 0;
    DateTime _lastDriverRouteTime = DateTime.MinValue;

    // Images - Mapsui requires svg-content:// URI scheme, not plain filenames
    static readonly string _customerMarker = "svg-content://<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 64 64'><path d='M32 2C20.4 2 11 11.4 11 23c0 14 18.2 35.8 20.1 38.1.5.6 1.4.6 1.9 0C34.8 58.8 53 37 53 23 53 11.4 43.6 2 32 2z' fill='#2196F3'/><circle cx='32' cy='23' r='10' fill='#FFFFFF'/><circle cx='32' cy='20' r='4.6' fill='#2196F3'/><path d='M24.5 30.5c1.8-3 4.2-4.5 7.5-4.5s5.7 1.5 7.5 4.5' fill='none' stroke='#2196F3' stroke-width='3' stroke-linecap='round'/></svg>";
    static readonly string _restaurantMarker = "svg-content://<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 64 64'><path d='M32 2C20.4 2 11 11.4 11 23c0 14 18.2 35.8 20.1 38.1.5.6 1.4.6 1.9 0C34.8 58.8 53 37 53 23 53 11.4 43.6 2 32 2z' fill='#4CAF50'/><rect x='20' y='16' width='24' height='18' rx='2' fill='#FFFFFF'/><path d='M20 22h24' stroke='#4CAF50' stroke-width='3'/><rect x='24' y='25' width='7' height='9' fill='#4CAF50'/><rect x='34' y='25' width='8' height='6' fill='#4CAF50'/></svg>";
    static readonly string _driverMarker = "svg-content://<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 64 64'><circle cx='32' cy='32' r='30' fill='#FF5722'/><circle cx='22' cy='43' r='8' fill='#FFFFFF'/><circle cx='22' cy='43' r='3.5' fill='#263238'/><circle cx='44' cy='43' r='8' fill='#FFFFFF'/><circle cx='44' cy='43' r='3.5' fill='#263238'/><path d='M18 36h18l8-8h-9l-4-8h-7l3 8h-9z' fill='#263238'/><circle cx='41' cy='24' r='4' fill='#FFFFFF'/></svg>";

    public OrderTrackingPage(OrderTrackingViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
        SetupMap();
        vm.MapUpdated += OnMapUpdated;
    }

    // ─── SVG: teardrop pin ────────────────────────────────────────────────────
    static string BuildPinSvg(string fill, string dark) =>
        $"svg-content://<svg xmlns='http://www.w3.org/2000/svg' width='56' height='72' viewBox='0 0 56 72'>" +
        $"<defs><filter id='sh'><feDropShadow dx='0' dy='2' stdDeviation='2.5' flood-color='#00000055'/></filter></defs>" +
        $"<g filter='url(#sh)'>" +
        $"<path d='M28 4C14.7 4 4 14.7 4 28C4 44 28 68 28 68C28 68 52 44 52 28C52 14.7 41.3 4 28 4Z' fill='{fill}' stroke='white' stroke-width='2'/>" +
        $"</g>" +
        $"<circle cx='28' cy='28' r='10' fill='white' opacity='0.9'/>" +
        $"<circle cx='28' cy='28' r='7' fill='{dark}'/>" +
        $"</svg>";

    // ─── SVG: driver circle ───────────────────────────────────────────────────
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

        // Center on Cairo as default until real coordinates arrive
        var (x, y) = SphericalMercator.FromLonLat(31.2357, 30.0444);
        MapControl.Map.Navigator.CenterOnAndZoomTo(
            new MPoint(x, y), MapControl.Map.Navigator.Resolutions[13]);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        MapControl.Refresh();
    }

    // ─── Main handler: يتنادى لما ViewModel يجيب data أو يتحدث موقع الدرايفر ──
    void OnMapUpdated()
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            bool hasCustomer = _vm.CustomerLat != 0 && _vm.CustomerLng != 0;
            bool hasRestaurant = _vm.RestaurantLat != 0 && _vm.RestaurantLng != 0;

            // ── الـ Static Pins (مطعم + عميل) ─────────────────────────────
            // FIX: نرسمهم لما يكون عندنا إحداثيات حتى لو مرة واحدة فقط
            if (!_staticPinsDrawn && hasCustomer && hasRestaurant)
            {
                _staticPinsDrawn = true;

                DrawPin(ref _customerLayer, "CustomerLayer",
                    _vm.CustomerLat, _vm.CustomerLng, _customerMarker, 0.8);

                DrawPin(ref _restaurantLayer, "RestaurantLayer",
                    _vm.RestaurantLat, _vm.RestaurantLng, _restaurantMarker, 0.8);

                // رسم الـ Route بين المطعم والعميل
                await DrawRouteAndUpdateEtaAsync(
                    _vm.RestaurantLat, _vm.RestaurantLng,
                    _vm.CustomerLat, _vm.CustomerLng,
                    "#FF5722", 5, "RouteLayer",
                    updateEta: true,
                    onComplete: layer => _routeLayer = layer);

                // Fit map to show both pins
                FitBounds(
                    _vm.RestaurantLat, _vm.RestaurantLng,
                    _vm.CustomerLat, _vm.CustomerLng);
            }
            else if (!_staticPinsDrawn && hasCustomer)
            {
                // لو في موقع عميل بس، اعرضه وانتظر المطعم
                DrawPin(ref _customerLayer, "CustomerLayer",
                    _vm.CustomerLat, _vm.CustomerLng, _customerMarker, 0.8);
                CenterOn(_vm.CustomerLat, _vm.CustomerLng, 15);
            }
            else if (!_staticPinsDrawn && hasRestaurant)
            {
                DrawPin(ref _restaurantLayer, "RestaurantLayer",
                    _vm.RestaurantLat, _vm.RestaurantLng, _restaurantMarker, 0.8);
                CenterOn(_vm.RestaurantLat, _vm.RestaurantLng, 15);
            }

            // ── الدرايفر ─────────────────────────────────────────────────────
            if (_vm.HasDriver && _vm.DriverLat != 0)
            {
                // تحديث pin الدرايفر دايما
                DrawPin(ref _driverLayer, "DriverLayer",
                    _vm.DriverLat, _vm.DriverLng, _driverMarker, 0.8);

                // FIX: تحديث Route الدرايفر → العميل كل 15 ثانية أو لو تحرك أكتر من 50m
                if (hasCustomer && ShouldUpdateDriverRoute())
                {
                    _lastDriverRouteFromLat = _vm.DriverLat;
                    _lastDriverRouteFromLng = _vm.DriverLng;
                    _lastDriverRouteTime = DateTime.Now;

                    await DrawRouteAndUpdateEtaAsync(
                        _vm.DriverLat, _vm.DriverLng,
                        _vm.CustomerLat, _vm.CustomerLng,
                        "#FF9800", 4, "DriverRouteLayer",
                        updateEta: true, // FIX: تحديث ETA من route الدرايفر مش المطعم
                        onComplete: layer => _driverRouteLayer = layer);
                }
            }

            MapControl.Refresh();
        });
    }

    // ─── هل لازم نحدث route الدرايفر؟ ────────────────────────────────────────
    bool ShouldUpdateDriverRoute()
    {
        // أول مرة
        if (_lastDriverRouteFromLat == 0) return true;

        // كل 15 ثانية على الأقل
        if ((DateTime.Now - _lastDriverRouteTime).TotalSeconds > 15) return true;

        // لو تحرك أكتر من ~50 متر
        double dlat = _vm.DriverLat - _lastDriverRouteFromLat;
        double dlng = _vm.DriverLng - _lastDriverRouteFromLng;
        double distDeg = Math.Sqrt(dlat * dlat + dlng * dlng);
        return distDeg > 0.0005; // ~55 متر
    }

    // ─── رسم Pin ──────────────────────────────────────────────────────────────
    void DrawPin(ref MemoryLayer? existing, string name,
                 double lat, double lng, string imageSource, double scale)
    {
        // FIX: Remove safely - check layer is still in map before removing
        if (existing != null)
        {
            try { MapControl.Map.Layers.Remove(existing); }
            catch { /* layer may already be gone */ }
        }

        var (x, y) = SphericalMercator.FromLonLat(lng, lat);
        var feature = new PointFeature(new MPoint(x, y));
        feature.Styles = new List<IStyle>
        {
            new ImageStyle
            {
                Image = imageSource,
                SymbolScale = scale,
                // لإرجاع الـ tip الخاص بالماركر على الإحداثي بالضبط
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

    // ─── رسم Route + تحديث ETA/Distance ──────────────────────────────────────
    async Task DrawRouteAndUpdateEtaAsync(
        double fromLat, double fromLng,
        double toLat, double toLng,
        string colorHex, double width,
        string layerName,
        bool updateEta,
        Action<MemoryLayer> onComplete)
    {
        try
        {
            Debug.WriteLine($"[Route:{layerName}] ▶ START from ({fromLat},{fromLng}) to ({toLat},{toLng})");

            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            http.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Linux; Android 10) AppleWebKit/537.36 Chrome/120 Mobile Safari/537.36");
            var url = $"https://router.project-osrm.org/route/v1/driving/" +
                      $"{fromLng},{fromLat};{toLng},{toLat}" +
                      $"?overview=full&geometries=geojson";

            Debug.WriteLine($"[Route:{layerName}] 🌐 Calling OSRM: {url}");
            var json = await http.GetStringAsync(url);
            Debug.WriteLine($"[Route:{layerName}] ✅ OSRM response length: {json.Length}");

            var doc = JsonDocument.Parse(json);
            var routes = doc.RootElement.GetProperty("routes");
            Debug.WriteLine($"[Route:{layerName}] 📍 Routes count: {routes.GetArrayLength()}");
            if (routes.GetArrayLength() == 0) return;

            var route = routes[0];

            // FIX: تحديث ETA والمسافة - سواء من الدرايفر أو من المطعم
            if (updateEta)
            {
                double distanceM = route.GetProperty("distance").GetDouble();
                double durationS = route.GetProperty("duration").GetDouble();

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    _vm.Distance = distanceM < 1000
                        ? $"{distanceM:F0} م"
                        : $"{distanceM / 1000:F1} كم";
                    _vm.TravelTime = durationS < 60
                        ? "< 1 دقيقة"
                        : $"{Math.Ceiling(durationS / 60):F0} دقيقة";
                });
            }

            // بناء نقاط الـ route
            var coords = route.GetProperty("geometry").GetProperty("coordinates");
            var points = new List<MPoint>();
            foreach (var c in coords.EnumerateArray())
            {
                var (mx, my) = SphericalMercator.FromLonLat(c[0].GetDouble(), c[1].GetDouble());
                points.Add(new MPoint(mx, my));
            }
            Debug.WriteLine($"[Route:{layerName}] 📐 Points built: {points.Count}");
            if (points.Count < 2) return;

            // FIX: حذف الـ layer القديم بالاسم بشكل آمن
            var existingByName = MapControl.Map.Layers
                .FirstOrDefault(l => l.Name == layerName);
            if (existingByName != null)
            {
                try { MapControl.Map.Layers.Remove(existingByName); }
                catch { }
            }

            var line = new NetTopologySuite.Geometries.LineString(
                points.Select(p =>
                    new NetTopologySuite.Geometries.Coordinate(p.X, p.Y)).ToArray());

            var feature = new Mapsui.Nts.GeometryFeature(line);
            feature.Styles = new List<IStyle>
            {
                // Shadow
                new VectorStyle
                {
                    Line = new Pen(Mapsui.Styles.Color.FromArgb(50, 0, 0, 0), width + 4)
                },
                // Main line
                new VectorStyle
                {
                    Line = new Pen(Mapsui.Styles.Color.FromString(colorHex), width)
                },
                // White dashes
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

            // FIX: أضف route layers تحت Pin layers (index 1)
            // Tile layer is index 0, routes at 1, pins at top
            int insertIdx = Math.Max(1, MapControl.Map.Layers.Count - 1);
            // إذا في pin layers بالفعل، أضف الـ route قبلهم
            int pinLayerIdx = MapControl.Map.Layers
                .ToList()
                .FindIndex(l => l.Name is "CustomerLayer" or "RestaurantLayer" or "DriverLayer");
            if (pinLayerIdx > 0)
                insertIdx = pinLayerIdx;
            else
                insertIdx = Math.Min(1, MapControl.Map.Layers.Count);

            MapControl.Map.Layers.Insert(insertIdx, newLayer);
            onComplete(newLayer);
            Debug.WriteLine($"[Route:{layerName}] ✅ DONE - layer inserted at index {insertIdx}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Route:{layerName}] ERROR: {ex.GetType().Name}: {ex.Message}");
            Debug.WriteLine($"[Route:{layerName}] StackTrace: {ex.StackTrace}");
        }
    }

    // ─── FitBounds: يضبط الـ viewport عشان يشمل نقطتين ──────────────────────
    void FitBounds(double lat1, double lng1, double lat2, double lng2)
    {
        var (x1, y1) = SphericalMercator.FromLonLat(lng1, lat1);
        var (x2, y2) = SphericalMercator.FromLonLat(lng2, lat2);

        double dist = Math.Sqrt(Math.Pow(x2 - x1, 2) + Math.Pow(y2 - y1, 2));

        // FIX: حساب zoom level أفضل بناءً على المسافة الفعلية
        int zoom = dist switch
        {
            < 2000 => 16,
            < 5000 => 15,
            < 10000 => 14,
            < 25000 => 13,
            < 50000 => 12,
            _ => 11
        };

        // FIX: padding من الأطراف - نحرك المركز شوية لفوق عشان الـ info panel بيغطي الجزء السفلي
        double centerX = (x1 + x2) / 2;
        double centerY = (y1 + y2) / 2;
        double verticalBias = (y2 - y1) * 0.15; // bias للأعلى

        MapControl.Map.Navigator.CenterOnAndZoomTo(
            new MPoint(centerX, centerY + verticalBias),
            MapControl.Map.Navigator.Resolutions[Math.Max(zoom, 0)]);
    }

    // ─── Center on single point ───────────────────────────────────────────────
    void CenterOn(double lat, double lng, int zoom)
    {
        var (x, y) = SphericalMercator.FromLonLat(lng, lat);
        MapControl.Map.Navigator.CenterOnAndZoomTo(
            new MPoint(x, y), MapControl.Map.Navigator.Resolutions[zoom]);
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _vm.MapUpdated -= OnMapUpdated;
        _vm.Cleanup();
    }
}