using System.Net.Http.Json;

using System.Text.Json;

using DeliveryApp.Customer.Models;
using DeliveryApp.Customer.ViewModels;

namespace DeliveryApp.Customer.Services;

public class ApiService
{

    private readonly HttpClient _http;

    private readonly AuthService _auth;

    private readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

    // ← غير ده لـ URL الـ API بتاعتك

    private const string Base = "https://deliveryappapi.runasp.net/api";

    public ApiService(AuthService auth)

    {

        _auth = auth;

        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };

    }

    private void SetAuth()

    {

        var t = _auth.GetToken();

        if (!string.IsNullOrEmpty(t))

            _http.DefaultRequestHeaders.Authorization =

                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", t);

    }

    private async Task<T?> GetAsync<T>(string path)

    {

        SetAuth();

        try

        {

            var r = await _http.GetAsync($"{Base}/{path}");

            if (r.IsSuccessStatusCode)

                return await r.Content.ReadFromJsonAsync<T>(_json);

        }

        catch (Exception ex) { Debug(ex, path); }

        return default;

    }
    public class ApiException : Exception
    {
        public ApiException(string message) : base(message) { }
    }

    private async Task<T?> PostAsync<T>(string path, object payload)
    {
        SetAuth();
        try
        {
            var r = await _http.PostAsJsonAsync($"{Base}/{path}", payload);
            if (r.IsSuccessStatusCode)
                return await r.Content.ReadFromJsonAsync<T>(_json);

            // ← قراءة رسالة الخطأ من الـ API
            var errorBody = await r.Content.ReadAsStringAsync();
            try
            {
                var doc = System.Text.Json.JsonDocument.Parse(errorBody);
                if (doc.RootElement.TryGetProperty("message", out var msg))
                    throw new ApiException(msg.GetString()!);
            }
            catch (ApiException) { throw; }
            catch { }

            throw new ApiException($"Request failed ({(int)r.StatusCode})");
        }
        catch (ApiException) { throw; }
        catch (Exception ex) { Debug(ex, path); }
        return default;
    }

    private async Task<bool> PutAsync(string path, object? payload = null)

    {

        SetAuth();

        try

        {

            var r = payload != null

                ? await _http.PutAsJsonAsync($"{Base}/{path}", payload)

                : await _http.PutAsync($"{Base}/{path}", null);

            return r.IsSuccessStatusCode;

        }

        catch (Exception ex) { Debug(ex, path); }

        return false;

    }

    // ─── Auth ────────────────────────────────────────────────────────────────

    public Task<LoginResponse?> LoginAsync(string email, string password)

        => PostAsync<LoginResponse>("auth/login", new { Email = email, Password = password });

    // ✅ الجديد: بارامتر otp إضافي — لازم يتبعت مع بيانات التسجيل عشان يتحقق منه السيرفر
    public Task<LoginResponse?> RegisterAsync(string name, string email, string password, string phone, string otp)

        => PostAsync<LoginResponse>("auth/register", new { FullName = name, Email = email, Password = password, Phone = phone, Role = "Customer", Otp = otp });

    // ─── OTP (تسجيل حساب جديد / نسيت كلمة المرور) ─────────────────────────────
    // purpose: "Register" أو "ResetPassword"

    public async Task SendOtpAsync(string email, string purpose)
        => await PostAsync<object>("auth/send-otp", new { Email = email, Purpose = purpose });

    public async Task<bool> VerifyOtpAsync(string email, string code, string purpose)
    {
        var result = await PostAsync<object>("auth/verify-otp", new { Email = email, Code = code, Purpose = purpose });
        return result != null;
    }

    public async Task ResetPasswordAsync(string email, string code, string newPassword)
        => await PostAsync<object>("auth/reset-password", new { Email = email, Code = code, NewPassword = newPassword });

    // ─── Restaurants ─────────────────────────────────────────────────────────

    /// <summary>
    /// Gets restaurants filtered by location, category, min-rating, and search term.
    /// lat/lng/radiusKm → backend يرجع فقط المحلات اللي جوه النطاق.
    /// category → "Restaurants" | "Pharmacy" | "Grocery" | "Supermarket" | "Vegetables" | "Drinks" | "Accessories"
    /// minRating → فقط المحلات >= هذا التقييم (4.0 للصفحة الرئيسية)
    /// </summary>
    public Task<PagedResult<Restaurant>?> GetRestaurantsAsync(
        string? search = null,
        string sortBy = "rating",
        int page = 1,
        double? lat = null,
        double? lng = null,
        double radiusKm = 10.0,
        string? category = null,
        double minRating = 0.0,
        int pageSize = 20)
    {
        var q = $"restaurants?page={page}&pageSize={pageSize}&sortBy={sortBy}";
        if (!string.IsNullOrEmpty(search))
            q += $"&search={Uri.EscapeDataString(search)}";
        if (lat.HasValue && lng.HasValue)
        {
            q += $"&lat={lat.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
            q += $"&lng={lng.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
            q += $"&radiusKm={radiusKm.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
        }
        if (!string.IsNullOrEmpty(category))
            q += $"&category={Uri.EscapeDataString(category)}";
        if (minRating > 0)
            q += $"&minRating={minRating.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
        return GetAsync<PagedResult<Restaurant>>(q);
    }

    /// <summary>
    /// lat/lng (اختياري) → لو اتبعتوا (موقع العميل الحالي)، السعر الراجع في
    /// Restaurant.DeliveryFee بيبقى محسوب فعلياً حسب المسافة الحقيقية بين
    /// المحل والعميل (أول 3 كم بسعر المحل الأساسي، وبعدها 10 جنيه على كل
    /// كيلومتر زيادة أو جزء منه).
    /// </summary>
    public Task<Restaurant?> GetRestaurantAsync(int id, double? lat = null, double? lng = null)
    {
        var q = $"restaurants/{id}";
        if (lat.HasValue && lng.HasValue)
        {
            q += $"?lat={lat.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
            q += $"&lng={lng.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
        }
        return GetAsync<Restaurant>(q);
    }

    public Task<List<Category>?> GetMenuAsync(int restaurantId)

        => GetAsync<List<Category>>($"restaurants/{restaurantId}/menu");

    // ─── Orders ──────────────────────────────────────────────────────────────

    public Task<Order?> PlaceOrderAsync(
        int restaurantId, List<CartItem> items,
        string address, double lat, double lng,
        string? notes, string paymentMethod,
        string? couponCode = null, int? couponId = null,
        string? prescriptionImageUrl = null, string? prescriptionNotes = null)
        => PostAsync<Order>("orders", new
        {
            RestaurantId = restaurantId,
            Items = items.Select(i => new
            {
                ProductId = i.Product.Id,
                i.Quantity,
                i.Notes,
                VariantId = i.VariantId,
                UnitPriceOverride = i.DealId.HasValue ? i.UnitPrice : (decimal?)null
            }),
            DeliveryAddress = address,
            DeliveryLatitude = lat,
            DeliveryLongitude = lng,
            DeliveryNotes = notes,
            PrescriptionImageUrl = prescriptionImageUrl,
            PrescriptionNotes = prescriptionNotes,
            PaymentMethod = paymentMethod,
            CouponCode = couponCode,
            CouponId = couponId
        });

    public async Task<string?> UploadPrescriptionAsync(FileResult file)
    {
        try
        {
            SetAuth();
            using var content = new MultipartFormDataContent();
            await using var stream = await file.OpenReadAsync();
            content.Add(new StreamContent(stream), "file", file.FileName);
            var response = await _http.PostAsync($"{Base}/upload/prescription", content);
            if (!response.IsSuccessStatusCode) return null;
            var json = await response.Content.ReadAsStringAsync();
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("url").GetString();
        }
        catch (Exception ex)
        {
            Debug(ex, "upload/prescription");
            return null;
        }
    }

    public Task<Product?> GetProductAsync(int id)
        => GetAsync<Product>($"products/{id}");

    public async Task<RedeemPointsResult?> RedeemPointsAsync(int points)
    {
        try
        {
            SetAuth();
            var body = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(new { points }),
                System.Text.Encoding.UTF8, "application/json");
            var response = await _http.PostAsync($"{Base}/user/points/redeem", body);
            var json = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode) return null;
            return System.Text.Json.JsonSerializer.Deserialize<RedeemPointsResult>(json,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (Exception ex)
        {
            Debug(ex, "user/points/redeem");
            return null;
        }
    }

    public Task<PagedResult<Order>?> GetMyOrdersAsync(int page = 1)

        => GetAsync<PagedResult<Order>>($"orders/my?page={page}");

    public Task<Order?> GetOrderAsync(int id)

        => GetAsync<Order>($"orders/{id}");

    public Task<bool> CancelOrderAsync(int id, string? reason)

        => PutAsync($"orders/{id}/cancel", new { Reason = reason });

    // ─── Notifications ───────────────────────────────────────────────────────

    public Task<PagedResult<Notification>?> GetNotificationsAsync(int page = 1)

        => GetAsync<PagedResult<Notification>>($"notifications?page={page}");

    public Task<bool> MarkNotificationReadAsync(int id) => PutAsync($"notifications/{id}/read");

    public Task<bool> MarkAllReadAsync() => PutAsync("notifications/read-all");

    // ─── Profile ─────────────────────────────────────────────────────────────

    public Task<User?> GetProfileAsync() => GetAsync<User>("user/me");

    public Task<bool> UpdateProfileAsync(string? name, string? phone, string? address)

        => PutAsync("user/me", new { FullName = name, Phone = phone, Address = address });

    // ─── Ratings ─────────────────────────────────────────────────────────────

    public async Task<bool> RateOrderAsync(int orderId, int restaurantRating, int? driverRating, string? comment)
    {
        var result = await PostAsync<object>("ratings", new
        {
            OrderId = orderId,
            RestaurantRating = restaurantRating,
            DriverRating = driverRating,
            Comment = comment
        });
        return result != null;
    }

    // ─── Chat ────────────────────────────────────────────────────────────────

    public Task<List<DriverChatMessage>?> GetChatMessagesAsync(int orderId)
        => GetAsync<List<DriverChatMessage>>($"chatmessages/{orderId}");

    // ─── Banners ─────────────────────────────────────────────────────────────

    public Task<List<Banner>?> GetBannersAsync()
        => GetAsync<List<Banner>>("banners");

    // ─── Coupons ─────────────────────────────────────────────────────────────

    public Task<List<Coupon>?> GetCouponsAsync()
        => GetAsync<List<Coupon>>("coupons");

    public Task<List<Coupon>?> GetMyCouponsAsync()
        => GetAsync<List<Coupon>>("coupons/my");

    public async Task<CouponValidationResult?> ValidateCouponAsync(string code, decimal orderAmount)
    {
        try
        {
            SetAuth();
            var r = await _http.PostAsJsonAsync($"{Base}/coupons/validate",
                new { Code = code, OrderAmount = orderAmount });
            if (r.IsSuccessStatusCode)
                return await r.Content.ReadFromJsonAsync<CouponValidationResult>(_json);
            var body = await r.Content.ReadAsStringAsync();
            try
            {
                var doc = System.Text.Json.JsonDocument.Parse(body);
                if (doc.RootElement.TryGetProperty("message", out var msg))
                    throw new ApiException(msg.GetString()!);
            }
            catch (ApiException) { throw; }
            catch { }
        }
        catch (ApiException) { throw; }
        catch (Exception ex) { Debug(ex, "coupons/validate"); }
        return null;
    }

    // ─── Deals ───────────────────────────────────────────────────────────────

    public Task<List<Deal>?> GetDealsAsync()
        => GetAsync<List<Deal>>("deals");

    public async Task<PointsResult> GetPointsAsync()
    {
        try
        {
            return await GetAsync<PointsResult>("user/points") ?? new PointsResult();
        }
        catch { return new PointsResult(); }
    }

    // ─── FCM Token ────────────────────────────────────────────────────────────

    public async Task UpdateFcmTokenAsync(string token)
    {
        try
        {
            SetAuth();
            if (string.IsNullOrEmpty(_auth.GetToken()))
            {
                System.Diagnostics.Debug.WriteLine("[API] user/fcm-token skipped — not logged in");
                return;
            }

            var body = new StringContent(
                JsonSerializer.Serialize(new
                {
                    token,
                    language = LocalizationService.Current.TwoLetterISOLanguageName
                }),
                System.Text.Encoding.UTF8, "application/json");
            var response = await _http.PutAsync($"{Base}/user/fcm-token", body);

            if (response.IsSuccessStatusCode)
                System.Diagnostics.Debug.WriteLine("[API] FCM token saved to backend");
            else
                System.Diagnostics.Debug.WriteLine($"[API] user/fcm-token failed: {(int)response.StatusCode} {response.ReasonPhrase}");
        }
        catch (Exception ex) { Debug(ex, "user/fcm-token"); }
    }

    public async Task UpdateLanguagePreferenceAsync()
    {
        try
        {
            SetAuth();
            if (string.IsNullOrEmpty(_auth.GetToken())) return;

            var body = new StringContent(
                JsonSerializer.Serialize(new
                {
                    language = LocalizationService.Current.TwoLetterISOLanguageName
                }),
                System.Text.Encoding.UTF8, "application/json");
            await _http.PutAsync($"{Base}/user/language", body);
        }
        catch (Exception ex) { Debug(ex, "user/language"); }
    }

    private static void Debug(Exception ex, string path)

        => System.Diagnostics.Debug.WriteLine($"[API] {path}: {ex.Message}");
    public Task<List<object>?> GetIceServersAsync()
       => GetAsync<List<object>>("webrtc/ice-servers");
    public Task<AgoraTokenResult?> GetAgoraTokenAsync(string channelName, uint uid = 0)
    => GetAsync<AgoraTokenResult>($"agora/token?channelName={Uri.EscapeDataString(channelName)}&uid={uid}");
}
public class AgoraTokenResult
{
    public string AppId { get; set; } = "";
    public string ChannelName { get; set; } = "";
    public uint Uid { get; set; }
    public string Token { get; set; } = "";
    public int ExpiresInSeconds { get; set; }
}
public class CouponValidationResult
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public decimal Discount { get; set; }
    public decimal FinalAmount { get; set; }
}

public class RedeemPointsResult
{
    public string Message { get; set; } = string.Empty;
    public string CouponCode { get; set; } = string.Empty;
    public decimal Discount { get; set; }
}