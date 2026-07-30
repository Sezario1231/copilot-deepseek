using System.Windows;

namespace deepseek_copilot;

public partial class SettingsWindow : Window
{
    private readonly AppSettings _settings;
    private readonly Action<double> _onWidthChanged;
    private readonly bool _originalAutoStart;
    private readonly string _originalChatUrl;

    public SettingsWindow(AppSettings settings, Action<double> onWidthChanged)
    {
        _settings = settings;
        _originalAutoStart = settings.AutoStart;
        _originalChatUrl = settings.ChatUrl;
        _onWidthChanged = onWidthChanged;
        InitializeComponent();
        DataContext = settings;
        WidthSlider.Value = settings.SidebarWidth;
        DelaySlider.Value = settings.UnloadDelaySeconds;
    }

    private void OnWidthChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        _onWidthChanged(e.NewValue);
    }

    private void OnSaveClick(object sender, RoutedEventArgs e)
    {
        _settings.SidebarWidth = WidthSlider.Value;
        _settings.AutoStart = AutoStartCheck.IsChecked ?? false;
        _settings.UnloadDelaySeconds = (int)DelaySlider.Value;
        _settings.Save();
        Close();
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        _settings.AutoStart = _originalAutoStart;
        _settings.ChatUrl = _originalChatUrl;
        _onWidthChanged(_settings.SidebarWidth);
        Close();
    }
}
