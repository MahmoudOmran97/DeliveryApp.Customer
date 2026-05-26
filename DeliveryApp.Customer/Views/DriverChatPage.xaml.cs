using DeliveryApp.Customer.ViewModels;

namespace DeliveryApp.Customer.Views;

public partial class DriverChatPage : ContentPage
{
    readonly DriverChatViewModel _vm;

    public DriverChatPage(DriverChatViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;

        // Auto-scroll to latest message
        _vm.Messages.CollectionChanged += (_, _) =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (_vm.Messages.Count > 0)
                    ChatList.ScrollTo(_vm.Messages[^1], ScrollToPosition.End, animate: true);
            });
        };
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _vm.Cleanup();
    }
}