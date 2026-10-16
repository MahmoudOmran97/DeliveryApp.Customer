using DeliveryApp.Customer.ViewModels;

namespace DeliveryApp.Customer.Views;

public partial class SearchPage : ContentPage
{
    bool _focusedOnce;

    public SearchPage(SearchViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // أول ما الصفحة تتفتح الكيبورد يطلع على طول، لكن لما نرجع من محل/منتج منعملش Focus تاني
        if (_focusedOnce) return;
        _focusedOnce = true;

        Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(350), () => SearchEntry.Focus());
    }
}
