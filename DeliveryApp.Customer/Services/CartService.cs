using DeliveryApp.Customer.Models;

namespace DeliveryApp.Customer.Services;

public class CartService

{

    private readonly List<CartItem> _items = new();

    private int? _restaurantId;

    public event Action? CartChanged;

    public IReadOnlyList<CartItem> Items => _items.AsReadOnly();

    public int? RestaurantId => _restaurantId;

    public int TotalCount => _items.Sum(i => i.Quantity);

    public decimal TotalPrice => _items.Sum(i => i.TotalPrice);

    public bool IsEmpty => !_items.Any();

    // returns false if cart belongs to a different restaurant

    public bool AddItem(int restaurantId, Product product, int qty = 1, string? notes = null)

    {

        if (_restaurantId.HasValue && _restaurantId != restaurantId)

            return false;

        _restaurantId = restaurantId;

        var existing = _items.FirstOrDefault(i => i.Product.Id == product.Id && i.Notes == notes);

        if (existing != null) existing.Quantity += qty;

        else _items.Add(new CartItem { RestaurantId = restaurantId, Product = product, Quantity = qty, Notes = notes });

        CartChanged?.Invoke();

        return true;

    }

    public void UpdateQuantity(int productId, int qty)

    {

        var item = _items.FirstOrDefault(i => i.Product.Id == productId);

        if (item == null) return;

        if (qty <= 0) RemoveItem(productId);

        else { item.Quantity = qty; CartChanged?.Invoke(); }

    }

    public void RemoveItem(int productId)

    {

        _items.RemoveAll(i => i.Product.Id == productId);

        if (_items.Count == 0) _restaurantId = null;

        CartChanged?.Invoke();

    }

    public void Clear()

    {

        _items.Clear();

        _restaurantId = null;

        CartChanged?.Invoke();

    }

}
