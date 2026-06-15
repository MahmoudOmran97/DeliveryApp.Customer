// ═══════════════════════════════════════════════════════════════
// ViewModels / HomeLocationPickerViewModel.cs
// اختيار الموقع من الـ HomePage – يحفظ في LocationService
// ويتحقق من الـ 10km zone
// ═══════════════════════════════════════════════════════════════
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeliveryApp.Customer.Services;

namespace DeliveryApp.Customer.ViewModels;

public partial class HomeLocationPickerViewModel : BaseViewModel
{
    readonly LocationService _location;

    [ObservableProperty] double _selectedLat = LocationService.ZoneCenterLat;
    [ObservableProperty] double _selectedLng = LocationService.ZoneCenterLng;
    [ObservableProperty] bool   _isLocationSelected;
    [ObservableProperty] bool   _isOutsideZone;
    [ObservableProperty] string _zoneWarning = string.Empty;

    public HomeLocationPickerViewModel(LocationService location)
    {
        _location = location;

        // لو في موقع محفوظ، ابدأ بيه
        if (_location.HasLocation)
        {
            _selectedLat = _location.Latitude;
            _selectedLng = _location.Longitude;
            _isLocationSelected = true;
        }
    }

    // ── يُستدعى من الـ Page عند تغيير الـ pin ──────────────────
    partial void OnSelectedLatChanged(double v) => ValidateZone();
    partial void OnSelectedLngChanged(double v) => ValidateZone();

    void ValidateZone()
    {
        IsLocationSelected = true;
        IsOutsideZone = !_location.IsWithinZone(SelectedLat, SelectedLng);
        ZoneWarning = IsOutsideZone
            ? LocalizationService.Get("ZoneExceededMsg")
            : string.Empty;
    }

    // ── تأكيد الموقع والرجوع للـ Home ─────────────────────────
    [RelayCommand]
    async Task ConfirmLocation()
    {
        if (IsOutsideZone)
        {
            await AlertAsync(LocalizationService.Get("ZoneExceededMsg"));
            return;
        }

        var label = $"{SelectedLat:F4}, {SelectedLng:F4}";
        _location.SaveLocation(SelectedLat, SelectedLng, label);

        // رجوع للـ Home
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    static Task GoBack() => Shell.Current.GoToAsync("..");
}
