using Microsoft.Web.WebView2.Wpf;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace deepseek_copilot;

public partial class SidebarWindow : Window
{
    private const double AnimationDurationMs = 250;
    private readonly AppSettings _settings;
    private double _sidebarWidth = 450;
    private DispatcherTimer? _unloadTimer;
    private WebView2? _webView;
    private bool _isOpeningSettings;

    public bool IsAnimating { get; private set; }
    public event Action? SettingsClicked;

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern IntPtr GetAncestor(IntPtr hWnd, uint gaFlags);

    private const uint GA_ROOTOWNER = 3;

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

        _webView = new WebView2 { Source = new Uri(_settings.ChatUrl) };
        WebViewHost.Children.Add(_webView);

        await _webView.EnsureCoreWebView2Async();
        _webView.CoreWebView2!.DocumentTitleChanged += OnTitleChanged;
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
            Duration = TimeSpan.FromMilliseconds(AnimationDurationMs),
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
            Duration = TimeSpan.FromMilliseconds(AnimationDurationMs),
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
