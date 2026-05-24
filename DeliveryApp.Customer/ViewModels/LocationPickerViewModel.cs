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
        // العودة لصفحة الشيك أوت مع إرسال الإحداثيات في الرابط
        await Shell.Current.GoToAsync($"..?lat={SelectedLat}&lng={SelectedLng}");
    }
}