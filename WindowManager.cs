namespace deepseek_copilot;

public sealed class WindowManager
{
    private SidebarWindow? _sidebar;
    private readonly AppSettings _settings;

    public WindowManager()
    {
        _settings = AppSettings.Load();
    }

    public void Toggle()
    {
        if (_sidebar is { IsAnimating: true }) return;

        if (_sidebar == null || !_sidebar.IsVisible)
        {
            ShowSidebar();
        }
        else
        {
            HideSidebar();
        }
    }

    private void ShowSidebar()
    {
        if (_sidebar == null)
        {
            _sidebar = new SidebarWindow(_settings);
            _sidebar.ApplyWidth(_settings.SidebarWidth);
            _sidebar.ApplyTheme(_settings.GetThemeMode());
            _sidebar.Opacity = _settings.SidebarOpacity;
            _sidebar.SettingsClicked += OnSettingsClicked;
        }

        _sidebar.SlideIn();
    }

    private void HideSidebar()
    {
        _sidebar?.SlideOut();
    }

    private void OnSettingsClicked()
    {
        var win = new SettingsWindow(_settings,
            onWidthChanged: w => _sidebar?.ApplyWidth(w),
            onThemeChanged: t => _sidebar?.ApplyTheme(t),
            onOpacityChanged: o => _sidebar!.Opacity = o);
        win.ShowDialog();
    }

    public void OpenSettings()
    {
        OnSettingsClicked();
    }

    public void Cleanup()
    {
        _sidebar?.Close();
        _sidebar = null;
    }
}
