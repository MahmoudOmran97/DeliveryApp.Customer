namespace DeliveryApp.Customer.Views;

// شاشة عرض الصورة بحجمها الكامل مع إمكانية الزوم (Pinch) والتحريك (Pan)
// وضغطتين سريعتين للريست. بتتفتح بتمرير رابط الصورة عبر ?url=
[QueryProperty(nameof(ImageUrl), "url")]
public partial class ImageZoomPage : ContentPage
{
    double _currentScale = 1;
    double _startScale = 1;
    double _xOffset = 0;
    double _yOffset = 0;

    string _imageUrl = "";
    public string ImageUrl
    {
        get => _imageUrl;
        set
        {
            _imageUrl = Uri.UnescapeDataString(value ?? "");
            if (!string.IsNullOrWhiteSpace(_imageUrl))
                ZoomImage.Source = _imageUrl;
        }
    }

    public ImageZoomPage()
    {
        InitializeComponent();
    }

    void OnPinchUpdated(object? sender, PinchGestureUpdatedEventArgs e)
    {
        if (e.Status == GestureStatus.Started)
        {
            _startScale = ZoomImage.Scale;
            ZoomImage.AnchorX = 0.5;
            ZoomImage.AnchorY = 0.5;
        }
        else if (e.Status == GestureStatus.Running)
        {
            _currentScale = Math.Max(1, Math.Min(_startScale * e.Scale, 5));
            ZoomImage.Scale = _currentScale;
        }
        else if (e.Status == GestureStatus.Completed)
        {
            // لو رجع لأصله (زووم = 1) نظبط مكان الصورة تاني
            if (_currentScale <= 1.01)
            {
                _currentScale = 1;
                _xOffset = 0;
                _yOffset = 0;
                ZoomImage.Scale = 1;
                ZoomImage.TranslationX = 0;
                ZoomImage.TranslationY = 0;
            }
        }
    }

    void OnPanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        // مسموح تحرك الصورة بس وهي متكبّرة (Scale > 1)
        if (_currentScale <= 1) return;

        switch (e.StatusType)
        {
            case GestureStatus.Running:
                ZoomImage.TranslationX = _xOffset + e.TotalX;
                ZoomImage.TranslationY = _yOffset + e.TotalY;
                break;
            case GestureStatus.Completed:
                _xOffset = ZoomImage.TranslationX;
                _yOffset = ZoomImage.TranslationY;
                break;
        }
    }

    async void OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        _currentScale = 1;
        _xOffset = 0;
        _yOffset = 0;
        await ZoomImage.ScaleTo(1, 200);
        await Task.WhenAll(
            ZoomImage.TranslateTo(0, 0, 200));
    }

    async void OnCloseTapped(object? sender, TappedEventArgs e) => await Shell.Current.GoToAsync("..");
}
