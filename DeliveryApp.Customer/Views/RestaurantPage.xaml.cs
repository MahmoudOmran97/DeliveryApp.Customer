using DeliveryApp.Customer.Models;
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

        ApplyHeaderLayout();
    }

    // هيدر بموقع ثابت مثل بقية الصفحات؛ لا يعتمد على Safe Area أو Window Insets.
    const double HeaderContentTop = 24;
    const double IconSize = 38;
    const double BottomPadding = 4;

    double _toolbarHeight = 92;

    // هيدر مختصر: مسافات صغيرة بين صف الأيقونات والبحث، مع فصل بسيط قبل المحتوى.
    const double SearchBarGap = 2;
    const double SearchBarHeight = 42;
    const double SearchBarBottomGap = 6;
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
        // شريط الأقسام وعنوان القسم أصبحا في صف المحتوى أسفل الهيدر.
        // لذلك لا نضيف ارتفاع الهيدر مرة أخرى إلى هوامشهما.
        var baseTop = StickyCategoriesBar.IsVisible
            ? _stickyCategoriesBarHeight
            : 0;
        CurrentGroupBar.Margin = new Thickness(0, baseTop, 0, 0);
    }

    void ApplyHeaderLayout()
    {
        var rowTop = HeaderContentTop;

        BackButtonImage.Margin = new Thickness(16, rowTop, 0, 0);
        CartIconGrid.Margin = new Thickness(0, rowTop, 16, 0);
        RestaurantNameHeaderLabel.Margin = new Thickness(60, rowTop + 10, 60, 0);

        _toolbarHeight = rowTop + IconSize + BottomPadding;

        // شريط البحث جزء ثابت من الهيدر: يبدأ بعد صف الأيقونات ويحافظ على
        // هامش جانبي واضح، فلا يلامس الشاشة ولا يتداخل مع الرجوع أو السلة.
        var searchBarTop = rowTop + IconSize + SearchBarGap;
        SearchBarBorder.Margin = new Thickness(16, searchBarTop, 16, 0);

        // ✅ FEATURE: لما شريط البحث يفتح، خلفية الشريط الثابت فوق (اللي بتغطي
        // من تحت زرار الرجوع لحد زرار السلة أفقيًا) لازم تتمد لتحت كمان عشان
        // تحضن شريط البحث بدل ما يفضل عايم من غير خلفية وراه.
        _searchExpandedToolbarHeight = searchBarTop + SearchBarHeight + SearchBarBottomGap;

        // تظل خلفية الهيدر مرئية أثناء كل التمرير، وتشمل الأيقونات والبحث معًا.
        ToolbarBackgroundFill.HeightRequest = _searchExpandedToolbarHeight;
        ToolbarBackgroundFill.Opacity = 1;

        // الغلاف وشريط الأقسام داخل الصف الثاني؛ يبدأان تلقائيًا بعد الهيدر بلا
        // Margin علوي أو تداخل معه.
        RestaurantCover.Margin = new Thickness(0);
        GroceryCover.Margin = new Thickness(0, 0, 0, -20);
        StickyCategoriesBar.Margin = new Thickness(0);
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

                // تظهر الأقسام فور الوصول لموضعها وتبقى أسفل الهيدر للعودة السريعة.
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
        if (view.Height > 0)
            _groceryStickyOffsetY = 220 + view.Height;
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

        // تبقى الأقسام ثابتة أسفل الهيدر حتى مع وجود شريط البحث.
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
        // الهيدر له خلفية ثابتة دائمًا؛ لا نجعله شفافًا أثناء التمرير حتى لا
        // تبدو أيقونتا الرجوع والسلة ومربع البحث كأنها تتحرك فوق المحتوى بلا تنسيق.
        ToolbarBackgroundFill.Opacity = 1;
        ToolbarBackgroundFill.HeightRequest = _searchExpandedToolbarHeight;
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
        // اسم القسم يظهر داخل قائمة المنتجات عبر GroupHeaderTemplate.
        // لا نضيف شريط عنوان عائمًا آخر حتى لا يغطي المنتجات أو شريط الأقسام.
        CurrentGroupBar.IsVisible = false;
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

    void OnSearchIconTapped(object? sender, TappedEventArgs e)
    {
        // ✅ FIX: البحث مفتوح دايمًا، فمافيش داعي للتوجل، بس نركز على حقل الإدخال
        SearchEntry.Focus();
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