using EAappEmulater.Core;

namespace EAappEmulater.Helper;

public static class RegistryHelper
{
    /// <summary>
    /// Read the registry
    /// </summary>
    public static string GetRegistryTargetVaule(string regPath, string keyName)
    {
        try
        {
            var localMachine = Registry.LocalMachine;

            using var regKey = localMachine.OpenSubKey(regPath);
            if (regKey is null)
                return string.Empty;

            return regKey.GetValue(keyName).ToString();
        }
        catch (Exception ex)
        {
            LoggerHelper.Error($"Exception reading registry {regPath} {keyName}", ex);
            return string.Empty;
        }
    }

    /// <summary>
    /// Write to the registry
    /// </summary>
    public static void SetRegistryTargetVaule(string regPath, string keyName, string value)
    {
        try
        {
            var localMachine = Registry.LocalMachine;

            //Create the registry, if it already exists it will not be affected
            using var regKey = localMachine.CreateSubKey(regPath, true);
            if (regKey is null)
                return;

            regKey.SetValue(keyName, value);
        }
        catch (Exception ex)
        {
            LoggerHelper.Error($"Exception writing to registry {regPath} {keyName} {value}", ex);
        }
    }

    /// <summary>
    /// Read the registry EA game installation directory
    /// </summary>
    public static string GetRegistryInstallDir(string regPath)
    {
        var dirPath = GetRegistryTargetVaule(regPath, "Install Dir");
        if (string.IsNullOrWhiteSpace(dirPath))
            return string.Empty;

        return Directory.Exists(dirPath) ? dirPath : string.Empty;
    }

    /// <summary>
    /// Read the registry EA game language information
    /// </summary>
    /// <param name="regPath"></param>
    /// <returns></returns>
    public static string GetRegistryLocale(string regPath)
    {
        var dirPath = GetRegistryTargetVaule(regPath, "Locale");
        if (string.IsNullOrWhiteSpace(dirPath))
            return string.Empty;

        return dirPath;
    }

    /// <summary>
    /// Get the current game installation language
    /// </summary>
    public static string GetLocaleByContentId(string contentId)
    {
        if (Base.GameRegistryDb.TryGetValue(contentId, out List<string> regs))
        {
            foreach (var reg in regs)
            {
                var locale = GetRegistryTargetVaule(reg, "Locale");
                if (!string.IsNullOrWhiteSpace(locale))
                    return locale;
            }
        }

        return "en_US";
    }

    /// <summary>
    /// Get the Origin/EA App registry status and write it directly every time it is started
    /// </summary>
    public static void CheckAndAddEaAppRegistryKey()
    {
                /*
                 * This can solve the problem that games such as F1 23 cannot be started if they do not install EA Desktop/Origin and there is no program in the registry ClientPath path.
                 * It can also solve the problem of EA App popping up when starting games such as TTF2
                 */

        try
        {
            using var localMachine = Registry.LocalMachine;
            using var newSubKey = localMachine.CreateSubKey(@"SOFTWARE\Wow6432Node\Origin", true);

            if (newSubKey is not null)
            {
                var clientPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "cmdkey.exe");
                newSubKey.SetValue("ClientPath", clientPath, RegistryValueKind.String);
            }
        }
        catch (Exception ex)
        {
            LoggerHelper.Error("Exception when writing EADesktop installation path to registry", ex);
        }
    }
}
