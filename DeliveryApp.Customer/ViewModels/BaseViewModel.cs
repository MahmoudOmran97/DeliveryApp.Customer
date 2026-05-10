using CommunityToolkit.Mvvm.ComponentModel;

namespace DeliveryApp.Customer.ViewModels;

public partial class BaseViewModel : ObservableObject

{

    [ObservableProperty]

    [NotifyPropertyChangedFor(nameof(IsNotBusy))]

    private bool _isBusy;

    [ObservableProperty]

    private string _title = string.Empty;

    public bool IsNotBusy => !IsBusy;

    protected static async Task AlertAsync(string msg) =>

        await Shell.Current.DisplayAlert("Notice", msg, "OK");

}

