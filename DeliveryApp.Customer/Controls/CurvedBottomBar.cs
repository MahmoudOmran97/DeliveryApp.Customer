using DeliveryApp.Customer.Services;
using Microsoft.Maui.Controls.Shapes;

namespace DeliveryApp.Customer.Controls
{
    /// <summary>
    /// Bottom navigation bar with a curved wave that follows the selected tab.
    /// The Home tab icon shows a layered logo animation identical to the HomePage header:
    ///   • HomeBase     – static background layer
    ///   • HomeWave     – horizontal oscillation (fast, like SpeedWave)
    ///   • HomeLine     – horizontal oscillation (slightly offset, like SpeedLine)
    ///   • HomePin      – vertical float (like LogoPin)
    /// All animation loops are driven by a CancellationTokenSource that is
    /// started/stopped when the Home tab is selected/deselected.
    /// </summary>
    public class CurvedBottomBar : ContentView
    {
        // ──────────────────────────────────────────────
        // Bindable property
        // ──────────────────────────────────────────────
        public static readonly BindableProperty SelectedTabProperty =
            BindableProperty.Create(
                nameof(SelectedTab),
                typeof(string),
                typeof(CurvedBottomBar),
                "home",
                propertyChanged: (bindable, oldVal, newVal) =>
                    ((CurvedBottomBar)bindable).OnSelectedTabChanged((string)oldVal, (string)newVal));

        // ──────────────────────────────────────────────
        // Private state
        // ──────────────────────────────────────────────
        private readonly GraphicsView _background;
        private readonly CurvedBarDrawable _drawable = new();
        private readonly List<(Border Bubble, Grid IconHost, Label Label)> _items = new();

        // Home-specific animated layers (indices match _tabs)
        private Image? _homeBase;
        private Image? _homeWave;
        private Image? _homeLine;
        private Image? _homePin;
        private CancellationTokenSource? _homeAnimCts;

        // Tab definitions (same order as before)
        private readonly (string Key, string Route, string LabelKey,
                           string? StaticIcon, bool IsAnimatedHome)[] _tabs =
        {
            ("orders",        "//OrdersPage",       "Tab_Orders", "tab_orders.svg",        false),
            ("notifications", "//NotificationsPage","Tab_Alerts", "tab_notifications.svg", false),
            ("home",          "//HomePage",         "Tab_Home",   null,                    true ),
            ("profile",       "//ProfilePage",      "Tab_Profile","tab_profile.svg",       false),
            ("settings",      "//SettingsPage",     "Tab_Settings","tab_settings.svg",     false),
        };

        // ──────────────────────────────────────────────
        // Properties
        // ──────────────────────────────────────────────
        public string SelectedTab
        {
            get => (string)GetValue(SelectedTabProperty);
            set => SetValue(SelectedTabProperty, value);
        }

        // ──────────────────────────────────────────────
        // Constructor
        // ──────────────────────────────────────────────
        public CurvedBottomBar()
        {
            FlowDirection = FlowDirection.LeftToRight;
            HorizontalOptions = LayoutOptions.Fill;
            VerticalOptions = LayoutOptions.End;
            Padding = 0;
            Margin = 0;
            HeightRequest = 92;

            // ── Background wave graphic ──────────────
            _background = new GraphicsView
            {
                Drawable = _drawable,
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Fill
            };

            // ── Button grid ──────────────────────────
            var buttonGrid = new Grid
            {
                FlowDirection = FlowDirection.LeftToRight,
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Fill,
                Padding = new Thickness(0, 8, 0, 0),
                ColumnSpacing = 0
            };
            for (int i = 0; i < 5; i++)
                buttonGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));

            // ── Build each tab ───────────────────────
            for (int index = 0; index < _tabs.Length; index++)
            {
                var tab = _tabs[index];

                // Bubble (the circular highlight that appears when selected)
                var bubble = new Border
                {
                    WidthRequest = 54,
                    HeightRequest = 54,
                    StrokeShape = new RoundRectangle { CornerRadius = 27 },
                    Stroke = Colors.Transparent,
                    BackgroundColor = Colors.Transparent,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Start
                };

                // Icon host – a fixed-size Grid that holds either a single static icon
                // or the four animated logo layers for the Home tab.
                Grid iconHost;

                if (tab.IsAnimatedHome)
                {
                    iconHost = BuildAnimatedHomeIcon();
                }
                else
                {
                    iconHost = new Grid
                    {
                        WidthRequest = 24,
                        HeightRequest = 24,
                        HorizontalOptions = LayoutOptions.Center,
                        VerticalOptions = LayoutOptions.Center
                    };
                    iconHost.Children.Add(new Image
                    {
                        Source = tab.StaticIcon!,
                        WidthRequest = 22,
                        HeightRequest = 22,
                        HorizontalOptions = LayoutOptions.Center,
                        VerticalOptions = LayoutOptions.Center
                    });
                }

                bubble.Content = iconHost;

                // Label
                var label = new Label
                {
                    Text = LocalizationService.Get(tab.LabelKey),
                    FontSize = 12,
                    HorizontalOptions = LayoutOptions.Center,
                    FontFamily = "CairoBold",
                    Margin = new Thickness(0)
                };

                // Container stack
                var stack = new VerticalStackLayout
                {
                    FlowDirection = FlowDirection.LeftToRight,
                    Spacing = 1,
                    HorizontalOptions = LayoutOptions.Fill,
                    VerticalOptions = LayoutOptions.Start,
                    Margin = new Thickness(0, -2, 0, 0),
                    Children = { bubble, label }
                };

                // Navigation tap
                var tap = new TapGestureRecognizer();
                var route = tab.Route;
                var tabKey = tab.Key;
                tap.Tapped += async (_, _) =>
                {
                    if (string.Equals(SelectedTab, tabKey, StringComparison.OrdinalIgnoreCase))
                        return;
                    if (Shell.Current is not null)
                        await Shell.Current.GoToAsync(route);
                };
                stack.GestureRecognizers.Add(tap);

                buttonGrid.Add(stack);
                Grid.SetColumn(stack, index);
                _items.Add((bubble, iconHost, label));
            }

