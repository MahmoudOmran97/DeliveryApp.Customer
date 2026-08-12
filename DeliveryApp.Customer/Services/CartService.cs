using DeliveryApp.Customer.Models;

namespace DeliveryApp.Customer.Services;

public class CartService
{
    private const string PrescriptionImageKey = "cart_prescription_image";
    private const string PrescriptionNotesKey = "cart_prescription_notes";
    private const string PrescriptionRequestIdKey = "cart_prescription_request_id";
    private const string PrescriptionAgreedPriceKey = "cart_prescription_agreed_price";
    private const string PrescriptionRestaurantIdKey = "cart_prescription_restaurant_id";
    private const string PrescriptionDeliveryFeeKey = "cart_prescription_delivery_fee";

    private readonly List<CartItem> _items = new();
    private int? _restaurantId;
    private decimal _restaurantDeliveryFee = 15m;

    public CartService() => RestorePrescription();

    public string? PrescriptionImageUrl { get; private set; }
    public string? PrescriptionNotes { get; private set; }
    public int? PrescriptionRequestId { get; private set; }
    public decimal? PrescriptionAgreedPrice { get; private set; }

    public event Action? CartChanged;

    public IReadOnlyList<CartItem> Items => _items.AsReadOnly();
    public int? RestaurantId => _restaurantId;
    public decimal RestaurantDeliveryFee => _restaurantDeliveryFee;
    public int TotalCount => _items.Sum(i => i.Quantity);
    public decimal TotalPrice => _items.Sum(i => i.TotalPrice);
    public bool IsEmpty => !_items.Any() && string.IsNullOrEmpty(PrescriptionImageUrl);
    public bool HasPrescription => !string.IsNullOrEmpty(PrescriptionImageUrl);

    public bool AddItem(
        int restaurantId,
        Product product,
        int qty = 1,
        string? notes = null,
        decimal deliveryFee = 15m,
        int? variantId = null,
        string? variantName = null,
        decimal? unitPrice = null,
        int? dealId = null)
    {
        if (_restaurantId.HasValue && _restaurantId != restaurantId)
            return false;

        _restaurantId = restaurantId;
        _restaurantDeliveryFee = deliveryFee;

        var price = unitPrice ?? product.EffectivePrice;
        var existing = _items.FirstOrDefault(i =>
            i.Product.Id == product.Id &&
            i.VariantId == variantId &&
            i.DealId == dealId &&
            i.Notes == notes);

        if (existing != null)
            existing.Quantity += qty;
        else
        {
            _items.Add(new CartItem
            {
                RestaurantId = restaurantId,
                Product = product,
                Quantity = qty,
                Notes = notes,
                VariantId = variantId,
                VariantName = variantName,
                UnitPrice = price,
                DealId = dealId
            });
        }

        CartChanged?.Invoke();
        return true;
    }

    public void SetPrescription(int restaurantId, string imageUrl, string? notes, decimal deliveryFee = 15m)
    {
        if (!_restaurantId.HasValue)
        {
            _restaurantId = restaurantId;
            _restaurantDeliveryFee = deliveryFee;
        }

        PrescriptionImageUrl = imageUrl;
        PrescriptionNotes = notes;
        PersistPrescription();
        CartChanged?.Invoke();
    }

    public void SetPrescriptionRequestId(int prescriptionRequestId)
    {
        PrescriptionRequestId = prescriptionRequestId;
        PersistPrescription();
        CartChanged?.Invoke();
    }

    public void SetPrescriptionAgreedPrice(int prescriptionRequestId, decimal price)
    {
        PrescriptionRequestId = prescriptionRequestId;
        PrescriptionAgreedPrice = price;
        PersistPrescription();
        CartChanged?.Invoke();
    }

    public void ClearPrescription()
    {
        PrescriptionImageUrl = null;
        PrescriptionNotes = null;
        PrescriptionRequestId = null;
        PrescriptionAgreedPrice = null;
        RemovePersistedPrescription();
        CartChanged?.Invoke();
    }

    public void UpdateQuantity(string lineKey, int qty)
    {
        var item = _items.FirstOrDefault(i => i.LineKey == lineKey);
        if (item == null) return;

        if (qty <= 0) RemoveItem(lineKey);
        else
        {
            item.Quantity = qty;
            CartChanged?.Invoke();
        }
    }

    public void RemoveItem(string lineKey)
    {
        _items.RemoveAll(i => i.LineKey == lineKey);
        if (_items.Count == 0 && string.IsNullOrEmpty(PrescriptionImageUrl))
        {
            _restaurantId = null;
            _restaurantDeliveryFee = 15m;
        }
        CartChanged?.Invoke();
    }

    public void Clear()
    {
        _items.Clear();
        _restaurantId = null;
        _restaurantDeliveryFee = 15m;
        PrescriptionImageUrl = null;
        PrescriptionNotes = null;
        PrescriptionRequestId = null;
        PrescriptionAgreedPrice = null;
        RemovePersistedPrescription();
        CartChanged?.Invoke();
    }

    private void RestorePrescription()
    {
        var imageUrl = Preferences.Get(PrescriptionImageKey, string.Empty);
        if (string.IsNullOrWhiteSpace(imageUrl)) return;

        PrescriptionImageUrl = imageUrl;
        PrescriptionNotes = Preferences.Get(PrescriptionNotesKey, string.Empty);

        var restaurantId = Preferences.Get(PrescriptionRestaurantIdKey, 0);
        _restaurantId = restaurantId > 0 ? restaurantId : null;
        _restaurantDeliveryFee = (decimal)Preferences.Get(PrescriptionDeliveryFeeKey, 15d);

        var requestId = Preferences.Get(PrescriptionRequestIdKey, 0);
        PrescriptionRequestId = requestId > 0 ? requestId : null;

        var agreedPrice = Preferences.Get(PrescriptionAgreedPriceKey, -1d);
        PrescriptionAgreedPrice = agreedPrice >= 0 ? (decimal)agreedPrice : null;
    }

    private void PersistPrescription()
    {
        if (string.IsNullOrWhiteSpace(PrescriptionImageUrl)) return;

        Preferences.Set(PrescriptionImageKey, PrescriptionImageUrl);
        Preferences.Set(PrescriptionNotesKey, PrescriptionNotes ?? string.Empty);
        Preferences.Set(PrescriptionRestaurantIdKey, _restaurantId ?? 0);
        Preferences.Set(PrescriptionDeliveryFeeKey, (double)_restaurantDeliveryFee);
        Preferences.Set(PrescriptionRequestIdKey, PrescriptionRequestId ?? 0);
        Preferences.Set(PrescriptionAgreedPriceKey, PrescriptionAgreedPrice.HasValue
            ? (double)PrescriptionAgreedPrice.Value
            : -1d);
    }

    private static void RemovePersistedPrescription()
    {
        Preferences.Remove(PrescriptionImageKey);
        Preferences.Remove(PrescriptionNotesKey);
        Preferences.Remove(PrescriptionRequestIdKey);
        Preferences.Remove(PrescriptionAgreedPriceKey);
        Preferences.Remove(PrescriptionRestaurantIdKey);
        Preferences.Remove(PrescriptionDeliveryFeeKey);
    }
}
