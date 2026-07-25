using DeliveryApp.Customer.Models;
using DeliveryApp.Customer.ViewModels;

namespace DeliveryApp.Customer.Views;

public partial class RestaurantPage : ContentPage

{
    // بنخزن هنا مرجع كل قسم (View) لما يتحمّل على الشاشة فعليًا، عشان نقدر نعمل
    // Scroll ليه بعدين لما المستخدم يدوس على أيقونته في الشريط العلوي.
    readonly Dictionary<int, View> _categorySections = new();

    // المسافة (من فوق محتوى الاسكرول) اللي لما نوصلها، شريط الكاتوجريز الأصلي
    // بيبقى وصل لحافة الاسكرول فيو — هنا نظهر النسخة العايمة (Sticky).
    double _categoryBarOffsetY = -1;
    bool _isSticky;

    public RestaurantPage(RestaurantViewModel vm) { InitializeComponent(); BindingContext = vm; }

    void OnCategorySectionLoaded(object? sender, EventArgs e)
    {
        if (sender is not View view || view.BindingContext is not Category category) return;
        _categorySections[category.Id] = view;
    }

    // بنقيس مكان شريط الكاتوجريز الأصلي كل ما حجمه/مكانه يتغيّر (مثلاً أول ما يتحمّل،
    // أو لو ارتفاع الكارت اللي فوقه اتغيّر بسبب طول النص)
    void OnInlineCategoriesBarSizeChanged(object? sender, EventArgs e)
    {
        if (sender is not VisualElement view) return;
        if (view.Bounds.Y > 0) _categoryBarOffsetY = view.Bounds.Y;
    }

    // لما اليوزر يعمل Scroll: لو وصل (أو عدى) مكان شريط الكاتوجريز الأصلي، نظهر
    // النسخة العايمة بدل منه؛ لو رجع لفوق تاني، نخفيها.
    void OnContentScrolled(object? sender, ScrolledEventArgs e)
    {
        if (_categoryBarOffsetY <= 0) return;

        var shouldStick = e.ScrollY >= _categoryBarOffsetY;
        if (shouldStick == _isSticky) return;

        _isSticky = shouldStick;
        StickyCategoriesBar.IsVisible = shouldStick;
    }

    // لما المستخدم يدوس على أيقونة القسم في الشريط العلوي (أي نسخة، الأصلية أو العايمة)، بننزله لنفس القسم
    // جوه الصفحة (ContentScrollView) بحركة سموث.
    async void OnCategoryChipTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is not Category category) return;
        if (!_categorySections.TryGetValue(category.Id, out var view)) return;
        await ContentScrollView.ScrollToAsync(view, ScrollToPosition.Start, true);
    }

}