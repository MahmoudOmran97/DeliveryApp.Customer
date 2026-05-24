using DeliveryApp.Customer.ViewModels;

namespace DeliveryApp.Customer.Views;

public partial class SupportChatPage : ContentPage
{
    readonly SupportChatViewModel _vm;

    public SupportChatPage(SupportChatViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;

        // Auto-scroll on new message
        _vm.Messages.CollectionChanged += (_, _) =>
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Task.Delay(100);
                if (_vm.Messages.Count > 0)
                    ChatList.ScrollTo(_vm.Messages[^1], animate: true);
            });
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm.InitIfNeeded();
    }
}
