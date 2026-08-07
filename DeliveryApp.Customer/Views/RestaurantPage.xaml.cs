using DeliveryApp.Customer.Models;
using DeliveryApp.Customer.ViewModels;

namespace DeliveryApp.Customer.Views;

public partial class RestaurantPage : ContentPage

{
    // المسافة (من فوق محتوى الـ CollectionView) اللي لما نوصلها، شريط الكاتوجريز الأصلي
    // بيبقى وصل لحافة الشاشة — هنا نظهر النسخة العايمة (Sticky).
    double _categoryBarOffsetY = -1;
    bool _isSticky;

    public RestaurantPage(RestaurantViewModel vm) { InitializeComponent(); BindingContext = vm; }

    // بنقيس مكان شريط الكاتوجريز الأصلي كل ما حجمه/مكانه يتغيّر (مثلاً أول ما يتحمّل،
    // أو لو ارتفاع الكارت اللي فوقه اتغيّر بسبب طول النص)
    void OnInlineCategoriesBarSizeChanged(object? sender, EventArgs e)
    {
        if (sender is not VisualElement view) return;
        if (view.Bounds.Y > 0) _categoryBarOffsetY = view.Bounds.Y;
    }

    // لما اليوزر يعمل Scroll: لو وصل (أو عدى) مكان شريط الكاتوجريز الأصلي، نظهر
    // النسخة العايمة بدل منه؛ لو رجع لفوق تاني، نخفيها.
    // ✅ CollectionView.Scrolled بيرجّع ItemsViewScrolledEventArgs (مش ScrolledEventArgs
    // بتاعة ScrollView) — الخاصية اللي بتدّينا الأوفست هي VerticalOffset.
    void OnContentScrolled(object? sender, ItemsViewScrolledEventArgs e)
    {
        if (_categoryBarOffsetY <= 0) return;

        var shouldStick = e.VerticalOffset >= _categoryBarOffsetY;
        if (shouldStick == _isSticky) return;

        _isSticky = shouldStick;
        StickyCategoriesBar.IsVisible = shouldStick;
    }

    // لما المستخدم يدوس على أيقونة القسم في الشريط العلوي (أي نسخة، الأصلية أو العايمة)،
    // بننزله لبداية نفس المجموعة (Group) جوه الـ CollectionView الرئيسي بحركة سموث.
    // ✅ بعد التحويل لـ CollectionView واحد بـ IsGrouped، بقينا بنلاقي index المجموعة
    // بدل ما كنا بنمسك View فعلي للقسم زي قبل.
    void OnCategoryChipTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is not Category category) return;
        if (BindingContext is not RestaurantViewModel vm) return;

        var groupIndex = -1;
        for (var i = 0; i < vm.MenuGroups.Count; i++)
        {
            if (vm.MenuGroups[i].Key.Id == category.Id) { groupIndex = i; break; }
        }
        if (groupIndex < 0) return;

        MenuCollectionView.ScrollTo(0, groupIndex, ScrollToPosition.Start, true);
    }

}
