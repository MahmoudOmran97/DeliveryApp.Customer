using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace DeliveryApp.Customer.ViewModels;

public partial class LocationPickerViewModel : BaseViewModel
{
    [ObservableProperty] double _selectedLat = 30.0444;
    [ObservableProperty] double _selectedLng = 31.2357;
    [ObservableProperty] bool _isLocationSelected = false;

    partial void OnSelectedLatChanged(double v) => IsLocationSelected = true;

    [RelayCommand]
    async Task ConfirmLocation()
    {
        // ✅ FIX: نفس مشكلة deliveryFee — لازم InvariantCulture عشان الفاصلة
        // العشرية تفضل "." مش "٫" لو اللغة عربي، وإلا الـ query string بيتبعت غلط.
        var lat = SelectedLat.ToString(CultureInfo.InvariantCulture);
        var lng = SelectedLng.ToString(CultureInfo.InvariantCulture);
        // العودة لصفحة الشيك أوت مع إرسال الإحداثيات في الرابط
        await Shell.Current.GoToAsync($"..?lat={lat}&lng={lng}");
    }
}