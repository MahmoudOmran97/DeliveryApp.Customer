using DeliveryApp.Customer.ViewModels;

namespace DeliveryApp.Customer.Views;

public partial class PrescriptionChatPage : ContentPage
{
    readonly PrescriptionChatViewModel _vm;

    public PrescriptionChatPage(PrescriptionChatViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;

        // Auto-scroll لآخر رسالة (نفس فكرة DriverChatPage)
        _vm.Messages.CollectionChanged += async (_, _) =>
        {
            if (_vm.Messages.Count == 0) return;
            await Task.Delay(100);
            MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    if (_vm.Messages.Count > 0)
                        ChatList.ScrollTo(_vm.Messages[^1], ScrollToPosition.End, animate: true);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[ScrollTo] {ex.Message}");
                }
            });
        };
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _vm.Cleanup();
    }
}
