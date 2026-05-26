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
        // نستخدم Task.Delay عشان ننتظر الـ CollectionView يعمل layout للـ item الجديد على Android
        // بدون الـ delay بتيجي: Java.Lang.IllegalArgumentException: Invalid target position
        _vm.Messages.CollectionChanged += async (_, _) =>
        {
            if (_vm.Messages.Count == 0) return;
            await Task.Delay(100); // ننتظر الـ render يخلص
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