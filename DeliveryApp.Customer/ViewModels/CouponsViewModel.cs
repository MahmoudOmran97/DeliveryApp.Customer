using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeliveryApp.Customer.Models;
using DeliveryApp.Customer.Services;

namespace DeliveryApp.Customer.ViewModels;

public partial class CouponsViewModel : BaseViewModel
{
    readonly ApiService _api;

    [ObservableProperty] bool _isRefreshing;
    [ObservableProperty] string _manualCode = string.Empty;
    [ObservableProperty] string _feedbackMessage = string.Empty;
    [ObservableProperty] bool _hasFeedback;
    [ObservableProperty] bool _feedbackIsError;
    [ObservableProperty] bool _isCopied;
    [ObservableProperty] string _currentTab = "Valid"; // Valid, Used, Expired

    public ObservableCollection<Coupon> Coupons { get; } = new();
    public ObservableCollection<Coupon> FilteredCoupons { get; } = new();

    public CouponsViewModel(ApiService api) => _api = api;

    [RelayCommand]
    async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            var list = await _api.GetMyCouponsAsync();
            Coupons.Clear();
            foreach (var c in list ?? new()) Coupons.Add(c);
            ApplyFilter();
        }
        finally { IsBusy = false; IsRefreshing = false; }
    }

    [RelayCommand]
    void ChangeTab(string tab)
    {
        CurrentTab = tab;
        ApplyFilter();
    }

    void ApplyFilter()
    {
        FilteredCoupons.Clear();
        var filtered = CurrentTab switch
        {
            "Valid" => Coupons.Where(c => c.Status == "Available"),
            "Used" => Coupons.Where(c => c.Status == "Used"),
            "Expired" => Coupons.Where(c => c.Status == "Expired"),
            _ => Coupons
        };
        foreach (var c in filtered) FilteredCoupons.Add(c);
    }

    [RelayCommand]
    async Task RefreshAsync() { IsRefreshing = true; await LoadAsync(); }

    [RelayCommand]
    async Task CopyCodeAsync(string code)
    {
        if (string.IsNullOrWhiteSpace(code)) return;
        await Clipboard.Default.SetTextAsync(code);
        IsCopied = true;
        ShowFeedback($"تم نسخ الكود: {code}", isError: false);
        await Task.Delay(2000);
        IsCopied = false;
    }

    [RelayCommand]
    async Task ValidateManualCodeAsync()
    {
        if (string.IsNullOrWhiteSpace(ManualCode)) return;
        IsBusy = true;
        try
        {
            var result = await _api.ValidateCouponAsync(ManualCode.Trim().ToUpper(), 200);
            ShowFeedback($"✅ {result?.Title} — خصم {result?.Discount:F0} جنيه", isError: false);
        }
        catch (ApiService.ApiException ex)
        {
            ShowFeedback(ex.Message, isError: true);
        }
        finally { IsBusy = false; }
    }

    void ShowFeedback(string msg, bool isError)
    {
        FeedbackMessage = msg;
        FeedbackIsError = isError;
        HasFeedback = true;
    }
}
