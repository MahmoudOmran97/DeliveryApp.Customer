using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace DeliveryApp.Customer.ViewModels;

public partial class BaseViewModel : ObservableObject

{

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    private bool _isBusy;

    [ObservableProperty]
    private string _title = string.Empty;

    public bool IsNotBusy => !IsBusy;

    // ── Back navigation (works inside Shell tabs and pushed pages) ──
    [RelayCommand]
    protected static async Task GoBackAsync()
    {
        try
        {
            await Shell.Current.GoToAsync("..");
        }
        catch
        {
            // Fallback for NavigationPage stack
            if (Shell.Current?.Navigation?.NavigationStack?.Count > 1)
                await Shell.Current.Navigation.PopAsync();
        }
    }

    protected static async Task AlertAsync(string msg) =>
        await Shell.Current.DisplayAlert(
            Services.LocalizationService.Get("Notice"), msg,
            Services.LocalizationService.Get("Ok"));

}