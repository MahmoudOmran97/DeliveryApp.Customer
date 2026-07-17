using DeliveryApp.Customer.Models;
using DeliveryApp.Customer.ViewModels;

namespace DeliveryApp.Customer.Views;

public partial class RestaurantPage : ContentPage

{
    // بنخزن هنا مرجع كل قسم (View) لما يتحمّل على الشاشة فعليًا، عشان نقدر نعمل
    // Scroll ليه بعدين لما المستخدم يدوس على أيقونته في الشريط العلوي.
    readonly Dictionary<int, View> _categorySections = new();

    public RestaurantPage(RestaurantViewModel vm) { InitializeComponent(); BindingContext = vm; }

    void OnCategorySectionLoaded(object? sender, EventArgs e)
    {
        if (sender is not View view || view.BindingContext is not Category category) return;
        _categorySections[category.Id] = view;
    }

    // لما المستخدم يدوس على أيقونة القسم في الشريط العلوي، بننزله لنفس القسم
    // جوه الصفحة (ContentScrollView) بحركة سموث.
    async void OnCategoryChipTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is not Category category) return;
        if (!_categorySections.TryGetValue(category.Id, out var view)) return;
        await ContentScrollView.ScrollToAsync(view, ScrollToPosition.Start, true);
    }

}