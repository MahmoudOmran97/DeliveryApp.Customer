using DeliveryApp.Customer.Models;
using DeliveryApp.Customer.Services;
using DeliveryApp.Customer.ViewModels;

namespace DeliveryApp.Customer.Views;

public partial class RestaurantPage : ContentPage

{
    // المسافة (من فوق محتوى الـ CollectionView) اللي لما نوصلها، شريط الكاتوجريز الأصلي
    // بيبقى وصل لحافة الشاشة — هنا نظهر النسخة العايمة (Sticky) + خلفية الشريط
    // الثابت فوق بتتحول لأبيض في نفس اللحظة (زي طلبات بالظبط).
    double _categoryBarOffsetY = -1;
    bool _isSticky;

    // ✅ نفس فكرة الـ Sticky بتاعة صفحة المطعم العادي، لكن هنا لصفحة السوبر
    // ماركت/الصيدلية (ScrollView مش CollectionView).
    double _groceryStickyOffsetY = -1;
    bool _isGroceryStuck;

    public RestaurantPage(RestaurantViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;

        // ✅ FIX (تصادم الأيقونات مع الساعة/الشحن/الشبكة): نظبط هوامش الشريط
        // الثابت فوق أول ما الصفحة تتحمّل بأي قيمة إنسيت متاحة وقتها، وبعدين
        // نعيد الضبط تاني أوتوماتيك أول ما القياس الحقيقي من المنصة يوصل.
        ApplyTopSafeMargins();
        SafeAreaService.TopInsetChanged += OnSafeAreaInsetChanged;
    }

    void OnSafeAreaInsetChanged(object? sender, EventArgs e)
        => Dispatcher.Dispatch(ApplyTopSafeMargins);

    protected override void OnNavigatedFrom(NavigatedFromEventArgs args)
    {
        base.OnNavigatedFrom(args);
        SafeAreaService.TopInsetChanged -= OnSafeAreaInsetChanged;
    }

    // ✅ FIX: بدل الهامش الثابت (45) اللي كان متخمّن على قد شريط حالة "متوسط"،
    // بنبني هوامش الشريط الثابت فوق دلوقتي على أساس ارتفاع شريط الحالة الحقيقي
    // لجهاز المستخدم (SafeAreaService.TopInset)، فمفيش أي أيقونة بتتلزّق في
    // الساعة/الشحن/الشبكة أو بتبان بعيدة أوي عنهم على أي جهاز.
    const double IconGap = 12;     // مسافة الأيقونات تحت شريط الحالة
    const double IconSize = 42;
    const double BottomPadding = 12; // مسافة تحت صف الأيقونات قبل ما شريط الكاتوجريز يبدأ

    double _toolbarHeight = 92;

    // ✅ FIX: مسافة بسيطة بين صف الأيقونات (رجوع/بحث/سلة) وشريط البحث لما يفتح،
    // عشان يبان واضح إنه "نازل تحتهم" مش ملزّق فيهم.
    const double SearchBarGap = 6;

    void ApplyTopSafeMargins()
    {
        var inset = SafeAreaService.TopInset;
        var rowTop = inset + IconGap;

        BackButtonImage.Margin = new Thickness(16, rowTop, 0, 0);
        CartIconGrid.Margin = new Thickness(0, rowTop, 16, 0);
        SearchIconBorder.Margin = new Thickness(0, rowTop, 66, 0);

        _toolbarHeight = rowTop + IconSize + BottomPadding;
        ToolbarBackgroundFill.HeightRequest = _toolbarHeight;

        // ✅ FIX (كان بيغطي على زرار الرجوع/السلة): شريط البحث دلوقتي بيبدأ من
        // تحت صف الأيقونات مباشرة (رجوع/بحث/سلة) بدل ما يكون في نفس ارتفاعهم،
        // فلما يفتح بينزل تحتهم بدل ما يتراكب فوقهم.
        var searchBarTop = rowTop + IconSize + SearchBarGap;
        SearchBarBorder.Margin = new Thickness(16, searchBarTop, 16, 0);

        // الشريط العايم (Sticky) لازم يبدأ بالظبط من تحت الشريط الثابت فوق.
        StickyCategoriesBar.Margin = new Thickness(0, _toolbarHeight, 0, 0);
    }

