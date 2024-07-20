using EAappEmulater.Core;
using EAappEmulater.Enums;
using EAappEmulater.Utils;
using EAappEmulater.Helper;

namespace EAappEmulater;

public static class Globals
{
    /// <summary>
    /// 全局配置文件路径
    /// </summary>
    private static readonly string _configPath;

    /// <summary>
    /// 当前使用的账号槽
    /// </summary>
    public static AccountSlot AccountSlot { get; set; } = AccountSlot.S0;

    public static bool IsGetFriendsSuccess { get; set; } = false;
    public static string FriendsXmlString { get; set; } = string.Empty;

    static Globals()
    {
        _configPath = Path.Combine(CoreUtil.Dir_Config, "Config.ini");
    }

    /// <summary>
    /// 读取全局配置文件
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
            LoggerHelper.Warn($"Enumeration conversion failed AccountSlot {slot}");
        }

        LoggerHelper.Info("Read global configuration file successfully");
    }

    /// <summary>
    /// 写入全局配置文件
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
    /// 获取当前账号槽全局配置文件路径
    /// </summary>
    public static string GetAccountIniPath()
    {
        return Account.AccountPathDb[AccountSlot];
    }

    /// <summary>
    /// 获取当前账号槽WebView2缓存路径
    /// </summary>
    public static string GetAccountCacheDir()
    {
        return CoreUtil.AccountCacheDb[AccountSlot];
    }
}