            // ── Root overlay grid ────────────────────
            var layout = new Grid
            {
                FlowDirection = FlowDirection.LeftToRight,
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Fill,
                RowDefinitions = { new RowDefinition(GridLength.Star) }
            };
            layout.Children.Add(_background);
            layout.Children.Add(buttonGrid);

            Content = layout;
            RefreshState(null, SelectedTab);
        }

        // ──────────────────────────────────────────────
        // Build the animated Home icon layers
        // ──────────────────────────────────────────────
        private Grid BuildAnimatedHomeIcon()
        {
            // All four layers stack on top of each other.
            // Sizes are intentionally small (fits inside 54×54 bubble) but
            // AspectFit keeps the artwork proportional.
            const int LayerSize = 46;

            _homeBase = new Image
            {
                Source = "logo_anim_base.png",
                WidthRequest = LayerSize,
                HeightRequest = LayerSize,
                Aspect = Aspect.AspectFit,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            };
            _homeWave = new Image
            {
                Source = "logo_anim_speedwave.png",
                WidthRequest = LayerSize,
                HeightRequest = LayerSize,
                Aspect = Aspect.AspectFit,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            };
            _homeLine = new Image
            {
                Source = "logo_anim_speedline.png",
                WidthRequest = LayerSize,
                HeightRequest = LayerSize,
                Aspect = Aspect.AspectFit,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            };
            _homePin = new Image
            {
                Source = "logo_anim_pin.png",
                WidthRequest = LayerSize,
                HeightRequest = LayerSize,
                Aspect = Aspect.AspectFit,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            };

            var host = new Grid
            {
                WidthRequest = LayerSize,
                HeightRequest = LayerSize,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            };
            host.Children.Add(_homeBase);
            host.Children.Add(_homeWave);
            host.Children.Add(_homeLine);
            host.Children.Add(_homePin);

            return host;
        }

        // ──────────────────────────────────────────────
        // Tab-change logic
        // ──────────────────────────────────────────────
        private void OnSelectedTabChanged(string? oldTab, string? newTab)
        {
            // Stop Home animation if we left the Home tab
            if (!string.Equals(newTab, "home", StringComparison.OrdinalIgnoreCase))
                StopHomeAnimation();

            RefreshState(oldTab, newTab);
        }

        private void RefreshState(string? oldTab, string? newTab)
        {
            var selectedIndex = Array.FindIndex(
                _tabs, t => string.Equals(t.Key, newTab, StringComparison.OrdinalIgnoreCase));
            if (selectedIndex < 0) selectedIndex = 2; // fallback to home

            _drawable.SelectedIndex = selectedIndex;
            _background.Invalidate();

            for (int i = 0; i < _items.Count; i++)
            {
                var active = i == selectedIndex;
                _items[i].Bubble.BackgroundColor = active ? Color.FromArgb("#FF5722") : Colors.Transparent;
                _items[i].Label.TextColor = active ? Color.FromArgb("#FF5722") : Color.FromArgb("#D1D8DB");

                // Opacity for static icons; animated home layers handled separately
                if (!_tabs[i].IsAnimatedHome)
                {
                    var img = _items[i].IconHost.Children.OfType<Image>().FirstOrDefault();
                    if (img is not null) img.Opacity = active ? 1.0 : 0.78;
                }
            }

            // Start or stop Home animation based on selection
            if (selectedIndex == 2 /* home */)
                StartHomeAnimation();
            else
                StopHomeAnimation();
        }

        // ──────────────────────────────────────────────
        // Home icon animation  (mirrors HomePage.xaml.cs)
        // ──────────────────────────────────────────────
        private void StartHomeAnimation()
        {
            if (_homeBase is null) return;        // shouldn't happen
            StopHomeAnimation();                   // always start fresh

            _homeAnimCts = new CancellationTokenSource();
            var token = _homeAnimCts.Token;

            _ = AnimatePinLoopAsync(token);
            _ = AnimateWaveLoopAsync(token);
            _ = AnimateLineLoopAsync(token);
        }

        private void StopHomeAnimation()
        {
            _homeAnimCts?.Cancel();
            _homeAnimCts?.Dispose();
            _homeAnimCts = null;

            // Reset layer transforms gracefully (no await needed – fire and forget)
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                if (_homePin is not null)
                    await _homePin.TranslateTo(0, 0, 200, Easing.SinOut);
                if (_homeWave is not null)
                    await _homeWave.TranslateTo(0, 0, 200, Easing.SinOut);
                if (_homeLine is not null)
                    await _homeLine.TranslateTo(0, 0, 200, Easing.SinOut);
            });
        }

        // Pin: float up/down (same timing as HomePage — 1100 ms SinInOut)
        private async Task AnimatePinLoopAsync(CancellationToken token)
        {
            if (_homePin is null) return;
            try
            {
                while (!token.IsCancellationRequested)
                {
                    await _homePin.TranslateTo(0, -5, 1100, Easing.SinInOut);
                    if (token.IsCancellationRequested) break;
                    await _homePin.TranslateTo(0, 0, 1100, Easing.SinInOut);
                }
            }
            catch (ObjectDisposedException) { }
            catch (TaskCanceledException) { }
        }

        // Speed-wave: horizontal oscillation (same timing as HomePage — 550 / 550 / 450 + 300 pause)
        private async Task AnimateWaveLoopAsync(CancellationToken token)
        {
            if (_homeWave is null) return;
            try
            {
                while (!token.IsCancellationRequested)
                {
                    await _homeWave.TranslateTo(-7, 0, 550, Easing.SinInOut);
                    if (token.IsCancellationRequested) break;
                    await _homeWave.TranslateTo(4, 0, 550, Easing.SinInOut);
                    if (token.IsCancellationRequested) break;
                    await _homeWave.TranslateTo(0, 0, 450, Easing.SinInOut);
                    if (token.IsCancellationRequested) break;
                    await Task.Delay(300, token);
                }
            }
            catch (ObjectDisposedException) { }
            catch (TaskCanceledException) { }
        }

        // Speed-line: offset start (150 ms), then same pattern but slightly wider (500 / 500 / 400 + 300)
        private async Task AnimateLineLoopAsync(CancellationToken token)
        {
            if (_homeLine is null) return;
            try
            {
                await Task.Delay(150, token);
                while (!token.IsCancellationRequested)
                {
                    await _homeLine.TranslateTo(-10, 0, 500, Easing.SinInOut);
                    if (token.IsCancellationRequested) break;
                    await _homeLine.TranslateTo(4, 0, 500, Easing.SinInOut);
                    if (token.IsCancellationRequested) break;
                    await _homeLine.TranslateTo(0, 0, 400, Easing.SinInOut);
                    if (token.IsCancellationRequested) break;
                    await Task.Delay(300, token);
                }
            }
            catch (ObjectDisposedException) { }
            catch (TaskCanceledException) { }
        }
    }

    // ────────────────────────────────────────────────────
    // Curved wave background — unchanged from original
    // ────────────────────────────────────────────────────
    internal sealed class CurvedBarDrawable : IDrawable
    {
        public int SelectedIndex { get; set; } = 2;

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            canvas.SaveState();
            canvas.Antialias = true;

            const float topRadius = 24f;
            const float sideInset = 20f;
            const float waveHalf = 34f;
            const float waveDepth = 20f;

            var barColor = Color.FromArgb("#1F2A30");
            var top = 0f;
            var width = dirtyRect.Width;
            var height = dirtyRect.Height;
            var itemWidth = (width - (sideInset * 2)) / 5f;
            var rawCx = sideInset + (itemWidth * SelectedIndex) + (itemWidth / 2f);
            var cx = Math.Clamp(rawCx, waveHalf + 4f, width - waveHalf - 4f);

            var path = new PathF();
            path.MoveTo(0, top + topRadius);
            path.QuadTo(0, top, topRadius, top);

            if (cx - waveHalf > topRadius)
                path.LineTo(cx - waveHalf, top);

            path.CurveTo(cx - 20f, top, cx - 18f, top + waveDepth, cx, top + waveDepth);
            path.CurveTo(cx + 18f, top + waveDepth, cx + 20f, top, cx + waveHalf, top);

            path.LineTo(width - topRadius, top);
            path.QuadTo(width, top, width, top + topRadius);
            path.LineTo(width, height);
            path.LineTo(0, height);
            path.Close();

            canvas.FillColor = barColor;
            canvas.FillPath(path);
            canvas.RestoreState();
        }
    }
}