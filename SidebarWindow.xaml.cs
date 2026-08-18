using KBEHtool;
using Microsoft.Web.WebView2.Wpf;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace deepseek_copilot;

public partial class SidebarWindow : Window
{
    private const double BaseAnimationDurationMs = 250;
    private readonly AppSettings _settings;
    private double _sidebarWidth = 960;
    private DispatcherTimer? _unloadTimer;
    private readonly DispatcherTimer _hideTimer;
    private WebView2? _webView;
    private bool _isOpeningSettings;
    private TaskCompletionSource<bool>? _slideInTcs;
    private System.Windows.Media.Brush? _iconHover;
    private System.Windows.Media.Brush _tabHover = System.Windows.Media.Brushes.Transparent;

    public bool IsAnimating { get; private set; }
    public event Action? SettingsClicked;

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern IntPtr GetAncestor(IntPtr hWnd, uint gaFlags);

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    private const uint GA_ROOTOWNER = 3;
    private const int VK_LBUTTON = 0x01;
    private const int MouseKeyDown = 0x8000;

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    public SidebarWindow(AppSettings settings)
    {
        _settings = settings;
        InitializeComponent();
        UpdateModeUi();

        Icon = IconHelper.RenderToBitmapSource(16);
        TitleIcon.Source = IconHelper.RenderToBitmapSource(18);

        var workArea = SystemParameters.WorkArea;
        Left = workArea.Right;
        Top = workArea.Top;
        Height = workArea.Height;

        Deactivated += OnDeactivated;
        PreviewKeyDown += OnPreviewKeyDown;
        Loaded += OnLoaded;

        SettingsButton.MouseEnter += (_, _) => SettingsButton.Background = _iconHover ?? System.Windows.Media.Brushes.Transparent;
        SettingsButton.MouseLeave += (_, _) => SettingsButton.Background = System.Windows.Media.Brushes.Transparent;
        CloseButton.MouseEnter += (_, _) => CloseButton.Background = _iconHover ?? System.Windows.Media.Brushes.Transparent;
        CloseButton.MouseLeave += (_, _) => CloseButton.Background = System.Windows.Media.Brushes.Transparent;
        ChatTab.MouseEnter += (_, _) => TabHover(ChatTab, false);
        ChatTab.MouseLeave += (_, _) => UpdateModeUi();
        AgentTab.MouseEnter += (_, _) => TabHover(AgentTab, true);
        AgentTab.MouseLeave += (_, _) => UpdateModeUi();

        _hideTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        _hideTimer.Tick += OnHideTick;
    }

    private void TabHover(System.Windows.Controls.Border tab, bool isAgent)
    {
        if (isAgent == _settings.IsAgentMode) return;
        tab.Background = _tabHover;
    }

    public void ApplyTheme(ThemeMode theme)
    {
        _settings.SetThemeMode(theme);
        ApplyOpacity(_settings.SidebarOpacity);

        var isDark = _settings.IsDarkTheme;
        var textColor = isDark ? Colors.White : System.Windows.Media.Color.FromRgb(0x1A, 0x1A, 0x2E);
        TitleText.Foreground = new SolidColorBrush(textColor);

        var btnColor = isDark ? Colors.White : System.Windows.Media.Color.FromRgb(0x66, 0x66, 0x66);
        SettingsButtonText.Foreground = new SolidColorBrush(btnColor);
        CloseButtonText.Foreground = new SolidColorBrush(btnColor);
        _iconHover = new SolidColorBrush(isDark
            ? System.Windows.Media.Color.FromArgb(0x26, 0xFF, 0xFF, 0xFF)
            : System.Windows.Media.Color.FromArgb(0x12, 0x00, 0x00, 0x00));
        _tabHover = new SolidColorBrush(isDark
            ? System.Windows.Media.Color.FromArgb(0x14, 0xFF, 0xFF, 0xFF)
            : System.Windows.Media.Color.FromArgb(0x0A, 0x00, 0x00, 0x00));

        TitleBar.BorderBrush = new SolidColorBrush(isDark
            ? System.Windows.Media.Color.FromArgb(0x1F, 0xFF, 0xFF, 0xFF)
            : System.Windows.Media.Color.FromArgb(0x12, 0x00, 0x00, 0x00));
        ModeSwitch.Background = new SolidColorBrush(isDark
            ? System.Windows.Media.Color.FromArgb(0x1A, 0xFF, 0xFF, 0xFF)
            : System.Windows.Media.Color.FromArgb(0x0C, 0x00, 0x00, 0x00));

        SetWebViewColorScheme(isDark);
        UpdateModeUi();
    }

