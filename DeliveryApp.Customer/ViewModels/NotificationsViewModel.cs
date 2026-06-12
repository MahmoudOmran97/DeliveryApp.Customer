using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeliveryApp.Customer.Models;
using DeliveryApp.Customer.Services;
using System.Collections.ObjectModel;

namespace DeliveryApp.Customer.ViewModels;

public partial class NotificationsViewModel : BaseViewModel

{

    readonly ApiService _api;

    [ObservableProperty] bool _isRefreshing;

    [ObservableProperty] bool _isEmpty;

    [ObservableProperty] int _unread;

    public ObservableCollection<Notification> Notifications { get; } = new();

    public NotificationsViewModel(ApiService api) { _api = api; }

    [RelayCommand]

    async Task LoadAsync()

    {

        IsBusy = true;

        try

        {

            var r = await _api.GetNotificationsAsync();

            Notifications.Clear();

            if (r != null)

            {

                foreach (var n in r.Data) Notifications.Add(n);

                Unread = r.Data.Count(n => !n.IsRead);

            }

            IsEmpty = !Notifications.Any();

        }

        finally { IsBusy = false; IsRefreshing = false; }

    }

    [RelayCommand] async Task Refresh() { IsRefreshing = true; await LoadAsync(); }

    [RelayCommand]

    async Task MarkAllRead() { await _api.MarkAllReadAsync(); await LoadAsync(); }

    [RelayCommand]

    async Task Tap(Notification n)

    {

        if (!n.IsRead) await _api.MarkNotificationReadAsync(n.Id);

        if (n.OrderId.HasValue)

            await Shell.Current.GoToAsync($"OrderDetailPage?orderId={n.OrderId}");

    }

}

