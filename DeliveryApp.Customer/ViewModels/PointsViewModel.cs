using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeliveryApp.Customer.Models;
using DeliveryApp.Customer.Services;

namespace DeliveryApp.Customer.ViewModels;

public partial class PointsViewModel : BaseViewModel
{
    readonly ApiService _api;

    [ObservableProperty] int _pointsBalance;
    [ObservableProperty] bool _isRefreshing;

    public ObservableCollection<PointTransaction> Transactions { get; } = new();
    public ObservableCollection<PointTransaction> FilteredTransactions { get; } = new();

    [ObservableProperty] string _selectedTab = "All"; // All, Earned, Spent

    public PointsViewModel(ApiService api)
    {
        _api = api;
        Title = "سجل النقاط";
    }

    [RelayCommand]
    async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            // Note: We will need to add GetPointsAsync to ApiService
            // For now, let's assume it returns a balance and a list of transactions
            var result = await _api.GetPointsAsync();
            PointsBalance = result.Balance;
            Transactions.Clear();
            foreach (var t in result.Transactions)
                Transactions.Add(t);
            
            FilterTransactions();
        }
        catch (Exception)
        {
            // Handle error
        }
        finally { IsBusy = false; IsRefreshing = false; }
    }

    [RelayCommand]
    void FilterTransactions()
    {
        FilteredTransactions.Clear();
        var query = SelectedTab switch
        {
            "Earned" => Transactions.Where(t => t.Amount > 0),
            "Spent" => Transactions.Where(t => t.Amount < 0),
            _ => Transactions
        };

        foreach (var t in query)
            FilteredTransactions.Add(t);
    }

    [RelayCommand]
    async Task RefreshAsync() { IsRefreshing = true; await LoadAsync(); }

    [RelayCommand]
    async Task BackAsync() => await Shell.Current.GoToAsync("..");
}

public class PointTransaction
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public string AmountText => (Amount > 0 ? "+" : "") + Amount.ToString();
    public string DateText => Date.ToString("yyyy/MM/dd");
}

public class PointsResult
{
    public int Balance { get; set; }
    public List<PointTransaction> Transactions { get; set; } = new();
}
