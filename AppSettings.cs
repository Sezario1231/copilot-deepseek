using System.IO;
using Microsoft.Win32;

namespace deepseek_copilot;

public sealed class AppSettings
{
    private const string AutoStartKey = "DeepSeekCopilot";

    public double SidebarWidth { get; set; } = 450;
    public bool AutoStart { get; set; }
    public int UnloadDelaySeconds { get; set; } = 5;
    public string ChatUrl { get; set; } = "https://chat.deepseek.com/";

    private static string FilePath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DeepSeekCopilot", "settings.json");

    public void Save()
    {
        SyncAutoStart();
        var dir = Path.GetDirectoryName(FilePath);
        if (dir != null) Directory.CreateDirectory(dir);
        File.WriteAllText(FilePath, DanKeJson.JSON.ToJson(this));
    }

    private void SyncAutoStart()
    {
        using var key = Registry.CurrentUser.OpenSubKey(
            @"Software\Microsoft\Windows\CurrentVersion\Run", true);
        if (AutoStart)
            key?.SetValue(AutoStartKey, Environment.ProcessPath ?? "");
        else
            key?.DeleteValue(AutoStartKey, false);
    }

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var s = DanKeJson.JSON.ToData<AppSettings>(File.ReadAllText(FilePath));
                if (s != null)
                {
                    s.ChatUrl = string.IsNullOrEmpty(s.ChatUrl) ? "https://chat.deepseek.com/" : s.ChatUrl;
                    if (s.SidebarWidth <= 0) s.SidebarWidth = 450;
                    if (s.UnloadDelaySeconds <= 0) s.UnloadDelaySeconds = 5;
                    return s;
                }
            }
        }
        catch { }
        return new AppSettings();
    }
}
