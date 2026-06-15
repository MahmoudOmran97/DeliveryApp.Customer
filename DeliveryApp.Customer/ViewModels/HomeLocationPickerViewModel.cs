// ═══════════════════════════════════════════════════════════════
// ViewModels / HomeLocationPickerViewModel.cs
// اختيار الموقع من الـ HomePage – يحفظ في LocationService
// ويعمل Reverse Geocoding عشان يظهر اسم المكان مش الإحداثيات
// ═══════════════════════════════════════════════════════════════
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeliveryApp.Customer.Services;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace DeliveryApp.Customer.ViewModels;

public partial class HomeLocationPickerViewModel : BaseViewModel
{
    readonly LocationService _location;
    readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(10) };

    [ObservableProperty] double _selectedLat = LocationService.ZoneCenterLat;
    [ObservableProperty] double _selectedLng = LocationService.ZoneCenterLng;
    [ObservableProperty] bool _isLocationSelected;
    [ObservableProperty] bool _isOutsideZone;
    [ObservableProperty] string _zoneWarning = string.Empty;

    // اسم المكان اللي هيتظهر في الـ label بعد الاختيار
    [ObservableProperty] string _resolvedAddress = string.Empty;
    [ObservableProperty] bool _isResolvingAddress;

    public HomeLocationPickerViewModel(LocationService location)
    {
        _location = location;

        // لو في موقع محفوظ، ابدأ بيه
        if (_location.HasLocation)
        {
            _selectedLat = _location.Latitude;
            _selectedLng = _location.Longitude;
            _resolvedAddress = _location.AddressLabel; // اللي اتحفظ قبل كده
            _isLocationSelected = true;
        }
    }

    // ── يُستدعى من الـ Page عند تغيير الـ pin ──────────────────
    partial void OnSelectedLatChanged(double v) => _ = OnPinChanged();
    partial void OnSelectedLngChanged(double v) => _ = OnPinChanged();

    async Task OnPinChanged()
    {
        IsLocationSelected = true;
        IsOutsideZone = !_location.IsWithinZone(SelectedLat, SelectedLng);
        ZoneWarning = IsOutsideZone
            ? LocalizationService.Get("ZoneExceededMsg")
            : string.Empty;

        // نعمل Reverse Geocoding عشان نجيب اسم المكان
        await ResolveAddressAsync(SelectedLat, SelectedLng);
    }

    /// <summary>
    /// Nominatim Reverse Geocoding — بيرجع اسم الشارع/الحي/المدينة
    /// مجاني ومش محتاج API Key
    /// </summary>
    async Task ResolveAddressAsync(double lat, double lng)
    {
        IsResolvingAddress = true;
        ResolvedAddress = string.Empty;

        try
        {
            var url = $"https://nominatim.openstreetmap.org/reverse?lat={lat:F6}&lon={lng:F6}&format=json&accept-language=ar";

            _httpClient.DefaultRequestHeaders.Remove("User-Agent");
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "DeliveryApp/1.0");

            var result = await _httpClient.GetFromJsonAsync<NominatimResponse>(url);

            if (result?.Address != null)
            {
                var addr = result.Address;

                // نبني اسم قصير: الحي أو الشارع + المدينة
                var parts = new List<string>();

                // أول جزء: أدق تفاصيل متاحة (حي/ضاحية/شارع)
                var detail = addr.Suburb
                          ?? addr.Neighbourhood
                          ?? addr.Road
                          ?? addr.Village
                          ?? addr.Town;

                if (!string.IsNullOrEmpty(detail))
                    parts.Add(detail);

                // ثاني جزء: المدينة أو المحافظة
                var city = addr.City
                        ?? addr.County
                        ?? addr.State;

                if (!string.IsNullOrEmpty(city) && city != detail)
                    parts.Add(city);

                ResolvedAddress = parts.Count > 0
                    ? string.Join("، ", parts)
                    : $"{lat:F4}, {lng:F4}"; // fallback للإحداثيات لو فشل كل شيء
            }
            else
            {
                ResolvedAddress = $"{lat:F4}, {lng:F4}";
            }
        }
        catch
        {
            // في حالة فشل الـ internet أو أي خطأ، نظهر الإحداثيات كـ fallback
            ResolvedAddress = $"{lat:F4}, {lng:F4}";
        }
        finally
        {
            IsResolvingAddress = false;
        }
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

        // نحفظ اسم المكان (مش الإحداثيات) كـ AddressLabel
        var label = !string.IsNullOrEmpty(ResolvedAddress)
            ? ResolvedAddress
            : $"{SelectedLat:F4}, {SelectedLng:F4}";

        _location.SaveLocation(SelectedLat, SelectedLng, label);

        // رجوع للـ Home
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    static Task GoBack() => Shell.Current.GoToAsync("..");

    // ── Nominatim Response Models ──────────────────────────────
    private class NominatimResponse
    {
        [JsonPropertyName("address")]
        public NominatimAddress? Address { get; set; }
    }

    private class NominatimAddress
    {
        [JsonPropertyName("road")] public string? Road { get; set; }
        [JsonPropertyName("suburb")] public string? Suburb { get; set; }
        [JsonPropertyName("neighbourhood")] public string? Neighbourhood { get; set; }
        [JsonPropertyName("village")] public string? Village { get; set; }
        [JsonPropertyName("town")] public string? Town { get; set; }
        [JsonPropertyName("city")] public string? City { get; set; }
        [JsonPropertyName("county")] public string? County { get; set; }
        [JsonPropertyName("state")] public string? State { get; set; }
        [JsonPropertyName("country")] public string? Country { get; set; }
    }
}