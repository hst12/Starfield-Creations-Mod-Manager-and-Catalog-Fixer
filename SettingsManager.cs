using System;
using System.IO;

public static class SettingsManager
{
    private static readonly string SettingsFileName = "settings.default";

    // Use a consistent, user-writable location for settings
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Starfield_Tools", // Replace with your app's name
        SettingsFileName
    );

    public static void SaveSettings(string content)
    {
        var directory = Path.GetDirectoryName(SettingsPath);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        File.WriteAllText(SettingsPath, content);
    }

    public static string LoadSettings()
    {
        if (File.Exists(SettingsPath))
        {
            return File.ReadAllText(SettingsPath);
        }
        return string.Empty;
    }
}
