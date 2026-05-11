using DeliveryApp.Customer.ViewModels;
using Mapsui;
using Mapsui.Layers;
using Mapsui.Projections;
using Mapsui.Styles;
using Mapsui.Tiling;
using Mapsui.UI.Maui;

namespace DeliveryApp.Customer.Views;

public partial class OrderTrackingPage : ContentPage
{
    readonly OrderTrackingViewModel _vm;
    readonly Mapsui.Map _map = new();
    readonly MapControl _mapControl = new();  // ✅ بنعمله هنا مش في XAML
    MemoryLayer? _driverLayer;

    public OrderTrackingPage(OrderTrackingViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;

        SetupMap();
        vm.MapUpdated += RefreshDriverPin;
    }

    void SetupMap()
    {
        _map.Layers.Add(OpenStreetMap.CreateTileLayer());
        _mapControl.Map = _map;

        // ✅ حط الـ MapControl في الـ placeholder
        MapContainer.Content = _mapControl;

        var (x, y) = SphericalMercator.FromLonLat(32.8998, 24.0889); // أسوان
        _map.Navigator.CenterOnAndZoomTo(new MPoint(x, y), _map.Navigator.Resolutions[14]);
    }

    void RefreshDriverPin()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (_driverLayer != null)
                _map.Layers.Remove(_driverLayer);

            var (x, y) = SphericalMercator.FromLonLat(_vm.DriverLng, _vm.DriverLat);
            var point = new MPoint(x, y);

            _driverLayer = new MemoryLayer
            {
                Name = "DriverLayer",
                Features = new[] { new PointFeature(point) },
                Style = new SymbolStyle
                {
                    SymbolScale = 0.8,
                    Fill = new Mapsui.Styles.Brush(Mapsui.Styles.Color.FromString("#FF5722"))
                }
            };

            _map.Layers.Add(_driverLayer);
            _map.Navigator.CenterOnAndZoomTo(point, _map.Navigator.Resolutions[15]);
            _mapControl.Refresh();
        });
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _vm.MapUpdated -= RefreshDriverPin;
        _vm.Cleanup();
    }
}