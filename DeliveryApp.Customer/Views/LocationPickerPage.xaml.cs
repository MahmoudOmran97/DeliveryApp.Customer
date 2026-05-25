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

        // رسم الدبوس الأولي (باستخدام شكل برمجي)
        DrawLocationPin(30.0444, 31.2357);

        // الحدث عند الضغط على الخريطة
        MapControl.MapTapped += OnMapTapped;
    }

    private void OnMapTapped(object? sender, MapEventArgs e)
    {
        var mapInfo = e.GetMapInfo(MapControl.Map.Layers);
        if (mapInfo?.WorldPosition == null) return;

        var worldPos = mapInfo.WorldPosition;
        var lonLat = SphericalMercator.ToLonLat(worldPos.X, worldPos.Y);

        // التعديل: استخدام الأسماء الصحيحة الموجودة في الـ ViewModel
        _vm.SelectedLat = lonLat.lat;
        _vm.SelectedLng = lonLat.lon;

        // تحديث الدبوس على الخريطة
        DrawLocationPin(lonLat.lat, lonLat.lon);
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
                SymbolScale = 1.5,
                SymbolType = SymbolType.Ellipse, // استخدام شكل برمجي بدلاً من صورة
                Fill = new Mapsui.Styles.Brush(Mapsui.Styles.Color.Blue),
                Outline = new Pen(Mapsui.Styles.Color.White, 2)
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