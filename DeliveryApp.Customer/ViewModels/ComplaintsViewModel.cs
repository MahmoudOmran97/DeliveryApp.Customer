using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeliveryApp.Customer.Models;
using DeliveryApp.Customer.Services;

namespace DeliveryApp.Customer.ViewModels;

public partial class ComplaintsViewModel : BaseViewModel
{
    readonly ApiService _api;

    [ObservableProperty] bool _isRefreshing;
    [ObservableProperty] bool _isFormOpen;
    [ObservableProperty] string _newSubject = string.Empty;
    [ObservableProperty] string _newDescription = string.Empty;
    [ObservableProperty] bool _isSubmitting;
    [ObservableProperty] string _feedbackMessage = string.Empty;
    [ObservableProperty] bool _hasFeedback;

    public ObservableCollection<ComplaintDto> Complaints { get; } = new();
    public bool HasComplaints => Complaints.Count > 0;

    public ComplaintsViewModel(ApiService api) => _api = api;

    [RelayCommand]
    async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            var list = await _api.GetMyComplaintsAsync();
            Complaints.Clear();
            foreach (var c in (list ?? new()).OrderByDescending(x => x.CreatedAt))
                Complaints.Add(c);
            OnPropertyChanged(nameof(HasComplaints));
        }
        finally
        {
            IsBusy = false;
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    void ToggleForm() => IsFormOpen = !IsFormOpen;

    [RelayCommand]
    async Task Submit()
    {
        if (string.IsNullOrWhiteSpace(NewSubject) || string.IsNullOrWhiteSpace(NewDescription))
        {
            ShowFeedback("اكتب عنوان ووصف للشكوى الأول");
            return;
        }

        IsSubmitting = true;
        try
        {
            var result = await _api.CreateComplaintAsync(NewSubject.Trim(), NewDescription.Trim());
            if (result != null)
            {
                NewSubject = string.Empty;
                NewDescription = string.Empty;
                IsFormOpen = false;
                ShowFeedback("تم إرسال الشكوى بنجاح، هنراجعها في أقرب وقت");
                await LoadAsync();
            }
            else
            {
                ShowFeedback("حصل خطأ، حاول تاني");
            }
        }
        finally
        {
            IsSubmitting = false;
        }
    }

    void ShowFeedback(string msg)
    {
        FeedbackMessage = msg;
        HasFeedback = true;
        _ = HideFeedbackAfterDelay();
    }

    async Task HideFeedbackAfterDelay()
    {
        await Task.Delay(3000);
        HasFeedback = false;
    }

    [RelayCommand]
    static async Task GoBack() => await Shell.Current.GoToAsync("..");
}
