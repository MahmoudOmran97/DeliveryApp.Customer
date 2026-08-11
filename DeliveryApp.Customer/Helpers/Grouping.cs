using System.Collections.ObjectModel;
using DeliveryApp.Customer.Models;

namespace DeliveryApp.Customer.Helpers;

/// <summary>
/// Wrapper بيخلي أي مجموعة (زي كاتيجوري ومنتجاتها) تتعرض جوه CollectionView واحد
/// بخاصية IsGrouped، بدل ما نعمل CollectionView منفصل لكل مجموعة جوه ScrollView.
/// ده اللي بيرجّع الـ virtualization/recycling بتاع الـ CollectionView (بيرندر بس
/// اللي ظاهر على الشاشة، مش كل العناصر مرة واحدة).
/// </summary>
public class Grouping<TKey, TItem> : ObservableCollection<TItem>
{
    public TKey Key { get; }

    public Grouping(TKey key, IEnumerable<TItem> items) : base(items)
    {
        Key = key;
    }
}

/// <summary>
/// مجموعة منتجات صفحة المطعم مع اسم قسم صريح للربط في GroupHeaderTemplate.
/// هذا يمنع ظهور شريط القسم فارغًا عند اختلاف سياق الـ Binding داخل CollectionView.
/// </summary>
public sealed class RestaurantMenuGroup : Grouping<Category, Product>
{
    public string SectionName => Key?.Name ?? string.Empty;

    public RestaurantMenuGroup(Category category, IEnumerable<Product> products)
        : base(category, products)
    {
    }
}
