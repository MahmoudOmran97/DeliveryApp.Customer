// ═══════════════════════════════════════════════════════════════
// DeliveryApp.Customer / ViewModels / CallViewModel.cs
// شاشة موحّدة لحالات المكالمة: بترن (Incoming) / بتتصل (Outgoing) / متصلة (Connected)
// ═══════════════════════════════════════════════════════════════
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeliveryApp.Customer.Services;

namespace DeliveryApp.Customer.ViewModels;

public enum CallState { Ringing, Calling, Connected, Ended }

[QueryProperty(nameof(OrderId), "orderId")]
[QueryProperty(nameof(OtherPartyName), "otherPartyName")]
[QueryProperty(nameof(IsIncomingRaw), "isIncoming")]
public partial class CallViewModel : BaseViewModel, IDisposable
{
    readonly SignalRService _signalR;
    readonly CallAudioService _callAudio;
    System.Timers.Timer? _durationTimer;
    int _secondsElapsed;
    bool _navigatedAway;
    string? _pendingOfferSdp;
    readonly List<string> _pendingIceCandidates = new();

    [ObservableProperty] int _orderId;
    [ObservableProperty] string _otherPartyName = "";
    [ObservableProperty] CallState _state;
    [ObservableProperty] string _statusText = "";
    [ObservableProperty] string _durationText = "00:00";

    public string IsIncomingRaw
    {
        set => State = value == "true" ? CallState.Ringing : CallState.Calling;
    }

    public bool IsRinging => State == CallState.Ringing;
    public bool IsCalling => State == CallState.Calling;
    public bool IsConnected => State == CallState.Connected;

    public CallViewModel(SignalRService signalR, CallAudioService callAudio)
    {
        _signalR = signalR;
        _callAudio = callAudio;
        _signalR.VoiceCallAccepted += OnAccepted;
        _signalR.VoiceCallRejected += OnRejected;
        _signalR.VoiceCallEnded += OnEnded;
        _signalR.CallOfferReceived += OnOfferReceived;
        _signalR.CallAnswerReceived += OnAnswerReceived;
        _signalR.IceCandidateReceived += OnIceCandidateReceived;
        _callAudio.OnError += ex => System.Diagnostics.Debug.WriteLine($"[Call] Audio error: {ex.Message}");
    }

    partial void OnStateChanged(CallState value)
    {
        StatusText = value switch
        {
            CallState.Ringing => "مكالمة واردة...",
            CallState.Calling => "جارِ الاتصال...",
            CallState.Connected => "متصل",
            CallState.Ended => "انتهت المكالمة",
            _ => ""
        };
        OnPropertyChanged(nameof(IsRinging));
        OnPropertyChanged(nameof(IsCalling));
        OnPropertyChanged(nameof(IsConnected));

        if (value == CallState.Calling)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    var sdp = await _callAudio.CreateOfferAsync(OrderId);
                    await _signalR.SendCallOfferAsync(OrderId, sdp);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[Call] CreateOffer failed: {ex.Message}");
                }
            });
        }
    }

    [RelayCommand]
    async Task Accept()
    {
        await _signalR.AcceptVoiceCallAsync(OrderId);
        State = CallState.Connected;
        StartTimer();

        if (_pendingOfferSdp != null)
        {
            await _callAudio.HandleRemoteOfferAsync(OrderId, _pendingOfferSdp);
            foreach (var c in _pendingIceCandidates) await _callAudio.AddRemoteIceCandidateAsync(c);
            _pendingIceCandidates.Clear();
        }
    }

    [RelayCommand]
    async Task Reject()
    {
        await _signalR.RejectVoiceCallAsync(OrderId);
        await CloseAsync();
    }

    [RelayCommand]
    async Task Cancel()
    {
        await _signalR.EndVoiceCallAsync(OrderId);
        await CloseAsync();
    }

    [RelayCommand]
    async Task EndCall()
    {
        await _signalR.EndVoiceCallAsync(OrderId);
        await CloseAsync();
    }

    void OnAccepted(int orderId, int byUserId)
    {
        if (orderId != OrderId || State != CallState.Calling) return;
        MainThread.BeginInvokeOnMainThread(() =>
        {
            State = CallState.Connected;
            StartTimer();
        });
    }

    void OnRejected(int orderId, int byUserId)
    {
        if (orderId != OrderId) return;
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await AlertAsync("الطرف التاني رفض المكالمة");
            await CloseAsync();
        });
    }

    void OnEnded(int orderId, int byUserId)
    {
        if (orderId != OrderId) return;
        MainThread.BeginInvokeOnMainThread(async () => await CloseAsync());
    }

    void OnOfferReceived(int orderId, int fromUserId, string sdp)
    {
        if (orderId != OrderId) return;
        _pendingOfferSdp = sdp;
    }

    void OnAnswerReceived(int orderId, int fromUserId, string sdp)
    {
        if (orderId != OrderId) return;
        _ = Task.Run(async () =>
        {
            await _callAudio.HandleRemoteAnswerAsync(sdp);
            foreach (var c in _pendingIceCandidates) await _callAudio.AddRemoteIceCandidateAsync(c);
            _pendingIceCandidates.Clear();
        });
    }

    void OnIceCandidateReceived(int orderId, int fromUserId, string candidateJson)
    {
        if (orderId != OrderId) return;
        _pendingIceCandidates.Add(candidateJson);
        _ = _callAudio.AddRemoteIceCandidateAsync(candidateJson);
    }

    void StartTimer()
    {
        _secondsElapsed = 0;
        _durationTimer = new System.Timers.Timer(1000);
        _durationTimer.Elapsed += (_, _) =>
        {
            _secondsElapsed++;
            var ts = TimeSpan.FromSeconds(_secondsElapsed);
            MainThread.BeginInvokeOnMainThread(() => DurationText = ts.ToString(@"mm\:ss"));
        };
        _durationTimer.Start();
    }

    async Task CloseAsync()
    {
        if (_navigatedAway) return;
        _navigatedAway = true;
        Dispose();
        await Shell.Current.GoToAsync("..");
    }

    public void Dispose()
    {
        _durationTimer?.Stop();
        _durationTimer?.Dispose();
        _callAudio.Hangup();
        _signalR.VoiceCallAccepted -= OnAccepted;
        _signalR.VoiceCallRejected -= OnRejected;
        _signalR.VoiceCallEnded -= OnEnded;
        _signalR.CallOfferReceived -= OnOfferReceived;
        _signalR.CallAnswerReceived -= OnAnswerReceived;
        _signalR.IceCandidateReceived -= OnIceCandidateReceived;
    }
}
