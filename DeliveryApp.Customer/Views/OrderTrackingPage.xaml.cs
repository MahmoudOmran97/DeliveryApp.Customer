using DeliveryApp.Customer.ViewModels;
using Mapsui;
using Mapsui.Layers;
using Mapsui.Projections;
using Mapsui.Styles;
using Mapsui.Tiling;
using Mapsui.UI.Maui;
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
        MapControl.Map.Layers.Add(OpenStreetMap.CreateTileLayer());
        var (x, y) = SphericalMercator.FromLonLat(31.2357, 30.0444);
        MapControl.Map.Navigator.CenterOnAndZoomTo(new MPoint(x, y), MapControl.Map.Navigator.Resolutions[13]);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        MapControl.Refresh();
    }

    void OnMapUpdated()
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            bool hasCustomer = _vm.CustomerLat != 0 && _vm.CustomerLng != 0;
            bool hasRestaurant = _vm.RestaurantLat != 0 && _vm.RestaurantLng != 0;

            if (!_staticPinsDrawn && hasCustomer && hasRestaurant)
            {
                _staticPinsDrawn = true;
                DrawCustomerPin(_vm.CustomerLat, _vm.CustomerLng);
                DrawRestaurantPin(_vm.RestaurantLat, _vm.RestaurantLng);
                await DrawRouteAsync(_vm.RestaurantLat, _vm.RestaurantLng, _vm.CustomerLat, _vm.CustomerLng);
                FitBounds(_vm.RestaurantLat, _vm.RestaurantLng, _vm.CustomerLat, _vm.CustomerLng);
            }

            if (_vm.HasDriver)
                DrawDriverPin(_vm.DriverLat, _vm.DriverLng);

            MapControl.Refresh();
        });
    }



    // دالة إنشاء الطبقة باستخدام رموز برمجية (بدون صور)
    MemoryLayer CreateMarkerLayer(string name, double x, double y, Mapsui.Styles.Color color, SymbolType type)
    {
        return new MemoryLayer
        {
            Name = name,
            Features = new[] { new PointFeature(new MPoint(x, y)) },
            Style = new SymbolStyle
            {
                SymbolScale = 1.2,
                SymbolType = type, // في 5.0 نستخدم SymbolType
                Fill = new Mapsui.Styles.Brush(color),
                Outline = new Pen(Mapsui.Styles.Color.White, 2)
            }
        };
    }

    // تعديل دوال الرسم
    void DrawCustomerPin(double lat, double lng)
    {
        if (_customerLayer != null) MapControl.Map.Layers.Remove(_customerLayer);
        var (x, y) = SphericalMercator.FromLonLat(lng, lat);
        // دائرة زرقاء للعميل
        _customerLayer = CreateMarkerLayer("CustomerLayer", x, y, Mapsui.Styles.Color.Blue, SymbolType.Ellipse);
        MapControl.Map.Layers.Add(_customerLayer);
    }

    void DrawRestaurantPin(double lat, double lng)
    {
        if (_restaurantLayer != null) MapControl.Map.Layers.Remove(_restaurantLayer);
        var (x, y) = SphericalMercator.FromLonLat(lng, lat);
        // مربع أخضر للمطعم
        _restaurantLayer = CreateMarkerLayer("RestaurantLayer", x, y, Mapsui.Styles.Color.Green, SymbolType.Rectangle);
        MapControl.Map.Layers.Add(_restaurantLayer);
    }

    void DrawDriverPin(double lat, double lng)
    {
        if (_driverLayer != null) MapControl.Map.Layers.Remove(_driverLayer);
        var (x, y) = SphericalMercator.FromLonLat(lng, lat);
        // مثلث برتقالي للدليفري
        _driverLayer = CreateMarkerLayer("DriverLayer", x, y, Mapsui.Styles.Color.Orange, SymbolType.Triangle);
        MapControl.Map.Layers.Add(_driverLayer);
    }
    async Task DrawRouteAsync(double fromLat, double fromLng, double toLat, double toLng)
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            var url = $"https://router.project-osrm.org/route/v1/driving/{fromLng},{fromLat};{toLng},{toLat}?overview=full&geometries=geojson";
            var json = await http.GetStringAsync(url);
            var doc = JsonDocument.Parse(json);
            var route = doc.RootElement.GetProperty("routes")[0];

            _vm.Distance = $"{route.GetProperty("distance").GetDouble() / 1000:F1} km";
            _vm.TravelTime = $"{Math.Ceiling(route.GetProperty("duration").GetDouble() / 60)} min";

            var coords = route.GetProperty("geometry").GetProperty("coordinates");
            var points = coords.EnumerateArray().Select(c => {
                var (mx, my) = SphericalMercator.FromLonLat(c[0].GetDouble(), c[1].GetDouble());
                return new MPoint(mx, my);
            }).ToList();

            if (points.Count < 2) return;
            if (_routeLayer != null) MapControl.Map.Layers.Remove(_routeLayer);

            var lineString = new NetTopologySuite.Geometries.LineString(points.Select(p => new NetTopologySuite.Geometries.Coordinate(p.X, p.Y)).ToArray());
            _routeLayer = new MemoryLayer
            {
                Name = "RouteLayer",
                Features = new[] { new Mapsui.Nts.GeometryFeature(lineString) },
                Style = new VectorStyle { Line = new Pen(Mapsui.Styles.Color.FromString("#FF5722"), 4) }
            };
            MapControl.Map.Layers.Insert(1, _routeLayer);
        }
        catch (Exception ex) { Debug.WriteLine(ex.Message); }
    }

    void FitBounds(double lat1, double lng1, double lat2, double lng2)
    {
        var points = new List<MPoint>();
        var (x1, y1) = SphericalMercator.FromLonLat(lng1, lat1);
        var (x2, y2) = SphericalMercator.FromLonLat(lng2, lat2);
        points.Add(new MPoint(x1, y1)); points.Add(new MPoint(x2, y2));

        if (_vm.HasDriver)
        {
            var (dx, dy) = SphericalMercator.FromLonLat(_vm.DriverLng, _vm.DriverLat);
            points.Add(new MPoint(dx, dy));
        }

        double minX = points.Min(p => p.X), maxX = points.Max(p => p.X);
        double minY = points.Min(p => p.Y), maxY = points.Max(p => p.Y);
        double dist = Math.Sqrt(Math.Pow(maxX - minX, 2) + Math.Pow(maxY - minY, 2));
        int zoom = dist switch { < 3000 => 16, < 7000 => 15, < 15000 => 14, < 30000 => 13, _ => 12 };

        MapControl.Map.Navigator.CenterOnAndZoomTo(new MPoint((minX + maxX) / 2, (minY + maxY) / 2), MapControl.Map.Navigator.Resolutions[zoom]);
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _vm.MapUpdated -= OnMapUpdated;
        _vm.Cleanup();
    }
}