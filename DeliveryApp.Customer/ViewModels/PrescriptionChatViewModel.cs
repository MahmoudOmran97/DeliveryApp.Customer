using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeliveryApp.Customer.Models;
using DeliveryApp.Customer.Services;

namespace DeliveryApp.Customer.ViewModels;

// ─────────────────────────────────────────────────────────────────────────────
// شات ما قبل الأوردر بين العميل وصاحب الصيدلية عشان يتفقوا على تمن الروشتة.
// لما صاحب الصيدلية يحدد السعر (Priced)، بيظهر بانر فوق الشات فيه زرار "موافق"
// يقفل السعر ويرجع العميل للـ Checkout بيه.
// ─────────────────────────────────────────────────────────────────────────────
[QueryProperty(nameof(RequestId), "requestId")]
public partial class PrescriptionChatViewModel : BaseViewModel
{
    readonly ApiService _api;
    readonly SignalRService _signalR;
    readonly CartService _cart;
    readonly AuthService _auth;

    [ObservableProperty] int _requestId;
    [ObservableProperty] string _inputText = "";
    [ObservableProperty] string _status = "Pending";
    [ObservableProperty] decimal? _agreedPrice;
    [ObservableProperty] string? _restaurantName;

    public bool IsPriced => Status == "Priced" && AgreedPrice.HasValue;
    public bool IsConfirmed => Status == "Confirmed";

    public ObservableCollection<PrescriptionMessage> Messages { get; } = new();

    public PrescriptionChatViewModel(ApiService api, SignalRService signalR, CartService cart, AuthService auth)
    {
        _api = api;
        _signalR = signalR;
        _cart = cart;
        _auth = auth;

        _signalR.PrescriptionMessageReceived += OnMessageReceived;
        _signalR.PrescriptionPriceSet += OnPriceSet;
    }

    partial void OnRequestIdChanged(int value)
    {
        if (value > 0) _ = InitAsync();
    }

    async Task InitAsync()
    {
        if (!_signalR.IsConnected)
            await _signalR.ConnectAsync(_auth.GetToken());

        await LoadAsync();
    }

    [RelayCommand]
    async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            var req = await _api.GetPrescriptionRequestAsync(RequestId);
            if (req != null)
            {
                Status = req.Status;
                AgreedPrice = req.AgreedPrice;
                RestaurantName = req.RestaurantName;
                OnPropertyChanged(nameof(IsPriced));
                OnPropertyChanged(nameof(IsConfirmed));
            }

            var history = await _api.GetPrescriptionMessagesAsync(RequestId);
            if (history != null)
            {
                Messages.Clear();
                foreach (var m in history) Messages.Add(m);
            }
        }
        finally { IsBusy = false; }
    }

    void OnMessageReceived(int requestId, string senderRole, string message, DateTime createdAt)
    {
        if (requestId != RequestId) return;
        // الرسائل اللي بعتها أنا بتتضاف محلي فورًا في Send()، فمنضيفهاش تاني هنا
        if (senderRole == "Customer") return;

        Messages.Add(new PrescriptionMessage { SenderRole = senderRole, Message = message, CreatedAt = createdAt });
    }

    void OnPriceSet(int requestId, decimal price)
    {
        if (requestId != RequestId) return;
        AgreedPrice = price;
        Status = "Priced";
        OnPropertyChanged(nameof(IsPriced));
    }

    [RelayCommand]
    async Task Send()
    {
        var text = InputText.Trim();
        if (string.IsNullOrEmpty(text)) return;
        InputText = "";

        Messages.Add(new PrescriptionMessage { SenderRole = "Customer", Message = text, CreatedAt = DateTime.Now });
        await _api.SendPrescriptionMessageAsync(RequestId, text);
    }

    // ✅ العميل يوافق على السعر اللي حدده صاحب الصيدلية — يقفل السعر جوه السلة
    // ويرجعه على الـ Checkout عشان يكمل الأوردر بيه.
    [RelayCommand]
    async Task ConfirmPrice()
    {
        if (!IsPriced || AgreedPrice is null) return;

        IsBusy = true;
        try
        {
            var ok = await _api.ConfirmPrescriptionPriceAsync(RequestId);
            if (!ok)
            {
                await AlertAsync(LocalizationService.Get("ActionFailed"));
                return;
            }

            Status = "Confirmed";
            OnPropertyChanged(nameof(IsConfirmed));
            _cart.SetPrescriptionAgreedPrice(RequestId, AgreedPrice.Value);
            await Shell.Current.GoToAsync("CheckoutPage");
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    async Task CancelRequest()
    {
        var confirm = await Shell.Current.DisplayAlert(
            LocalizationService.Get("CancelOrder"),
            LocalizationService.Get("CancelOrderConfirm"),
            LocalizationService.Get("Ok"),
            LocalizationService.Get("Cancel"));
        if (!confirm) return;

        if (await _api.CancelPrescriptionRequestAsync(RequestId))
        {
            _cart.ClearPrescription();
            await Shell.Current.GoToAsync("..");
        }
    }

    public void Cleanup()
    {
        _signalR.PrescriptionMessageReceived -= OnMessageReceived;
        _signalR.PrescriptionPriceSet -= OnPriceSet;
    }
}
