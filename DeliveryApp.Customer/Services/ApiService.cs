using System.Net.Http.Json;

using System.Text.Json;

using DeliveryApp.Customer.Models;

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

    public Task<LoginResponse?> RegisterAsync(string name, string email, string password, string phone)

        => PostAsync<LoginResponse>("auth/register", new { FullName = name, Email = email, Password = password, Phone = phone, Role = "Customer" });

    // ─── Restaurants ─────────────────────────────────────────────────────────

    public Task<PagedResult<Restaurant>?> GetRestaurantsAsync(string? search = null, string sortBy = "rating", int page = 1)

    {

        var q = $"restaurants?page={page}&sortBy={sortBy}";

        if (!string.IsNullOrEmpty(search)) q += $"&search={Uri.EscapeDataString(search)}";

        return GetAsync<PagedResult<Restaurant>>(q);

    }

    public Task<Restaurant?> GetRestaurantAsync(int id)

        => GetAsync<Restaurant>($"restaurants/{id}");

    public Task<List<Category>?> GetMenuAsync(int restaurantId)

        => GetAsync<List<Category>>($"restaurants/{restaurantId}/menu");

    // ─── Orders ──────────────────────────────────────────────────────────────

    public Task<Order?> PlaceOrderAsync(

        int restaurantId, List<CartItem> items,

        string address, double lat, double lng,

        string? notes, string paymentMethod, string? couponCode = null, int? couponId = null)

        => PostAsync<Order>("orders", new

        {

            RestaurantId = restaurantId,

            Items = items.Select(i => new { ProductId = i.Product.Id, i.Quantity, i.Notes }),

            DeliveryAddress = address,

            DeliveryLatitude = lat,

            DeliveryLongitude = lng,

            DeliveryNotes = notes,

            PaymentMethod = paymentMethod,

            CouponCode = couponCode,

            CouponId = couponId

        });

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

    private static void Debug(Exception ex, string path)

        => System.Diagnostics.Debug.WriteLine($"[API] {path}: {ex.Message}");

}

public class CouponValidationResult
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public decimal Discount { get; set; }
    public decimal FinalAmount { get; set; }
}

