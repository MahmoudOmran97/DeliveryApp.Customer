using System.Collections.ObjectModel;

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
