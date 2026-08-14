using System.Windows.Threading;
using KBEHtool;

namespace deepseek_copilot;

public sealed class KeyboardHookService : IDisposable
{
    private const int LongPressMs = 500;

    private readonly AppSettings _settings;
    private readonly DispatcherTimer _holdTimer;
    private bool _copilotMapped;
    private bool _copilotDown;
    private bool _copilotLongPressed;

    public event Action? CopilotKeyPressed;
    public event Action? CopilotLongPressed;
    public event Action? MappingToggled;

    public KeyboardHookService(AppSettings settings)
    {
        _settings = settings;
        KBEH.Start();
        KeyAction.AddKeyDownPreview(OnKeyDown);
        KeyAction.AddKeyUpPreview(OnKeyUp);

        _holdTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(LongPressMs) };
        _holdTimer.Tick += OnHoldTick;
    }

    private bool OnKeyDown(KeyCode kc)
    {
        if (kc != KeyCode.Copilot) return false;

        KeyAction.ReleaseKey(KeyCode.LeftWin);
        _copilotMapped = false;

        if (_settings.EnableToggleShortcut && KeyAction.IsKeyPressed(KeyCode.LeftControl))
        {
            _settings.MapCopilotToRightCtrl = !_settings.MapCopilotToRightCtrl;
            _settings.Save();
            MappingToggled?.Invoke();
            return true;
        }

        if (_settings.MapCopilotToRightCtrl)
        {
            KeyAction.PressKey(KeyCode.RightControl, -1);
            _copilotMapped = true;
            return true;
        }

        if (!_copilotDown)
        {
            _copilotDown = true;
            _copilotLongPressed = false;
            _holdTimer.Start();
        }
        return true;
    }

    private void OnHoldTick(object? sender, EventArgs e)
    {
        _holdTimer.Stop();
        if (!_copilotDown) return;

        _copilotLongPressed = true;
        CopilotLongPressed?.Invoke();
    }

    private bool OnKeyUp(KeyCode kc)
    {
        if (kc != KeyCode.Copilot) return false;

        _holdTimer.Stop();
        _copilotDown = false;

        if (_copilotMapped)
        {
            KeyAction.ReleaseKey(KeyCode.RightControl);
        }
        else if (!_copilotLongPressed)
        {
            CopilotKeyPressed?.Invoke();
        }
        return true;
    }

    public void Dispose()
    {
        _holdTimer.Stop();
        KBEH.Stop();
    }
}