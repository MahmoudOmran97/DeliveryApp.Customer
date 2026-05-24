using DeliveryApp.Customer.ViewModels;
using Mapsui;
using Mapsui.Layers;
using Mapsui.Projections;
using Mapsui.Styles;
using Mapsui.Tiling;
using Mapsui.UI.Maui;
using System.Linq;

namespace DeliveryApp.Customer.Views;

public partial class LocationPickerPage : ContentPage
{
    readonly LocationPickerViewModel _vm;
    MemoryLayer? _selectedLocationLayer;

    public LocationPickerPage(LocationPickerViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;

        SetupMap();
    }

    void SetupMap()
    {
        // إضافة طبقة الخرائط
        MapControl.Map.Layers.Add(OpenStreetMap.CreateTileLayer());

        // المركز الافتراضي (القاهرة)
        var (x, y) = SphericalMercator.FromLonLat(31.2357, 30.0444);
        MapControl.Map.Navigator.CenterOnAndZoomTo(
            new MPoint(x, y),
            MapControl.Map.Navigator.Resolutions[13]);

        // رسم الدبوس الأولي
        DrawLocationPin(30.0444, 31.2357);

        // الحدث الصحيح في إصدار 5.0 هو MapTapped ويستخدم MapEventArgs
        MapControl.MapTapped += OnMapTapped;
    }

    private void OnMapTapped(object? sender, MapEventArgs e)
    {
        // جلب المعلومات من الحدث
        // ملاحظة: GetMapInfo هي extension method موجودة في Mapsui.UI.Maui
        var mapInfo = e.GetMapInfo(MapControl.Map.Layers);

        if (mapInfo?.WorldPosition == null) return;

        var worldPosition = mapInfo.WorldPosition;

        // تحويل الإحداثيات إلى خط طول وعرض
        // تأكدنا من النوع لفك التفكيك (Deconstruction) بشكل صحيح
        MPoint pos = worldPosition;
        var lonLat = SphericalMercator.ToLonLat(pos.X, pos.Y);
        double lon = lonLat.lon;
        double lat = lonLat.lat;

        // تحديث الـ ViewModel
        _vm.SelectedLat = lat;
        _vm.SelectedLng = lon;

        // تحديث مكان الدبوس
        DrawLocationPin(lat, lon);

        // تحديث النص في الواجهة
        if (CoordinatesLabel != null)
        {
            CoordinatesLabel.Text = $"📍 {lat:F4}, {lon:F4}";
        }

        MapControl.Refresh();
    }

    void DrawLocationPin(double lat, double lng)
    {
        if (_selectedLocationLayer != null)
            MapControl.Map.Layers.Remove(_selectedLocationLayer);

        var (x, y) = SphericalMercator.FromLonLat(lng, lat);

        _selectedLocationLayer = new MemoryLayer
        {
            Name = "SelectedLocationLayer",
            Features = new[] { new PointFeature(new MPoint(x, y)) },
            Style = new SymbolStyle
            {
                SymbolScale = 1.2,
                Fill = new Mapsui.Styles.Brush(Mapsui.Styles.Color.FromString("#FF5722")),
                Outline = new Pen(Mapsui.Styles.Color.White, 3)
            }
        };

        MapControl.Map.Layers.Add(_selectedLocationLayer);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        MapControl.Refresh();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        MapControl.MapTapped -= OnMapTapped;
    }
}
