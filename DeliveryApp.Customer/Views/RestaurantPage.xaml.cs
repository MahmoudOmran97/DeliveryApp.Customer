using DeliveryApp.Customer.Models;
using DeliveryApp.Customer.ViewModels;

namespace DeliveryApp.Customer.Views;

public partial class RestaurantPage : ContentPage

{
    // المسافة (من فوق محتوى الـ CollectionView) اللي لما نوصلها، شريط الكاتوجريز الأصلي
    // بيبقى وصل لحافة الشاشة — هنا نظهر النسخة العايمة (Sticky).
    double _categoryBarOffsetY = -1;
    bool _isSticky;

    // ✅ FIX: نفس فكرة الـ Sticky بتاعة صفحة المطعم العادي، لكن هنا لصفحة
    // السوبر ماركت/الصيدلية (ScrollView مش CollectionView). قبل كده مكنش فيه
    // أي Scrolled handler هنا خالص، فالشريط العايم (واسم المحل) كان يفضل مختفي
    // دايمًا حتى لو اليوزر نزل لتحت خالص.
    double _groceryStickyOffsetY = -1;
    bool _isGroceryStuck;

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
        UpdateCoverCollapse(e.VerticalOffset);

        if (_categoryBarOffsetY <= 0) return;

        var shouldStick = e.VerticalOffset >= _categoryBarOffsetY;
        if (shouldStick == _isSticky) return;

        _isSticky = shouldStick;
        StickyCategoriesBar.IsVisible = shouldStick;
    }

    // بنقيس ارتفاع كارت المعلومات بتاع السوبر ماركت/الصيدلية — لما اليوزر ينزل
    // اسكرول بمقدار ارتفاع الكارت ده، معناه وصل لحافة الشاشة فنظهر النسخة العايمة.
    void OnGroceryInfoCardSizeChanged(object? sender, EventArgs e)
    {
        if (sender is not VisualElement view) return;
        if (view.Height > 0) _groceryStickyOffsetY = view.Height;
    }

    // ScrollView.Scrolled بيرجّع ScrolledEventArgs بتاعة الـ ScrollY (مش VerticalOffset
    // زي CollectionView.Scrolled).
    void OnGroceryScrolled(object? sender, ScrolledEventArgs e)
    {
        UpdateCoverCollapse(e.ScrollY);

        if (_groceryStickyOffsetY <= 0) return;

        var shouldStick = e.ScrollY >= _groceryStickyOffsetY;
        if (shouldStick == _isGroceryStuck) return;

        _isGroceryStuck = shouldStick;
        StickyCategoriesBar.IsVisible = shouldStick;
    }

    // ✅ FIX (تصغير الغلاف فعليًا زي طلبات): الحل اللي قبل كده كان بيخفت الصورة
    // بس (Opacity) والمساحة المحجوزة (220) فاضلة زي ما هي — يعني حتى لو الصورة
    // اختفت، المستطيل الفاضي مكانه لسه واخد نفس المساحة. دلوقتي بنربط ارتفاع
    // الغلاف (CoverBorder.HeightRequest) والمسافة اللي بيبدأ منها المحتوى
    // (Margin) مباشرة بنفس أوفست السكرول (1:1 من غير Animation)، فالغلاف نفسه
    // بيصغّر فعليًا وهو بينزل لحد ما يوصل لأقل ارتفاع (92) بيفضل شايل فيه بس
    // صف الأيقونات — بالظبط زي الشريط المضغوط اللي بيبان في طلبات بعد ما تنزل.
    // بما إن المحتوى (CollectionView/ScrollView) بيتحرك بنفس القيمة بالظبط،
    // فمفيش أي Overlap جديد بيحصل، فمفيش رجوع لمشكلة التداخل القديمة.
    const double CoverExpandedHeight = 220;
    const double CoverCollapsedHeight = 92;
    const double CoverCollapseRange = CoverExpandedHeight - CoverCollapsedHeight;

    void UpdateCoverCollapse(double offset)
    {
        var newHeight = Math.Max(CoverCollapsedHeight, CoverExpandedHeight - offset);
        CoverBorder.HeightRequest = newHeight;
        MenuCollectionView.Margin = new Thickness(0, newHeight, 0, 0);
        GroceryScrollView.Margin = new Thickness(0, newHeight, 0, 0);

        var fadeOpacity = 1 - Math.Clamp(offset / CoverCollapseRange, 0, 1);
        CoverImage.Opacity = fadeOpacity;
        CoverFallback.Opacity = fadeOpacity;
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
