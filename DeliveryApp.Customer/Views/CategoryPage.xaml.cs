using DeliveryApp.Customer.ViewModels;

namespace DeliveryApp.Customer.Views;

public partial class CategoryPage : ContentPage
{
	readonly CategoryViewModel _vm;
	IDispatcherTimer? _bannerTimer;
	// نفس فكرة الرئيسية: بنقارن بالـ index اللي التايمر ضبطه عشان منعملش Restart للتايمر من تغيير هو عمله
	int? _programmaticBannerIndex;

	public CategoryPage(CategoryViewModel vm)
	{
		InitializeComponent();
		BindingContext = _vm = vm;
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();

		// الـ QueryProperties (category/search) بيتحطوا قبل ما الصفحة تظهر،
		// فنحمّل هنا عشان نضمن إن القيمتين اتحطوا الاتنين قبل النداء على الـ API
		if (_vm.LoadCommand.CanExecute(null))
			_vm.LoadCommand.Execute(null);

		StartBannerTimer();
	}

	protected override void OnDisappearing()
	{
		base.OnDisappearing();
		_bannerTimer?.Stop();
	}

	// ══════════════════════════════════════════════
	//  Banner Auto-Scroll (كل 3 ثواني — زي الرئيسية)
	// ══════════════════════════════════════════════
	void StartBannerTimer()
	{
		_bannerTimer?.Stop();
		_bannerTimer = Dispatcher.CreateTimer();
		_bannerTimer.Interval = TimeSpan.FromSeconds(3);
		_bannerTimer.Tick += (_, _) =>
		{
			try
			{
				if (_vm.Banners == null || _vm.Banners.Count <= 1) return;

				var next = (_vm.CurrentBannerIndex + 1) % _vm.Banners.Count;
				_programmaticBannerIndex = next;
				_vm.CurrentBannerIndex = next;
			}
			catch
			{
				// تجاهل أي استثناء عشان التايمر مايوقفش التطبيق
			}
		};
		_bannerTimer.Start();
	}

	void RestartBannerTimer()
	{
		try
		{
			_bannerTimer?.Stop();
			_bannerTimer?.Start();
		}
		catch { /* تجاهل */ }
	}

	void BannerCarousel_PositionChanged(object? sender, PositionChangedEventArgs e)
	{
		if (_programmaticBannerIndex.HasValue && _programmaticBannerIndex.Value == e.CurrentPosition)
		{
			_programmaticBannerIndex = null;
			return;
		}
		_programmaticBannerIndex = null;
		RestartBannerTimer();
	}
}
