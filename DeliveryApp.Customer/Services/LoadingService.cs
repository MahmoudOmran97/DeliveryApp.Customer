using CommunityToolkit.Mvvm.ComponentModel;

namespace DeliveryApp.Customer.Services
{
    public partial class LoadingService : ObservableObject
    {
        private static LoadingService? _instance;
        public static LoadingService Instance => _instance ??= new LoadingService();

        [ObservableProperty]
        private bool _isBusy;

        private LoadingService() { }

        public void SetBusy(bool busy)
        {
            MainThread.BeginInvokeOnMainThread(() => IsBusy = busy);
        }
    }
}