    public void ApplyOpacity(double opacity)
    {
        var isDark = _settings.IsDarkTheme;
        var baseColor = isDark
            ? System.Windows.Media.Color.FromRgb(0x15, 0x15, 0x17)
            : System.Windows.Media.Color.FromRgb(0xFF, 0xFF, 0xFF);
        var alpha = (byte)(opacity * 255);
        var bgColor = System.Windows.Media.Color.FromArgb(alpha, baseColor.R, baseColor.G, baseColor.B);

        MainBorder.Background = new SolidColorBrush(bgColor);
        TitleBar.Background = new SolidColorBrush(bgColor);
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

    private string CurrentUrl => _settings.IsAgentMode ? _settings.HarnessUrl : _settings.ChatUrl;

    private void NavigateToCurrent()
    {
        if (_webView?.CoreWebView2 == null) return;
        if (_settings.IsAgentMode && !IsHarnessListening())
        {
            _webView.CoreWebView2.NavigateToString(BuildHarnessNoticeHtml());
            return;
        }
        _webView.CoreWebView2.Navigate(CurrentUrl);
    }

    private static bool IsHarnessListening()
    {
        try
        {
            using var client = new System.Net.Sockets.TcpClient();
            var task = client.ConnectAsync(System.Net.IPAddress.Loopback, 3081);
            return task.Wait(300) && client.Connected;
        }
        catch
        {
            return false;
        }
    }

    private string BuildHarnessNoticeHtml()
    {
        var isDark = _settings.IsDarkTheme;
        var bg = isDark ? "#151517" : "#FFFFFF";
        var fg = isDark ? "#E8E8EA" : "#1A1A2E";
        var sub = isDark ? "#9A9AA5" : "#666680";
        var accent = isDark ? "#7FD1FF" : "#4D7CFE";
        var html = """
        <!DOCTYPE html>
        <html lang="zh-CN">
        <head>
        <meta charset="utf-8">
        <style>
          body { margin:0; background:__BG__; color:__FG__; font-family:"Segoe UI","Microsoft YaHei",sans-serif; }
          .wrap { max-width:420px; margin:80px auto 0; padding:0 28px; }
          h1 { font-size:18px; margin:0 0 12px; }
          p { font-size:13px; line-height:1.7; color:__SUB__; }
          code { background:__BG__; border:1px solid __SUB__55; border-radius:4px; padding:1px 6px; font-size:12px; }
          .tip { font-size:12px; color:__SUB__; margin-top:18px; border-top:1px solid __SUB__44; padding-top:14px; }
          a { color:__ACCENT__; }
        </style>
        </head>
        <body>
          <div class="wrap">
            <h1>Agent 模式需要本地 DeepSeek Harness</h1>
            <p>目前 __URL__（端口 3081）没有在运行。</p>
            <p>两种方式继续：</p>
            <p>1. 按仓库 README 执行 <code>setup-harness.cmd</code> 后运行 <code>start-web.cmd</code>，本地部署 Harness 后自动可用；</p>
            <p>2. 切回顶栏 <b>Chat</b> 模式，直接用 DeepSeek 网页版，无需任何配置。</p>
            <div class="tip">说明：这是为保护隐私与隔离设计的本地 Agent 环境；直接下载的 exe 默认未附带该组件。</div>
          </div>
        </body>
        </html>
        """;
        return html
            .Replace("__BG__", bg)
            .Replace("__FG__", fg)
            .Replace("__SUB__", sub)
            .Replace("__ACCENT__", accent)
            .Replace("__URL__", _settings.HarnessUrl);
    }

    private void OnChatTabClick(object sender, MouseButtonEventArgs e) => SetAgentMode(false);

    private void OnAgentTabClick(object sender, MouseButtonEventArgs e) => SetAgentMode(true);

    private void SetAgentMode(bool agent)
    {
        if (_settings.IsAgentMode == agent) return;
        _settings.IsAgentMode = agent;
        _settings.Save();
        UpdateModeUi();
        NavigateToCurrent();
    }

    private void UpdateModeUi()
    {
        if (ChatTabText == null || AgentTabText == null) return;
        var isDark = _settings.IsDarkTheme;
        var activeBg = isDark
            ? System.Windows.Media.Color.FromArgb(0x2B, 0xFF, 0xFF, 0xFF)
            : System.Windows.Media.Color.FromRgb(0xFF, 0xFF, 0xFF);
        var activeBorder = isDark
            ? System.Windows.Media.Color.FromArgb(0x33, 0xFF, 0xFF, 0xFF)
            : System.Windows.Media.Color.FromArgb(0x1F, 0x00, 0x00, 0x00);
        var activeFg = isDark ? Colors.White : System.Windows.Media.Color.FromRgb(0x1A, 0x1A, 0x2E);
        var inactiveFg = isDark
            ? System.Windows.Media.Color.FromArgb(0x8A, 0xFF, 0xFF, 0xFF)
            : System.Windows.Media.Color.FromArgb(0x99, 0x00, 0x00, 0x00);

        if (_settings.IsAgentMode)
        {
            AgentTab.Background = new SolidColorBrush(activeBg);
            AgentTab.BorderBrush = new SolidColorBrush(activeBorder);
            AgentTab.BorderThickness = new Thickness(1);
            AgentTabText.Foreground = new SolidColorBrush(activeFg);
            ChatTab.Background = System.Windows.Media.Brushes.Transparent;
            ChatTab.BorderThickness = new Thickness(0);
            ChatTabText.Foreground = new SolidColorBrush(inactiveFg);
        }
        else
        {
            ChatTab.Background = new SolidColorBrush(activeBg);
            ChatTab.BorderBrush = new SolidColorBrush(activeBorder);
            ChatTab.BorderThickness = new Thickness(1);
            ChatTabText.Foreground = new SolidColorBrush(activeFg);
            AgentTab.Background = System.Windows.Media.Brushes.Transparent;
            AgentTab.BorderThickness = new Thickness(0);
            AgentTabText.Foreground = new SolidColorBrush(inactiveFg);
        }
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
        NavigateToCurrent();
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
        if (_unloadTimer == null)
        {
            _unloadTimer = new DispatcherTimer();
            _unloadTimer.Tick += OnUnloadTick;
        }
        _unloadTimer.Interval = TimeSpan.FromSeconds(_settings.UnloadDelaySeconds);
        _unloadTimer.Start();
    }

    private void OnUnloadTick(object? sender, EventArgs e)
    {
        _unloadTimer?.Stop();
        if (Visibility != Visibility.Visible)
            DestroyWebView();
    }

    public async void SlideIn()
    {
        if (IsAnimating) return;
        _hideTimer.Stop();
        _unloadTimer?.Stop();

        _slideInTcs?.TrySetCanceled();
        _slideInTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        IsAnimating = true;

        if (_webView == null)
        {
            Show();
            Activate();
            await CreateWebViewAsync();
            AnimateIn();
        }
        else
        {
            Show();
            Activate();
            AnimateIn();
        }
    }

    public Task WaitForSlideInAsync()
    {
        if (_slideInTcs != null) return _slideInTcs.Task;

        _slideInTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (!IsAnimating && Visibility == Visibility.Visible)
            _slideInTcs.TrySetResult(true);
        return _slideInTcs.Task;
    }

    public async Task PasteClipboardImageAsync()
    {
        if (_webView?.CoreWebView2 == null) return;

        const string script =
            "(()=>{const i=document.querySelector('textarea,input');if(i){i.focus();}" +
            "const v=document.querySelector('[data-model-type=\"vision\"][role=\"radio\"]');" +
            "if(v&&v.getAttribute('aria-checked')!=='true'){v.click();}" +
            "return i?true:false;})()";

        for (var i = 0; i < 40; i++)
        {
            try
            {
                var result = await _webView.CoreWebView2.ExecuteScriptAsync(script);
                if (result == "true")
                {
                    await Task.Delay(250);
                    break;
                }
            }
            catch { }
            await Task.Delay(150);
        }

        try
        {
            await _webView.CoreWebView2.ExecuteScriptAsync(
                "(()=>{const v=document.querySelector('[data-model-type=\"vision\"][role=\"radio\"]');" +
                "if(v&&v.getAttribute('aria-checked')!=='true'){v.click();}return true;})()");
        }
        catch { }

        await Task.Delay(200);

        try
        {
            KeyAction.PressKey(new[] { KeyCode.LeftControl, KeyCode.V }, 60);
        }
        catch { }
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
        anim.Completed += async (_, _) =>
        {
            IsAnimating = false;
            _slideInTcs?.TrySetResult(true);
            if (_webView != null)
            {
                Keyboard.Focus(_webView);
                try
                {
                    await _webView.CoreWebView2!.ExecuteScriptAsync(
                        "try{let e=document.querySelector('textarea,input');e?.focus()}catch{}");
                }
                catch { }
            }
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

    private void OnSettingsClick(object sender, MouseButtonEventArgs e)
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

        _hideTimer.Stop();
        _hideTimer.Start();
    }

    private void OnHideTick(object? sender, EventArgs e)
    {
        _hideTimer.Stop();
        if (IsActive) return;
        if (Visibility != Visibility.Visible) return;

        if (IsCursorOverWindow())
            return;

        if ((GetAsyncKeyState(VK_LBUTTON) & MouseKeyDown) != 0)
        {
            _hideTimer.Start();
            return;
        }

        SlideOut();
    }

    private bool IsCursorOverWindow()
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        if (!GetCursorPos(out var pt)) return false;
        if (!GetWindowRect(hwnd, out var rect)) return false;
        return pt.X >= rect.Left && pt.X <= rect.Right &&
               pt.Y >= rect.Top && pt.Y <= rect.Bottom;
    }

    private void OnPreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Escape)
        {
            SlideOut();
        }
    }

    private void OnCloseClick(object sender, MouseButtonEventArgs e)
    {
        SlideOut();
    }
}
