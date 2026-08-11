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
    const double SearchBarHeight = 42;
    // ✅ FEATURE: مسافة تحت شريط البحث لما الخلفية بتتمد لتحته، عشان مايبقاش
    // ملزّق في المحتوى اللي تحته.
    const double SearchBarBottomGap = 14;

    // ارتفاع الخلفية الثابتة فوق وهي متمدة عشان تغطي شريط البحث لما يكون فاتح.
    double _searchExpandedToolbarHeight = 92;

    // ارتفاع StickyCategoriesBar الفعلي بعد ما يترندر (بيتغيّر حسب طول اسم
    // المحل وعدد الأقسام)، بنستخدمه عشان نحط عنوان القسم الحالي (CurrentGroupBar)
    // بالظبط تحته من غير ما يتراكبوا.
    double _stickyCategoriesBarHeight = 178;

    void OnStickyCategoriesBarSizeChanged(object? sender, EventArgs e)
    {
        if (sender is not VisualElement view) return;
        if (view.Height <= 0) return;
        _stickyCategoriesBarHeight = view.Height;
        RepositionCurrentGroupBar();
    }

    void RepositionCurrentGroupBar()
    {
        // لما شريط الأقسام العايم (StickyCategoriesBar) ظاهر، عنوان القسم بيبان
        // تحته. لو لسه مش ظاهر (يعني لسه في أول الصفحة)، بيبان تحت التولبار على طول.
        var baseTop = StickyCategoriesBar.IsVisible ? _toolbarHeight + _stickyCategoriesBarHeight : _toolbarHeight;
        CurrentGroupBar.Margin = new Thickness(0, baseTop, 0, 0);
    }

    void ApplyTopSafeMargins()
    {
        var inset = SafeAreaService.TopInset;
        var rowTop = inset + IconGap;

        BackButtonImage.Margin = new Thickness(16, rowTop, 0, 0);
        CartIconGrid.Margin = new Thickness(0, rowTop, 16, 0);
        SearchIconBorder.Margin = new Thickness(0, rowTop, 66, 0);

        _toolbarHeight = rowTop + IconSize + BottomPadding;
        if (!IsSearchCurrentlyActive())
            ToolbarBackgroundFill.HeightRequest = _toolbarHeight;

        // ✅ FIX (كان بيغطي على زرار الرجوع/السلة): شريط البحث دلوقتي بيبدأ من
        // تحت صف الأيقونات مباشرة (رجوع/بحث/سلة) بدل ما يكون في نفس ارتفاعهم،
        // فلما يفتح بينزل تحتهم بدل ما يتراكب فوقهم.
        // ✅ FIX: بيمتد بعرض الشاشة بالكامل (Margin أفقي = 0) بدل ما يكون فيه
        // فراغ جانبي — بيبدأ بالظبط من تحت زرار الرجوع لحد تحت زرار السلة.
        var searchBarTop = rowTop + IconSize + SearchBarGap;
        SearchBarBorder.Margin = new Thickness(0, searchBarTop, 0, 0);

        // ✅ FEATURE: لما شريط البحث يفتح، خلفية الشريط الثابت فوق (اللي بتغطي
        // من تحت زرار الرجوع لحد زرار السلة أفقيًا) لازم تتمد لتحت كمان عشان
        // تحضن شريط البحث بدل ما يفضل عايم من غير خلفية وراه.
        _searchExpandedToolbarHeight = searchBarTop + SearchBarHeight + SearchBarBottomGap;

        // الشريط العايم (Sticky) لازم يبدأ بالظبط من تحت الشريط الثابت فوق.
        StickyCategoriesBar.Margin = new Thickness(0, _toolbarHeight, 0, 0);
    }

    bool IsSearchCurrentlyActive() => BindingContext is RestaurantViewModel vm && vm.IsSearchActive;

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
    // آخر أوفست سكرول اتسجل (من أي مصدر: القايمة العادية أو ScrollView السوبر
    // ماركت)، بنستخدمه عشان نرجّع خلفية الشريط الثابت فوق للون الصح بعد ما شريط
    // البحث يتقفل.
    double _lastScrollOffset;
    int _lastFirstVisibleItemIndex = -1;

    void OnContentScrolled(object? sender, ItemsViewScrolledEventArgs e)
    {
        _lastScrollOffset = e.VerticalOffset;
        _lastFirstVisibleItemIndex = e.FirstVisibleItemIndex;
        UpdateToolbarBackground(e.VerticalOffset);

        if (_categoryBarOffsetY > 0)
        {
            var shouldStick = e.VerticalOffset >= _categoryBarOffsetY;
            if (shouldStick != _isSticky)
            {
                _isSticky = shouldStick;

                // ✅ FIX: لو شريط البحث فاتح، منسيبش سكرول الصفحة يظهر النسخة العايمة
                // فوق شريط البحث (كانت هي سبب مشكلة التسريب البصري). بنسجّل الحالة
                // بس (_isSticky) ونطبّقها فعليًا لما البحث يتقفل.
                if (!IsSearchCurrentlyActive())
                    StickyCategoriesBar.IsVisible = shouldStick;
            }
        }

        UpdateCurrentGroupLabel(e.FirstVisibleItemIndex);
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
        _lastScrollOffset = e.ScrollY;
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
        // ✅ FIX: لما شريط البحث فاتح، الخلفية لازم تفضل ظاهرة بالكامل (بتتحكم
        // فيها OnSearchIconTapped)، فمنسيبش سكرول الخلفية اللي تحتها (لو حصل
        // مصادفة) يغيّر شفافيتها.
        if (IsSearchCurrentlyActive()) return;

        var threshold = _categoryBarOffsetY > 0 ? _categoryBarOffsetY
                       : _groceryStickyOffsetY > 0 ? _groceryStickyOffsetY
                       : 220;

        var fadeStart = Math.Max(0, threshold - ToolbarFadeDistance);
        var t = Math.Clamp((offset - fadeStart) / ToolbarFadeDistance, 0, 1);
        ToolbarBackgroundFill.Opacity = t * (2 - t); // ease-out بسيط لسلاسة بصرية أكتر
    }

    // بنحدد اسم القسم (الكاتوجري) اللي منتجاته ظاهرة دلوقتي فوق الشاشة، عشان
    // نكتبه في CurrentGroupBar وهو ثابت فوق أثناء السكرول جوه المنتجات، بنفس
    // شكل عنوان القسم العادي.
    // ملحوظة: الفهرسة هنا مبنية على افتراض إن FirstVisibleItemIndex بيرجّع
    // ترتيب العنصر جوه القايمة المسطحة للمنتجات (من غير احتساب صفوف عناوين
    // الأقسام). لو ظهر فرق بسيط في التوقيت على جهاز معين، ممكن يتظبط بإضافة/
    // طرح واحد من كل عداد قسم.
    void UpdateCurrentGroupLabel(int firstVisibleItemIndex)
    {
        if (BindingContext is not RestaurantViewModel vm || vm.MenuGroups.Count == 0)
        {
            CurrentGroupBar.IsVisible = false;
            return;
        }

        // ما نظهرش عنوان القسم لحد ما شريط الأقسام العايم نفسه يظهر (يعني اليوزر
        // فعلاً نزل جوه قايمة المنتجات، مش لسه في الغلاف/الكارت اللي فوق).
        if (!_isSticky || IsSearchCurrentlyActive() || firstVisibleItemIndex < 0)
        {
            CurrentGroupBar.IsVisible = false;
            return;
        }

        var running = 0;
        string? name = null;
        foreach (var group in vm.MenuGroups)
        {
            running += group.Count;
            if (firstVisibleItemIndex < running) { name = group.Key.Name; break; }
        }
        name ??= vm.MenuGroups[^1].Key.Name;

        CurrentGroupNameLabel.Text = name;
        RepositionCurrentGroupBar();
        CurrentGroupBar.IsVisible = true;
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
                CurrentGroupBar.IsVisible = false;

                // ✅ FEATURE: خلفية الشريط الثابت فوق تتمد لتحت عشان تحضن شريط
                // البحث بالكامل (من تحت زرار الرجوع لحد تحت زرار السلة)، وتفضل
                // ظاهرة بالكامل طول ما البحث فاتح مهما كان مكان السكرول.
                ToolbarBackgroundFill.AbortAnimation("ToolbarHeightAnim");
                var expandAnim = new Animation(v => ToolbarBackgroundFill.HeightRequest = v,
                    ToolbarBackgroundFill.Height > 0 ? ToolbarBackgroundFill.Height : _toolbarHeight,
                    _searchExpandedToolbarHeight);

                await Task.WhenAll(
                    SearchBarBorder.FadeTo(1, 180, Easing.CubicOut),
                    SearchBarBorder.ScaleTo(1, 180, Easing.CubicOut),
                    SearchIconBorder.FadeTo(0, 140, Easing.CubicOut),
                    ToolbarBackgroundFill.FadeTo(1, 180, Easing.CubicOut),
                    RunAnimation(expandAnim, "ToolbarHeightAnim", 180));

                SearchIconBorder.InputTransparent = true;
                SearchEntry.Focus();
            }
            else
            {
                SearchEntry.Unfocus();
                SearchIconBorder.InputTransparent = false;

                ToolbarBackgroundFill.AbortAnimation("ToolbarHeightAnim");
                var collapseAnim = new Animation(v => ToolbarBackgroundFill.HeightRequest = v,
                    ToolbarBackgroundFill.Height > 0 ? ToolbarBackgroundFill.Height : _searchExpandedToolbarHeight,
                    _toolbarHeight);

                await Task.WhenAll(
                    SearchBarBorder.FadeTo(0, 150, Easing.CubicIn),
                    SearchBarBorder.ScaleTo(0.92, 150, Easing.CubicIn),
                    SearchIconBorder.FadeTo(1, 180, Easing.CubicOut),
                    RunAnimation(collapseAnim, "ToolbarHeightAnim", 150));

                SearchBarBorder.InputTransparent = true;
                vm.ToggleSearchCommand.Execute(null);

                // بعد ما شريط البحث يقفل، نرجّع خلفية الشريط الثابت فوق لشفافيتها
                // الطبيعية على حسب مكان السكرول الحالي، ونرجّع النسخة العايمة
                // تظهر تاني لو اليوزر كان أصلاً واصل لمكان الالتصاق (Sticky).
                UpdateToolbarBackground(_lastScrollOffset);
                StickyCategoriesBar.IsVisible = _isSticky || _isGroceryStuck;
                UpdateCurrentGroupLabel(_lastFirstVisibleItemIndex);
            }
        }
        finally
        {
            _isSearchAnimating = false;
        }
    }

    // بيلف Animation.Commit (اللي بتاعته callback مش Task) في Task عشان يتظبط
    // جنب باقي حركات الـ FadeTo/ScaleTo في نفس الـ Task.WhenAll.
    Task RunAnimation(Animation animation, string name, uint length)
    {
        var tcs = new TaskCompletionSource();
        animation.Commit(RootPage, name, length: length,
            finished: (v, cancelled) => tcs.TrySetResult());
        return tcs.Task;
    }

}