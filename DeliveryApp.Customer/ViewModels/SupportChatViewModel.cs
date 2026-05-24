using System.Collections.ObjectModel;
using System.Text;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeliveryApp.Customer.Models;

namespace DeliveryApp.Customer.ViewModels;

public partial class SupportChatViewModel : BaseViewModel
{
    // ── State ─────────────────────────────────────────────────────
    public ObservableCollection<ChatMessage> Messages { get; } = new();

    [ObservableProperty] string _inputText = string.Empty;
    [ObservableProperty] bool _isTyping;

    private bool _initialized;

    // Anthropic API key — loaded from config / secrets at runtime
    private const string ApiUrl = "https://api.anthropic.com/v1/messages";
    private const string Model = "claude-sonnet-4-20250514";

    // Keep a simple history for multi-turn context (user/assistant pairs)
    private readonly List<object> _history = new();

    private const string SystemPrompt =
        "You are a helpful customer support agent for a food delivery app called 'Deliver'. " +
        "Be friendly, concise, and helpful. Help customers with order issues, tracking, refunds, " +
        "cancellations, and general questions. Ask clarifying questions when needed. " +
        "Respond in the same language the user writes in (Arabic or English). " +
        "Keep responses short (2-4 sentences max unless detail is needed).";

    // ── Init greeting ─────────────────────────────────────────────
    public void InitIfNeeded()
    {
        if (_initialized) return;
        _initialized = true;
        Messages.Add(new ChatMessage
        {
            Text = "👋 Hello! I'm your support assistant. How can I help you today?\n\nYou can ask me about:\n• Order status & tracking\n• Cancellations & refunds\n• Account issues\n• General questions",
            IsFromAi = true
        });
    }

    // ── Send ──────────────────────────────────────────────────────
    [RelayCommand]
    async Task Send()
    {
        var text = InputText.Trim();
        if (string.IsNullOrEmpty(text) || IsTyping) return;

        InputText = string.Empty;

        // Add user message
        Messages.Add(new ChatMessage { Text = text, IsFromAi = false });

        // Build history
        _history.Add(new { role = "user", content = text });

        IsTyping = true;
        try
        {
            var reply = await CallClaudeAsync();
            _history.Add(new { role = "assistant", content = reply });
            Messages.Add(new ChatMessage { Text = reply, IsFromAi = true });
        }
        catch (Exception ex)
        {
            Messages.Add(new ChatMessage
            {
                Text = "⚠️ Sorry, I'm having trouble connecting right now. Please try again.",
                IsFromAi = true
            });
        }
        finally
        {
            IsTyping = false;
        }
    }

    // ── Anthropic API call ────────────────────────────────────────
    private static readonly HttpClient _http = new();

    private async Task<string> CallClaudeAsync()
    {
        // Build request
        var body = new
        {
            model = Model,
            max_tokens = 512,
            system = SystemPrompt,
            messages = _history
        };

        var json = JsonSerializer.Serialize(body);
        var request = new HttpRequestMessage(HttpMethod.Post, ApiUrl);
        request.Headers.Add("x-api-key", GetApiKey());
        request.Headers.Add("anthropic-version", "2023-06-01");
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _http.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new Exception($"API error: {response.StatusCode}");

        using var doc = JsonDocument.Parse(responseBody);
        return doc.RootElement
                   .GetProperty("content")[0]
                   .GetProperty("text")
                   .GetString() ?? "Sorry, I couldn't generate a response.";
    }

    // ── Load API key from Preferences or app config ───────────────
    private static string GetApiKey()
    {
        // Store your key via: Preferences.Set("claude_api_key", "sk-ant-...")
        // Or replace with your key for testing:
        return Preferences.Get("claude_api_key", string.Empty);
    }

    // ── Back ──────────────────────────────────────────────────────
    [RelayCommand]
    static async Task GoBack() => await Shell.Current.GoToAsync("..");
}
