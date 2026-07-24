using CommunityToolkit.Mvvm.ComponentModel;

using CommunityToolkit.Mvvm.Input;

using DeliveryApp.Customer.Models;

using DeliveryApp.Customer.Services;

namespace DeliveryApp.Customer.ViewModels;

[QueryProperty(nameof(OrderId), "orderId")]

public partial class OrderDetailViewModel : BaseViewModel

{

    readonly ApiService _api;

    [ObservableProperty] int _orderId;

    [ObservableProperty] Order? _order;

    [ObservableProperty] bool _showRating;

    // ── بيبدأو من 0 (مفيش تقييم لسه) بدل 5 عشان النجوم تبان فاضية
    // لحد ما المستخدم يدوس عليها، أو لحد ما نحمّل تقييم سابق محفوظ ──
    [ObservableProperty] int _restaurantRating;

    [ObservableProperty] int _driverRating;

    [ObservableProperty] string _comment = string.Empty;

    public OrderDetailViewModel(ApiService api) { _api = api; }

    partial void OnOrderIdChanged(int v) => LoadCommand.Execute(null);

    [RelayCommand]

    async Task LoadAsync()

    {

        if (OrderId == 0) return;

        IsBusy = true;

        try
        {
            Order = await _api.GetOrderAsync(OrderId);

            // ✅ FIX: لو الأوردر ده اتقيم قبل كده، بنعرض التقييم المحفوظ
            // بالنجوم على طول بدل ما نسيب الفورم فاضي وكأنه لسه مش متقيّم
            if (Order?.Rating != null)
            {
                RestaurantRating = Order.Rating.RestaurantRating;
                DriverRating = Order.Rating.DriverRating ?? 0;
                Comment = Order.Rating.Comment ?? string.Empty;
                ShowRating = false;
            }
        }
        finally { IsBusy = false; }

    }

    [RelayCommand]

    async Task Cancel()

    {

        if (Order == null) return;

        var reason = await Shell.Current.DisplayPromptAsync("Cancel Order", "Reason (optional):");

        if (await _api.CancelOrderAsync(Order.Id, reason)) await LoadAsync();

        else await AlertAsync("Could not cancel at this stage");

    }

    [RelayCommand]
    void Rate()
    {
        if (Order?.IsRated == true) return; // ✅ متقيّم قبل كده، منفتحش الفورم تاني
        RestaurantRating = 0;
        DriverRating = 0;
        Comment = string.Empty;
        ShowRating = true;
    }

    // ── تقييم المطعم/السواق بالنجوم (تاب على نجمة = القيمة دي) ──
    [RelayCommand] void SetRestaurantRating(string star) => RestaurantRating = int.Parse(star);

    [RelayCommand] void SetDriverRating(string star) => DriverRating = int.Parse(star);

    [RelayCommand]

    async Task SubmitRating()

    {

        if (Order == null) return;

        if (RestaurantRating < 1)
        {
            await AlertAsync("من فضلك اختر تقييم للمطعم أولاً");
            return;
        }

        IsBusy = true;
        try
        {
            var success = await _api.RateOrderAsync(
                Order.Id, RestaurantRating, DriverRating > 0 ? DriverRating : null, Comment);

            if (success)
            {
                ShowRating = false;
                await AlertAsync("شكرًا لتقييمك!");
                await LoadAsync(); // ✅ يرجع يحمّل الأوردر عشان يبان التقييم بالنجوم على طول
            }
        }
        catch (ApiService.ApiException ex)
        {
            // ✅ FIX: كان بيرمي استثناء غير متوقع (مثلاً "Order already rated")
            // ويكسر الصفحة. دلوقتي بنعرض رسالة واضحة ونحدّث الشاشة بدل الكراش
            ShowRating = false;
            await AlertAsync(ex.Message);
            await LoadAsync();
        }
        finally { IsBusy = false; }

    }

}