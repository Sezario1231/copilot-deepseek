using System.Windows;

namespace deepseek_copilot;

using Forms = System.Windows.Forms;

public partial class App : System.Windows.Application
{
    private static readonly System.Threading.Mutex _mutex = new(true, "DeepSeekCopilot-Singleton");
    private WindowManager? _windowManager;
    private KeyboardHookService? _keyboardService;
    private Forms.NotifyIcon? _trayIcon;
    private Forms.ToolStripMenuItem? _mapMenuItem;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        if (!_mutex.WaitOne(0, false))
        {
            System.Windows.MessageBox.Show("DeepSeek Copilot 已在运行中", "DeepSeek Copilot",
                MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }

        if (e.Args.Length > 0 && e.Args[0] == "--detect")
        {
            new MainWindow().Show();
            return;
        }

        _windowManager = new WindowManager();
        _keyboardService = new KeyboardHookService(_windowManager.Settings);
        _keyboardService.CopilotKeyPressed += OnCopilotKeyPressed;

        SetupTrayIcon();

        _keyboardService.MappingToggled += () =>
        {
            if (_mapMenuItem != null)
                _mapMenuItem.Checked = _windowManager!.Settings.MapCopilotToRightCtrl;
        };
    }

    private void OnCopilotKeyPressed()
    {
        _windowManager?.Toggle();
    }

    private void SetupTrayIcon()
    {
        _trayIcon = new Forms.NotifyIcon
        {
            Icon = CreateTrayIcon(),
            Text = "DeepSeek Copilot\n点击切换侧边栏",
            Visible = true
        };
        _trayIcon.Click += (_, _) => _windowManager?.Toggle();

        var menu = new Forms.ContextMenuStrip();

        _mapMenuItem = new Forms.ToolStripMenuItem("Copilot → 右 Ctrl")
        {
            Checked = _windowManager!.Settings.MapCopilotToRightCtrl
        };
        _mapMenuItem.Click += (_, _) =>
        {
            var s = _windowManager.Settings;
            s.MapCopilotToRightCtrl = !s.MapCopilotToRightCtrl;
            s.Save();
            _mapMenuItem.Checked = s.MapCopilotToRightCtrl;
        };

        menu.Items.Add("显示/隐藏", null, (_, _) => _windowManager?.Toggle());
        menu.Items.Add("设置", null, (_, _) => _windowManager?.OpenSettings());
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add(_mapMenuItem);
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("退出", null, (_, _) => ExitApp());
        _trayIcon.ContextMenuStrip = menu;
    }

    private static System.Drawing.Icon CreateTrayIcon()
    {
        try
        {
            return IconHelper.RenderToIcon(32);
        }
        catch
        {
            return FallbackIcon();
        }
    }

    private static System.Drawing.Icon FallbackIcon()
    {
        using var bmp = new System.Drawing.Bitmap(16, 16);
        using var g = System.Drawing.Graphics.FromImage(bmp);
        g.Clear(System.Drawing.Color.Transparent);
        using var bg = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(0x4F, 0x6B, 0xED));
        g.FillRectangle(bg, 0, 0, 16, 16);
        using var font = new System.Drawing.Font("Segoe UI", 9, System.Drawing.FontStyle.Bold);
        g.DrawString("D", font, System.Drawing.Brushes.White, 2, 1);
        return System.Drawing.Icon.FromHandle(bmp.GetHicon());
    }

    private void ExitApp()
    {
        _windowManager?.Cleanup();
        _keyboardService?.Dispose();
        _trayIcon?.Dispose();
        _mutex.ReleaseMutex();
        _mutex.Close();
        Shutdown();
    }
}
