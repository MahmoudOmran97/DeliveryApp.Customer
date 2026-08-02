// ═══════════════════════════════════════════════════════════════
// DeliveryApp.Customer / ViewModels / PrescriptionRequestsViewModel.cs
// ملف جديد — قائمة "روشاتي" عشان العميل يقدر يرجع لأي شات روشتة قديم
// ═══════════════════════════════════════════════════════════════
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeliveryApp.Customer.Models;
using DeliveryApp.Customer.Services;

namespace DeliveryApp.Customer.ViewModels;

public partial class PrescriptionRequestsViewModel : BaseViewModel
{
    readonly ApiService _api;

    public ObservableCollection<PrescriptionRequest> Requests { get; } = new();

    public PrescriptionRequestsViewModel(ApiService api) => _api = api;

    [RelayCommand]
    async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            var list = await _api.GetMyPrescriptionRequestsAsync();
            Requests.Clear();
            if (list != null)
                foreach (var r in list) Requests.Add(r);
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    async Task OpenAsync(PrescriptionRequest req)
    {
        if (req is null) return;
        await Shell.Current.GoToAsync($"PrescriptionChatPage?requestId={req.Id}");
    }
}
