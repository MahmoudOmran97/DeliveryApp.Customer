namespace DeliveryApp.Customer.Services;

public class AuthService
{
    private const string K_Token = "auth_token";
    private const string K_Id = "user_id";
    private const string K_Name = "user_name";
    private const string K_Email = "user_email";

    // 🔒 SECURITY FIX: كان التوكن متخزن بـ Preferences (SharedPreferences عادي
    // على أندرويد = plaintext، أي حد عنده root أو ياخد backup للجهاز يقدر يقراه).
    // دلوقتي بيتخزن في SecureStorage (Android Keystore / iOS Keychain = مشفّر).
    //
    // SecureStorage async بس، وGetToken()/IsLoggedIn مستخدمين sync في أماكن كتير
    // (ApiService.SetAuth بيتنادى قبل كل request)، فبنعمل كاش في الذاكرة بيتحمّل
    // مرة واحدة بدري عن طريق InitializeAsync() (لازم تتنادى من SplashPage قبل أي
    // قراءة لـ IsLoggedIn)، وبعد كده القراءات كلها sync وسريعة من الكاش.
    private string? _cachedToken;
    private int _cachedId;
    private string? _cachedName;
    private string? _cachedEmail;
    private Task? _initTask;

    /// <summary>
    /// لازم تتنادى مرة واحدة (وأول حاجة) قبل أي استخدام لـ IsLoggedIn/GetToken —
    /// أفضل مكان SplashPage.OnAppearing قبل ما تشيك IsLoggedIn. آمنة تتنادى أكتر
    /// من مرة (idempotent) — أي نداء تاني هياخد نفس الـ Task المحفوظ.
    /// </summary>
    public Task InitializeAsync() => _initTask ??= LoadFromSecureStorageAsync();

    private async Task LoadFromSecureStorageAsync()
    {
        try
        {
            _cachedToken = await SecureStorage.Default.GetAsync(K_Token);
            _cachedName = await SecureStorage.Default.GetAsync(K_Name);
            _cachedEmail = await SecureStorage.Default.GetAsync(K_Email);
            var idStr = await SecureStorage.Default.GetAsync(K_Id);
            _cachedId = int.TryParse(idStr, out var id) ? id : 0;

            // 🔄 Migration: لو المستخدم مثبت نسخة قديمة كانت بتخزن في Preferences
            // (plaintext)، ننقل البيانات لـ SecureStorage مرة واحدة ونمسحها من
            // Preferences عشان محدش يقدر يقراها تاني من هناك.
            if (string.IsNullOrEmpty(_cachedToken))
            {
                var legacyToken = Preferences.Get(K_Token, string.Empty);
                if (!string.IsNullOrEmpty(legacyToken))
                {
                    _cachedToken = legacyToken;
                    _cachedId = Preferences.Get(K_Id, 0);
                    _cachedName = Preferences.Get(K_Name, string.Empty);
                    _cachedEmail = Preferences.Get(K_Email, string.Empty);

                    await SecureStorage.Default.SetAsync(K_Token, _cachedToken);
                    await SecureStorage.Default.SetAsync(K_Id, _cachedId.ToString());
                    await SecureStorage.Default.SetAsync(K_Name, _cachedName ?? string.Empty);
                    await SecureStorage.Default.SetAsync(K_Email, _cachedEmail ?? string.Empty);

                    Preferences.Remove(K_Token);
                    Preferences.Remove(K_Id);
                    Preferences.Remove(K_Name);
                    Preferences.Remove(K_Email);
                }
            }
        }
        catch
        {
            // لو الـ Keystore/Keychain فشل لأي سبب (نادر)، نعتبر المستخدم مش مسجل
            // دخول بدل ما نكراش على الإقلاع — هيضطر يعمل login تاني، أأمن من كراش.
        }
    }

    public bool IsLoggedIn => !string.IsNullOrEmpty(_cachedToken);

    public async Task SaveUserAsync(string token, int id, string name, string email, string role)
    {
        _cachedToken = token;
        _cachedId = id;
        _cachedName = name;
        _cachedEmail = email;

        await SecureStorage.Default.SetAsync(K_Token, token);
        await SecureStorage.Default.SetAsync(K_Id, id.ToString());
        await SecureStorage.Default.SetAsync(K_Name, name ?? string.Empty);
        await SecureStorage.Default.SetAsync(K_Email, email ?? string.Empty);
    }

    public string GetToken() => _cachedToken ?? string.Empty;

    public int GetUserId() => _cachedId;

    public string GetUserName() => _cachedName ?? string.Empty;

    public string GetEmail() => _cachedEmail ?? string.Empty;

    public void Logout()
    {
        _cachedToken = null;
        _cachedId = 0;
        _cachedName = null;
        _cachedEmail = null;

        SecureStorage.Default.Remove(K_Token);
        SecureStorage.Default.Remove(K_Id);
        SecureStorage.Default.Remove(K_Name);
        SecureStorage.Default.Remove(K_Email);
    }
}
