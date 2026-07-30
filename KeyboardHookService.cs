using KBEHtool;

namespace deepseek_copilot;

public sealed class KeyboardHookService : IDisposable
{
    private readonly AppSettings _settings;
    public event Action? CopilotKeyPressed;

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

        if (_settings.MapCopilotToRightCtrl)
        {
            KeyAction.PressKey(KeyCode.RightControl, -1);
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

        if (_settings.MapCopilotToRightCtrl)
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
