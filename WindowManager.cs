namespace deepseek_copilot;

public sealed class WindowManager
{
    public AppSettings Settings { get; }
    private SidebarWindow? _sidebar;

    public WindowManager()
    {
        Settings = AppSettings.Load();
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
            _sidebar = new SidebarWindow(Settings);
            _sidebar.ApplyWidth(Settings.SidebarWidth);
            _sidebar.ApplyTheme(Settings.GetThemeMode());
            _sidebar.ApplyOpacity(Settings.SidebarOpacity);
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
        var win = new SettingsWindow(Settings,
            onWidthChanged: w => _sidebar?.ApplyWidth(w),
            onThemeChanged: t => _sidebar?.ApplyTheme(t),
            onOpacityChanged: o => _sidebar!.ApplyOpacity(o));
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
