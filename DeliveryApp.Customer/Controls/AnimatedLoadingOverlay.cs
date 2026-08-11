using Microsoft.Maui.Controls.Shapes;

namespace DeliveryApp.Customer.Controls
{
    public class AnimatedLoadingOverlay : ContentView
    {
        private Image? _baseImg;
        private Image? _waveImg;
        private Image? _lineImg;
        private Image? _pinImg;
        private Grid _logoHost;
        private Grid _containerGrid;
        private bool _isAnimating;

        public static readonly BindableProperty IsLoadingProperty =
            BindableProperty.Create(
                nameof(IsLoading),
                typeof(bool),
                typeof(AnimatedLoadingOverlay),
                false,
                propertyChanged: (bindable, oldVal, newVal) =>
                    ((AnimatedLoadingOverlay)bindable).OnIsLoadingChanged((bool)newVal));

        public bool IsLoading
        {
            get => (bool)GetValue(IsLoadingProperty);
            set => SetValue(IsLoadingProperty, value);
        }

        public AnimatedLoadingOverlay()
        {
            IsVisible = false;
            Opacity = 0;
            InputTransparent = true;
            HorizontalOptions = LayoutOptions.Fill;
            VerticalOptions = LayoutOptions.Fill;
            ZIndex = 999999;

            const int layerSize = 56;

            _baseImg = new Image { Source = "logo_anim_base.png", WidthRequest = layerSize, HeightRequest = layerSize, Aspect = Aspect.AspectFit };
            _waveImg = new Image { Source = "logo_anim_speedwave.png", WidthRequest = layerSize, HeightRequest = layerSize, Aspect = Aspect.AspectFit };
            _lineImg = new Image { Source = "logo_anim_speedline.png", WidthRequest = layerSize, HeightRequest = layerSize, Aspect = Aspect.AspectFit };
            _pinImg = new Image { Source = "logo_anim_pin.png", WidthRequest = layerSize, HeightRequest = layerSize, Aspect = Aspect.AspectFit };

            _logoHost = new Grid
            {
                WidthRequest = layerSize,
                HeightRequest = layerSize,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                Children = { _baseImg, _waveImg, _lineImg, _pinImg }
            };

            var card = new Border
            {
                WidthRequest = 110,
                HeightRequest = 110,
                StrokeShape = new RoundRectangle { CornerRadius = 28 },
                BackgroundColor = Color.FromArgb("#FF5722"),
                Stroke = Colors.White,
                StrokeThickness = 2.5,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                Content = _logoHost,
                Shadow = new Shadow { Brush = Brush.Black, Offset = new Point(0, 8), Radius = 20, Opacity = 0.3f }
            };

            _containerGrid = new Grid
            {
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Fill,
                Children = { card }
            };

            UpdateBackgroundForTheme();
            Content = _containerGrid;

            Application.Current.RequestedThemeChanged += (s, e) => UpdateBackgroundForTheme();
        }

        private void UpdateBackgroundForTheme()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                var currentTheme = Application.Current.RequestedTheme;
                if (currentTheme == AppTheme.Unspecified)
                    currentTheme = Application.Current.PlatformAppTheme;

                if (currentTheme == AppTheme.Dark)
                    _containerGrid.BackgroundColor = Color.FromRgba(0, 0, 0, 0.6);
                else
                    _containerGrid.BackgroundColor = Color.FromRgba(255, 255, 255, 0.45);
            });
        }

        private void OnIsLoadingChanged(bool isLoading)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                this.AbortAnimation("OverlayFade");

                if (isLoading)
                {
                    UpdateBackgroundForTheme();
                    InputTransparent = false;
                    IsVisible = true;
                    StartSmoothAnimation();
                    await this.FadeTo(1, 200, Easing.CubicOut);
                }
                else
                {
                    InputTransparent = true;
                    await this.FadeTo(0, 400, Easing.CubicIn);
                    if (!IsLoading)
                    {
                        IsVisible = false;
                        StopSmoothAnimation();
                    }
                }
            });
        }

        private void StartSmoothAnimation()
        {
            if (_isAnimating) return;
            _isAnimating = true;

            // SIMPLE & SMOOTH: A unified gentle vertical float for the entire logo
            var floatAnim = new Animation(v => _logoHost.TranslationY = v, 0, -8, Easing.CubicInOut);
            floatAnim.Commit(this, "LogoFloat", 16, 1200, Easing.Linear, (v, c) => _logoHost.TranslationY = 0, () => true);

            // Very subtle independent pin movement
            var pinAnim = new Animation(v => _pinImg!.TranslationY = v, 0, -4, Easing.SinInOut);
            pinAnim.Commit(this, "PinFloat", 16, 800, Easing.Linear, (v, c) => _pinImg!.TranslationY = 0, () => true);
        }

        private void StopSmoothAnimation()
        {
            _isAnimating = false;
            this.AbortAnimation("LogoFloat");
            this.AbortAnimation("PinFloat");
            _logoHost.TranslationY = 0;
            _pinImg!.TranslationY = 0;
        }
    }
}