    // بنقيس مكان شريط الكاتوجريز الأصلي كل ما حجمه/مكانه يتغيّر (مثلاً أول ما يتحمّل،
    // أو لو ارتفاع الكارت اللي فوقه اتغيّر بسبب طول النص)
    void OnInlineCategoriesBarSizeChanged(object? sender, EventArgs e)
    {
        if (sender is not VisualElement view) return;
        if (view.Bounds.Y > 0) _categoryBarOffsetY = view.Bounds.Y;
    }

    // لما اليوزر يعمل Scroll: لو وصل (أو عدى) مكان شريط الكاتوجريز الأصلي، نظهر
    // النسخة العايمة بدل منه + نحوّل خلفية الشريط الثابت فوق لأبيض في نفس اللحظة.
    // ✅ CollectionView.Scrolled بيرجّع ItemsViewScrolledEventArgs (مش ScrolledEventArgs
    // بتاعة ScrollView) — الخاصية اللي بتدّينا الأوفست هي VerticalOffset.
    void OnContentScrolled(object? sender, ItemsViewScrolledEventArgs e)
    {
        UpdateToolbarBackground(e.VerticalOffset);

        if (_categoryBarOffsetY <= 0) return;

        var shouldStick = e.VerticalOffset >= _categoryBarOffsetY;
        if (shouldStick == _isSticky) return;

        _isSticky = shouldStick;

        // ✅ FIX: لو شريط البحث فاتح، منسيبش سكرول الصفحة يظهر النسخة العايمة
        // فوق شريط البحث (كانت هي سبب مشكلة التسريب البصري). بنسجّل الحالة
        // بس (_isSticky) ونطبّقها فعليًا لما البحث يتقفل.
        if (BindingContext is RestaurantViewModel vm && vm.IsSearchActive) return;

        StickyCategoriesBar.IsVisible = shouldStick;
    }

    // بنقيس ارتفاع كارت المعلومات بتاع السوبر ماركت/الصيدلية — لما اليوزر ينزل
    // اسكرول بمقدار (ارتفاع الغلاف + ارتفاع الكارت)، معناه وصل لحافة الشاشة
    // فنظهر النسخة العايمة.
    void OnGroceryInfoCardSizeChanged(object? sender, EventArgs e)
    {
        if (sender is not VisualElement view) return;
        if (view.Height > 0) _groceryStickyOffsetY = 220 + view.Height;
    }

    // ScrollView.Scrolled بيرجّع ScrolledEventArgs بتاعة الـ ScrollY (مش VerticalOffset
    // زي CollectionView.Scrolled).
    void OnGroceryScrolled(object? sender, ScrolledEventArgs e)
    {
        UpdateToolbarBackground(e.ScrollY);

        if (_groceryStickyOffsetY <= 0) return;

        var shouldStick = e.ScrollY >= _groceryStickyOffsetY;
        if (shouldStick == _isGroceryStuck) return;

        _isGroceryStuck = shouldStick;

        if (BindingContext is RestaurantViewModel vm && vm.IsSearchActive) return;

        StickyCategoriesBar.IsVisible = shouldStick;
    }

    // ✅ FIX (زي طلبات فعلاً): بعد ما شفنا اسكرين شوتس طلبات، الغلاف مبيتقفلش/
    // يصغّر — هو بيسكرول عادي زي أي عنصر (تم نقله جوه CollectionView.Header/
    // ScrollView Content). اللي بيتغيّر هنا بس هو خلفية الشريط الثابت فوق:
    // شفافة تمامًا فوق الغلاف، وبتتحول لأبيض بسلاسة (Fade على مدى 60px) قبل
    // ما نوصل بالظبط للحظة اللي شريط الكاتوجريز العايم (فيه اسم المحل) بيظهر
    // فيها — فبيبقى حاسس إنه انتقال واحد متناسق مش حاجتين منفصلين.
    const double ToolbarFadeDistance = 60;

