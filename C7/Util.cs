using System;

public class Util
{
    static public string GetCiv3Path()
    {
        // Use CIV3_HOME env var if present
        string path = Environment.GetEnvironmentVariable("CIV3_HOME");
        if (path != null) return path;

        // Look up in Windows registry if present
        path = Civ3PathFromRegistry("");
        if (path != "") return path;

        // TODO: Maybe check an array of hard-coded paths during dev time?
        return "/civ3/path/not/found";
    }

    static public string Civ3PathFromRegistry(string defaultPath = "D:/Civilization III")
    {
        // Assuming 64-bit platform, get vanilla Civ3 install folder from registry
        return (string)Microsoft.Win32.Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Infogrames Interactive\Civilization III", "install_path", defaultPath);
    }
}