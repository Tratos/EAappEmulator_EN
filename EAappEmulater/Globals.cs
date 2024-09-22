using EAappEmulater.Core;
using EAappEmulater.Enums;
using EAappEmulater.Helper;
using EAappEmulater.Utils;

namespace EAappEmulater;

public static class Globals
{
    /// <summary>
    /// Global configuration file path
    /// </summary>
    private static readonly string _configPath;

    /// <summary>
    /// The currently used account slot
    /// </summary>
    public static AccountSlot AccountSlot { get; set; } = AccountSlot.S0;

    public static bool IsGetFriendsSuccess { get; set; } = false;
    public static string FriendsXmlString { get; set; } = string.Empty;
    public static string QueryPresenceString { get; set; } = string.Empty;


    static Globals()
    {
        _configPath = Path.Combine(CoreUtil.Dir_Config, "Config.ini");
    }

    /// <summary>
    /// Read the global configuration file
    /// </summary>
    public static void Read()
    {
        LoggerHelper.Info("Start reading global configuration file...");

        var slot = IniHelper.ReadString("Globals", "AccountSlot", _configPath);
        LoggerHelper.Info($"Current read configuration file path {_configPath}");
        LoggerHelper.Info($"Read configuration file successfully Globals AccountSlot {slot}");

        if (Enum.TryParse(slot, out AccountSlot accountSlot))
        {
            AccountSlot = accountSlot;
            LoggerHelper.Info($"Enumeration conversion successful AccountSlot {AccountSlot}");
        }
        else
        {
            LoggerHelper.Warn($"Enum conversion failed AccountSlot {slot}");
        }

        LoggerHelper.Info("Read global configuration file successfully");
    }

    /// <summary>
    /// Write global configuration file
    /// </summary>
    public static void Write()
    {
        LoggerHelper.Info("Start saving global configuration file...");

        IniHelper.WriteString("Globals", "AccountSlot", $"{AccountSlot}", _configPath);
        LoggerHelper.Info($"Current saved configuration file path {_configPath}");
        LoggerHelper.Info($"Configuration file saved successfully Globals AccountSlot {AccountSlot}");

        LoggerHelper.Info("Global configuration file saved successfully");
    }

    /// <summary>
    /// Get the current account slot global configuration file path
    /// </summary>
    public static string GetAccountIniPath()
    {
        return Account.AccountPathDb[AccountSlot];
    }

    /// <summary>
    /// Get the current account slot WebView2 cache path
    /// </summary>
    public static string GetAccountCacheDir()
    {
        return CoreUtil.AccountCacheDb[AccountSlot];
    }
}