    void UpdateToolbarBackground(double offset)
    {
        var threshold = _categoryBarOffsetY > 0 ? _categoryBarOffsetY
                       : _groceryStickyOffsetY > 0 ? _groceryStickyOffsetY
                       : 220;

        var fadeStart = Math.Max(0, threshold - ToolbarFadeDistance);
        var t = Math.Clamp((offset - fadeStart) / ToolbarFadeDistance, 0, 1);
        ToolbarBackgroundFill.Opacity = t * (2 - t); // ease-out بسيط لسلاسة بصرية أكتر
    }

    // لما المستخدم يدوس على أيقونة القسم في الشريط العلوي (أي نسخة، الأصلية أو العايمة)،
    // بننزله لبداية نفس المجموعة (Group) جوه الـ CollectionView الرئيسي بحركة سموث.
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

    // ✅ FIX (زرار البحث مش سلس): كان بيتحكم فيه بس بـ IsVisible Binding — يعني
    // ظهور/اختفاء فجائي (قفشة) من غير أي حركة. دلوقتي بندي الزرار حركة Fade +
    // Scale لطيفة (فتح وقفل)، ولحد ما الحركة تخلص بنستدعي أمر الـ ViewModel
    // (ToggleSearchCommand) اللي بيعمل التوجل الفعلي على IsSearchActive/SearchQuery.
    bool _isSearchAnimating;

    async void OnSearchIconTapped(object? sender, TappedEventArgs e)
    {
        if (_isSearchAnimating) return;
        if (BindingContext is not RestaurantViewModel vm) return;

        _isSearchAnimating = true;
        try
        {
            var opening = !vm.IsSearchActive;

            if (opening)
            {
                vm.ToggleSearchCommand.Execute(null);
                SearchBarBorder.InputTransparent = false;
                SearchBarBorder.IsVisible = true;

                // ✅ FIX (تسريب نص من ورا شريط البحث لما يتفتح والصفحة متعمّلها
                // اسكرول): النسخة العايمة من اسم المحل/الأقسام (StickyCategoriesBar)
                // بتبدأ من نفس نقطة بداية شريط البحث تقريبًا، فكان شريط البحث
                // (ارتفاعه 42 بس) بيغطي أول جزء منها بس والباقي (اسم المحل/
                // الأقسام) كان بيبان "طالع" من تحته أو حواليه. دلوقتي بنخفيها
                // تمامًا طول ما شريط البحث فاتح، وبنرجعها زي ما كانت لما يتقفل.
                StickyCategoriesBar.IsVisible = false;

                await Task.WhenAll(
                    SearchBarBorder.FadeTo(1, 180, Easing.CubicOut),
                    SearchBarBorder.ScaleTo(1, 180, Easing.CubicOut),
                    SearchIconBorder.FadeTo(0, 140, Easing.CubicOut));

                SearchIconBorder.InputTransparent = true;
                SearchEntry.Focus();
            }
            else
            {
                SearchEntry.Unfocus();
                SearchIconBorder.InputTransparent = false;

                await Task.WhenAll(
                    SearchBarBorder.FadeTo(0, 150, Easing.CubicIn),
                    SearchBarBorder.ScaleTo(0.92, 150, Easing.CubicIn),
                    SearchIconBorder.FadeTo(1, 180, Easing.CubicOut));

                SearchBarBorder.InputTransparent = true;
                vm.ToggleSearchCommand.Execute(null);

                // بعد ما شريط البحث يقفل، نرجّع النسخة العايمة تظهر تاني لو
                // اليوزر كان أصلاً واصل لمكان الالتصاق (Sticky) وقت ما فتح البحث.
                StickyCategoriesBar.IsVisible = _isSticky || _isGroceryStuck;
            }
        }
        finally
        {
            _isSearchAnimating = false;
        }
    }

}