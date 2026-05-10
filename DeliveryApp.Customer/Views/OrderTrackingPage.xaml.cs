using DeliveryApp.Customer.ViewModels;

using Microsoft.Maui.Controls.Maps;

using Microsoft.Maui.Maps;
using System.Net.NetworkInformation;

namespace DeliveryApp.Customer.Views;

public partial class OrderTrackingPage : ContentPage

{

    readonly OrderTrackingViewModel _vm;

    Pin? _driverPin;

    public OrderTrackingPage(OrderTrackingViewModel vm)

    {

        InitializeComponent();

        _vm = vm;

        BindingContext = vm;

        vm.MapUpdated += RefreshDriverPin;

    }

    void RefreshDriverPin()

    {

        if (_driverPin == null)

        {

            _driverPin = new Pin { Label = "🛵 Driver", Type = PinType.Generic };

            MapView.Pins.Add(_driverPin);

        }

        _driverPin.Location = new Location(_vm.DriverLat, _vm.DriverLng);

        MapView.MoveToRegion(MapSpan.FromCenterAndRadius(

            new Location(_vm.DriverLat, _vm.DriverLng),

            Distance.FromKilometers(1.5)));

    }

    protected override void OnDisappearing()

    {

        base.OnDisappearing();

        _vm.Cleanup();

    }

}

