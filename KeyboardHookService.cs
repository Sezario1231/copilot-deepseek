using KBEHtool;

namespace deepseek_copilot;

public sealed class KeyboardHookService : IDisposable
{
    private readonly AppSettings _settings;
    private bool _copilotMapped;
    public event Action? CopilotKeyPressed;
    public event Action? MappingToggled;

    public KeyboardHookService(AppSettings settings)
    {
        _settings = settings;
        KBEH.Start();
        KeyAction.AddKeyDownPreview(OnKeyDown);
        KeyAction.AddKeyUpPreview(OnKeyUp);
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
        }
        else
        {
            CopilotKeyPressed?.Invoke();
        }
        return true;
    }

    private bool OnKeyUp(KeyCode kc)
    {
        if (kc != KeyCode.Copilot) return false;

        if (_copilotMapped)
        {
            KeyAction.ReleaseKey(KeyCode.RightControl);
        }
        return true;
    }

    public void Dispose()
    {
        KBEH.Stop();
    }
}
