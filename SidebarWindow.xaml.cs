using Microsoft.Web.WebView2.Wpf;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace deepseek_copilot;

public partial class SidebarWindow : Window
{
    private const double BaseAnimationDurationMs = 250;
    private readonly AppSettings _settings;
    private double _sidebarWidth = 450;
    private DispatcherTimer? _unloadTimer;
    private WebView2? _webView;
    private bool _isOpeningSettings;

    public bool IsAnimating { get; private set; }
    public event Action? SettingsClicked;

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hwnd, int index);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hwnd, int index, int value);

    private const int GWL_STYLE = -16;
    private const int WS_THICKFRAME = 0x40000;
    private const int WM_NCCALCSIZE = 0x0083;

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern IntPtr GetAncestor(IntPtr hWnd, uint gaFlags);

    private const uint GA_ROOTOWNER = 3;

    private IntPtr _hwnd;

    public SidebarWindow(AppSettings settings)
    {
        _settings = settings;
        InitializeComponent();

        Icon = IconHelper.RenderToBitmapSource(16);
        TitleIcon.Source = IconHelper.RenderToBitmapSource(18);

        var workArea = SystemParameters.WorkArea;
        Left = workArea.Right;
        Top = workArea.Top;
        Height = workArea.Height;

        Deactivated += OnDeactivated;
        PreviewKeyDown += OnPreviewKeyDown;
        Loaded += OnLoaded;
    }

    public void ApplyTheme(ThemeMode theme)
    {
        _settings.SetThemeMode(theme);
        var isDark = _settings.IsDarkTheme;

        TitleBar.Background = new SolidColorBrush(isDark
            ? System.Windows.Media.Color.FromRgb(0x15, 0x15, 0x17)
            : System.Windows.Media.Color.FromRgb(0xFF, 0xFF, 0xFF));

        var textColor = isDark ? Colors.White : System.Windows.Media.Color.FromRgb(0x1A, 0x1A, 0x2E);
        TitleText.Foreground = new SolidColorBrush(textColor);

        var btnColor = isDark ? Colors.White : System.Windows.Media.Color.FromRgb(0x66, 0x66, 0x66);
        SettingsButton.Foreground = new SolidColorBrush(btnColor);
        CloseButton.Foreground = new SolidColorBrush(btnColor);

        SetWebViewColorScheme(isDark);
    }

    private void SetWebViewColorScheme(bool isDark)
    {
        if (_webView?.CoreWebView2 == null) return;
        _webView.CoreWebView2.Profile.PreferredColorScheme =
            isDark
                ? Microsoft.Web.WebView2.Core.CoreWebView2PreferredColorScheme.Dark
                : Microsoft.Web.WebView2.Core.CoreWebView2PreferredColorScheme.Light;
    }

    public void ApplyWidth(double width)
    {
        _sidebarWidth = width;
        Width = width;
        var workArea = SystemParameters.WorkArea;
        BeginAnimation(LeftProperty, null);
        Left = workArea.Right - width;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await CreateWebViewAsync();
    }

    private async Task CreateWebViewAsync()
    {
        if (_webView != null) return;

        var env = await Microsoft.Web.WebView2.Core.CoreWebView2Environment.CreateAsync(
            null,
            System.IO.Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),
                "DeepSeekCopilot", "WebView2"));

        _webView = new WebView2();
        WebViewHost.Children.Add(_webView);

        await _webView.EnsureCoreWebView2Async(env);
        _webView.CoreWebView2!.DocumentTitleChanged += OnTitleChanged;
        SetWebViewColorScheme(_settings.IsDarkTheme);
        _webView.CoreWebView2.Navigate(_settings.ChatUrl);
        TitleText.Text = _webView.CoreWebView2.DocumentTitle;
    }

    private void OnTitleChanged(object? sender, object e)
    {
        Dispatcher.Invoke(() =>
        {
            TitleText.Text = _webView?.CoreWebView2?.DocumentTitle ?? "DeepSeek";
        });
    }

    private void DestroyWebView()
    {
        _unloadTimer = null;

        if (_webView == null) return;

        _webView.CoreWebView2!.DocumentTitleChanged -= OnTitleChanged;
        WebViewHost.Children.Remove(_webView);
        _webView.Dispose();
        _webView = null;

        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    private void StartUnloadTimer()
    {
        _unloadTimer?.Stop();
        _unloadTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(_settings.UnloadDelaySeconds)
        };
        _unloadTimer.Tick += (_, _) =>
        {
            if (Visibility != Visibility.Visible)
                DestroyWebView();
        };
        _unloadTimer.Start();
    }

    public async void SlideIn()
    {
        if (IsAnimating) return;
        _unloadTimer?.Stop();

        IsAnimating = true;

        if (_webView == null)
        {
            Show();
            await CreateWebViewAsync();
            AnimateIn();
        }
        else
        {
            Show();
            AnimateIn();
        }
    }

    private void AnimateIn()
    {
        var workArea = SystemParameters.WorkArea;
        var anim = new DoubleAnimation
        {
            From = workArea.Right,
            To = workArea.Right - _sidebarWidth,
            Duration = TimeSpan.FromMilliseconds(BaseAnimationDurationMs / _settings.AnimationSpeed),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };
        anim.Completed += (_, _) =>
        {
            IsAnimating = false;
            _webView?.Focus();
        };
        BeginAnimation(LeftProperty, anim);
    }

    public void SlideOut()
    {
        if (IsAnimating) return;

        IsAnimating = true;
        var anim = new DoubleAnimation
        {
            From = Left,
            To = SystemParameters.WorkArea.Right,
            Duration = TimeSpan.FromMilliseconds(BaseAnimationDurationMs / _settings.AnimationSpeed),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
        };
        anim.Completed += (_, _) =>
        {
            IsAnimating = false;
            Hide();
            StartUnloadTimer();
        };
        BeginAnimation(LeftProperty, anim);
    }

    private void OnSettingsClick(object sender, RoutedEventArgs e)
    {
        _isOpeningSettings = true;
        SettingsClicked?.Invoke();
        _isOpeningSettings = false;
    }

    private void OnDeactivated(object? sender, EventArgs e)
    {
        if (_isOpeningSettings) return;
        if (Visibility != Visibility.Visible) return;

        var fg = GetForegroundWindow();
        if (fg != IntPtr.Zero)
        {
            var ourHwnd = new WindowInteropHelper(this).Handle;
            var root = GetAncestor(fg, GA_ROOTOWNER);
            if (root == ourHwnd) return;
        }

        SlideOut();
    }

    private void OnPreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Escape)
        {
            SlideOut();
        }
    }

    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        SlideOut();
    }
}
