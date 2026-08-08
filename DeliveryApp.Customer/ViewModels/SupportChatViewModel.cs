using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeliveryApp.Customer.Models;
using DeliveryApp.Customer.Services;

namespace DeliveryApp.Customer.ViewModels;

// ─────────────────────────────────────────────────────────────────────────────
// شات الدعم: كان قبل كده بيكلم Anthropic API مباشرة من الموبايل (والـ API key
// كانت متخزنة/هاردكودد جوا التطبيق نفسه، وده خطر أمني). دلوقتي التطبيق بيكلم
// الباكإند بس (SupportChatController)، والباكإند هو اللي بيكلم الـ AI باستخدام
// إعدادات AiSettings اللي الأدمن بيتحكم فيها من لوحة التحكم. الباكإند برضو هو
// اللي بيقرر لو محتاج يسجل شكوى تلقائي أو يحول الشات لأدمن حقيقي.
// ─────────────────────────────────────────────────────────────────────────────
public partial class SupportChatViewModel : BaseViewModel
{
    readonly ApiService _api;
    readonly SignalRService _signalR;

    public ObservableCollection<ChatMessage> Messages { get; } = new();

    [ObservableProperty] string _inputText = string.Empty;
    [ObservableProperty] bool _isTyping;
    [ObservableProperty] bool _isEscalated; // true لما الشات يتحول لأدمن حقيقي

    private bool _initialized;
    private int _sessionId;

    public SupportChatViewModel(ApiService api, SignalRService signalR)
    {
        _api = api;
        _signalR = signalR;
        _signalR.SupportMessageReceived += OnAdminMessageReceived;
    }

    // ── Init: يجيب (أو يعمل) شات الدعم المفتوح بتاع العميل ويحمّل تاريخه ──
    public void InitIfNeeded()
    {
        if (_initialized) return;
        _initialized = true;
        _ = InitAsync();
    }

    async Task InitAsync()
    {
        IsBusy = true;
        try
        {
            var session = await _api.GetOrCreateSupportSessionAsync();
            if (session == null)
            {
                Messages.Add(new ChatMessage { Text = "⚠️ مش قادرين نفتح شات الدعم دلوقتي، حاول تاني كمان شوية.", IsFromAi = true });
                return;
            }

            _sessionId = session.Id;
            IsEscalated = session.Status == "Escalated";

            if (session.Messages.Count == 0)
            {
                Messages.Add(new ChatMessage
                {
                    Text = "👋 أهلاً بيك! أنا مساعد الدعم بتاعك. تقدر تسألني عن حالة طلبك، الإلغاء، الاسترجاع، أو أي مشكلة واجهتك.",
                    IsFromAi = true
                });
            }
            else
            {
                foreach (var m in session.Messages)
                {
                    Messages.Add(new ChatMessage
                    {
                        Text = m.Message,
                        IsFromAi = !m.IsMine,
                        Time = m.CreatedAt
                    });
                }
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    // ── Send ──────────────────────────────────────────────────────
    [RelayCommand]
    async Task Send()
    {
        var text = InputText.Trim();
        if (string.IsNullOrEmpty(text) || IsTyping || _sessionId == 0) return;

        InputText = string.Empty;
        Messages.Add(new ChatMessage { Text = text, IsFromAi = false });

        IsTyping = true;
        try
        {
            var result = await _api.SendSupportMessageAsync(_sessionId, text);
            if (result == null)
            {
                Messages.Add(new ChatMessage { Text = "⚠️ الرسالة معملتش، حاول تاني.", IsFromAi = true });
                return;
            }

            if (result.Escalated) IsEscalated = true;

            // في وضع Escalated الرد بييجي من أدمن حقيقي عن طريق SignalR مش رد فوري هنا
            if (result.AiReply != null)
            {
                Messages.Add(new ChatMessage { Text = result.AiReply.Message, IsFromAi = true, Time = result.AiReply.CreatedAt });
            }
        }
        catch (Exception)
        {
            Messages.Add(new ChatMessage { Text = "⚠️ في مشكلة في الاتصال، حاول تاني.", IsFromAi = true });
        }
        finally
        {
            IsTyping = false;
        }
    }

    // ── رسالة جاية من الأدمن لحظيًا بعد ما الشات اتحول له ──
    void OnAdminMessageReceived(int sessionId, string message)
    {
        if (sessionId != _sessionId) return;
        MainThread.BeginInvokeOnMainThread(() =>
        {
            IsEscalated = true;
            Messages.Add(new ChatMessage { Text = message, IsFromAi = true });
        });
    }

    // ── Back ──────────────────────────────────────────────────────
    [RelayCommand]
    static async Task GoBack() => await Shell.Current.GoToAsync("..");
}
