using DeliveryApp.Customer.ViewModels;
using Mapsui;
using Mapsui.Layers;
using Mapsui.Projections;
using Mapsui.Styles;
using Mapsui.Tiling;
using Mapsui.UI.Maui;

namespace DeliveryApp.Customer.Views;

public partial class LocationPickerPage : ContentPage
{
    readonly LocationPickerViewModel _vm;
    MemoryLayer? _pinLayer;

    public LocationPickerPage(LocationPickerViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        SetupMap();
    }

    async void SetupMap()
    {
        // تنظيف الـ layers القديمة
        MapControl.Map.Layers.Clear();
        MapControl.Map.Layers.Add(OpenStreetMap.CreateTileLayer());

        // الإحداثيات الافتراضية (القاهرة)
        double lat = 30.0444;
        double lng = 31.2357;

        // ── محاولة الحصول على موقع المستخدم الحقيقي ──
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

                    // تحديث الـ ViewModel بالموقع الحقيقي
                    _vm.SelectedLat = lat;
                    _vm.SelectedLng = lng;
                }
            }
        }
        catch { /* فشل GPS → نبقى على القاهرة */ }

        // ── تمركز الخريطة على الموقع ──
        var (x, y) = SphericalMercator.FromLonLat(lng, lat);
        MapControl.Map.Navigator.CenterOnAndZoomTo(
            new MPoint(x, y),
            MapControl.Map.Navigator.Resolutions[15]);

        DrawLocationPin(lat, lng);
        UpdateLabel(lat, lng);

        MapControl.MapTapped -= OnMapTapped;
        MapControl.MapTapped += OnMapTapped;

        MapControl.Refresh();
    }

    void OnMapTapped(object? sender, MapEventArgs e)
    {
        var info = e.GetMapInfo(MapControl.Map.Layers);
        if (info?.WorldPosition == null) return;

        var lonLat = SphericalMercator.ToLonLat(info.WorldPosition.X, info.WorldPosition.Y);
        _vm.SelectedLat = lonLat.lat;
        _vm.SelectedLng = lonLat.lon;

        DrawLocationPin(lonLat.lat, lonLat.lon);
        UpdateLabel(lonLat.lat, lonLat.lon);
    }

    void DrawLocationPin(double lat, double lng)
    {
        // إزالة الـ layer القديم
        if (_pinLayer != null)
            MapControl.Map.Layers.Remove(_pinLayer);

        var (x, y) = SphericalMercator.FromLonLat(lng, lat);

        // ── الدبوس الخارجي (دائرة برتقالية كبيرة) ──
        var outerStyle = new SymbolStyle
        {
            SymbolType = SymbolType.Ellipse,
            Fill = new Mapsui.Styles.Brush(new Mapsui.Styles.Color(255, 87, 34)),   // #FF5722 برتقالي
            Outline = new Pen(new Mapsui.Styles.Color(255, 255, 255), 3), // حدود بيضاء
            SymbolScale = 1.4,
            Offset = new Offset(0, 0)
        };

        // ── النقطة الداخلية (دائرة بيضاء صغيرة) ──
        var innerStyle = new SymbolStyle
        {
            SymbolType = SymbolType.Ellipse,
            Fill = new Mapsui.Styles.Brush(new Mapsui.Styles.Color(255, 255, 255)), // أبيض
            Outline = new Pen(new Mapsui.Styles.Color(255, 87, 34), 1),
            SymbolScale = 0.5,
            Offset = new Offset(0, 0)
        };

        var feature = new PointFeature(new MPoint(x, y));
        // نضع الـ styles في list
        feature.Styles.Clear();
        feature.Styles.Add(outerStyle);
        feature.Styles.Add(innerStyle);

        _pinLayer = new MemoryLayer
        {
            Name = "PinLayer",
            Features = [feature]
        };

        MapControl.Map.Layers.Add(_pinLayer);
        MapControl.Refresh();
    }

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
        MapControl.MapTapped -= OnMapTapped;
    }
}