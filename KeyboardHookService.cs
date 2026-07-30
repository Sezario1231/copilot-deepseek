using KBEHtool;

namespace deepseek_copilot;

public sealed class KeyboardHookService : IDisposable
{
    public event Action? CopilotKeyPressed;

    public KeyboardHookService()
    {
        KBEH.Start();
        KeyAction.AddKeyDownPreview(kc =>
        {
            if (kc == KeyCode.Copilot)
            {
                KeyAction.ReleaseKey(KeyCode.LeftWin);
                CopilotKeyPressed?.Invoke();
                return true;
            }
            return false;
        });
    }

    public void Dispose()
    {
        KBEH.Stop();
    }
}
