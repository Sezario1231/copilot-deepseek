using System.Text;
using System.Windows;
using KBEHtool;
using KBEHtool.Base;

namespace deepseek_copilot;

public partial class MainWindow : Window
{
    private readonly HashSet<string> _pressedKeys = new();

    public MainWindow()
    {
        KBEH.Start();
        InitializeComponent();
        KeyAction.AddKeyDownListener(OnKeyDown);
        KeyAction.AddKeyUpListener(OnKeyUp);
        KeyAction.RawKeyDown += OnRawKeyDown;
    }

    private void OnRawKeyDown(RawKeyEvent raw)
    {
        Dispatcher.Invoke(() =>
        {
            RawDisplay.Text =
                $"VK=0x{raw.VkCode:X2} SCAN=0x{raw.ScanCode:X2} " +
                $"EXT={raw.IsExtendedKey} ALT={raw.IsAltDown} FLAGS=0x{raw.Flags:X2}";
            LogBox.AppendText(
                $"[{DateTime.Now:HH:mm:ss}] RAW VK=0x{raw.VkCode:X2} SCAN=0x{raw.ScanCode:X2} " +
                $"EXT={raw.IsExtendedKey} ALT={raw.IsAltDown} FLAGS=0x{raw.Flags:X2}{Environment.NewLine}");
            LogBox.ScrollToEnd();
        });
    }

    private void OnKeyDown(KeyCode key)
    {
        var name = key.ToString();
        Dispatcher.Invoke(() =>
        {
            _pressedKeys.Add(name);
            UpdateDisplay();
        });
    }

    private void OnKeyUp(KeyCode key)
    {
        var name = key.ToString();
        Dispatcher.Invoke(() =>
        {
            _pressedKeys.Remove(name);
            UpdateDisplay();
        });
    }

    private void UpdateDisplay()
    {
        var sb = new StringBuilder();
        foreach (var k in _pressedKeys)
        {
            sb.Append(k);
            sb.Append(' ');
        }
        var text = sb.ToString().Trim();
        KeyDisplay.Text = string.IsNullOrEmpty(text) ? "等待按键..." : text;

        if (!string.IsNullOrEmpty(text))
        {
            LogBox.AppendText($"[{DateTime.Now:HH:mm:ss}] KBEHtool KeyCode: {text}{Environment.NewLine}");
            LogBox.ScrollToEnd();
        }
    }

    private void Window_Closed(object sender, EventArgs e)
    {
        KBEH.Stop();
    }
}
