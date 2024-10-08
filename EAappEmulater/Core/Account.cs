﻿using EAappEmulater.Enums;
using EAappEmulater.Helper;
using EAappEmulater.Utils;

namespace EAappEmulater.Core;

public static class Account
{
    private static string _iniPath;

    public static Dictionary<AccountSlot, string> AccountPathDb { get; private set; } = new();

    /////////////////////////////////////////////////

    public static string PlayerName { get; set; }
    public static string PersonaId { get; set; }
    public static string UserId { get; set; }
    public static string AvatarId { get; set; }
    public static string Avatar { get; set; }

    /////////////////////////////////////////////////

    public static string Remid { get; set; }
    public static string Sid { get; set; }
    public static string AccessToken { get; set; }
    public static string OriginPCAuth { get; set; }
    public static string OriginPCToken { get; set; }
    public static string LSXAuthCode { get; set; }

    /////////////////////////////////////////////////

    static Account()
    {
        // Create account slot configuration files in batches
        foreach (int value in Enum.GetValues(typeof(AccountSlot)))
        {
            var path = Path.Combine(CoreUtil.Dir_Account, $"Account{value}.ini");
            FileHelper.CreateFile(path);

            AccountPathDb[(AccountSlot)value] = path;
        }

        GetIniPath();
    }

    /// <summary>
    /// Get the current configuration file path
    /// </summary>
    public static void GetIniPath()
    {
        _iniPath = AccountPathDb[Globals.AccountSlot];
    }

    /// <summary>
    /// Reset configuration file
    /// </summary>
    public static void Reset()
    {
        GetIniPath();
        LoggerHelper.Info($"Current reset configuration file path {_iniPath}");

        PlayerName = string.Empty;
        PersonaId = string.Empty;
        UserId = string.Empty;
        AvatarId = string.Empty;
        Avatar = string.Empty;

        Remid = string.Empty;
        Sid = string.Empty;
        AccessToken = string.Empty;
        OriginPCAuth = string.Empty;
        OriginPCToken = string.Empty;
        LSXAuthCode = string.Empty;
    }

    /// <summary>
    /// Read configuration file
    /// </summary>
    public static void Read()
    {
        GetIniPath();
        LoggerHelper.Info($"Current read configuration file path {_iniPath}");

        PlayerName = ReadString("Account", "PlayerName");
        PersonaId = ReadString("Account", "PersonaId");
        UserId = ReadString("Account", "UserId");
        AvatarId = ReadString("Account", "AvatarId");

        var avatar = ReadString("Account", "Avatar");
        // The player avatar exists, and the player avatar name and avatar ID are consistent
        if (File.Exists(avatar) && Path.GetFileNameWithoutExtension(avatar) == AvatarId)
            Avatar = avatar;
        else
            Avatar = string.Empty;

        Remid = ReadString("Cookie", "Remid");
        Sid = ReadString("Cookie", "Sid");
        AccessToken = ReadString("Cookie", "AccessToken");
        OriginPCAuth = ReadString("Cookie", "OriginPCAuth");
        OriginPCToken = ReadString("Cookie", "OriginPCToken");
        LSXAuthCode = ReadString("Cookie", "LSXAuthCode");

        foreach (var item in Base.GameInfoDb)
        {
            var key = item.Key.ToString();

            item.Value.IsUseCustom = ReadBoolean(key, "IsUseCustom");

            //item.Value.Dir = ReadString(key, "Dir");
            item.Value.Args = ReadString(key, "Args");
            item.Value.Dir2 = ReadString(key, "Dir2");
            item.Value.Args2 = ReadString(key, "Args2");
        }
    }

    /// <summary>
    /// Save configuration file
    /// </summary>
    public static void Write()
    {
        GetIniPath();
        LoggerHelper.Info($"Current saved configuration file path {_iniPath}");

        WriteString("Account", "PlayerName", PlayerName);
        WriteString("Account", "PersonaId", PersonaId);
        WriteString("Account", "UserId", UserId);
        WriteString("Account", "AvatarId", AvatarId);
        WriteString("Account", "Avatar", Avatar);

        WriteString("Cookie", "Remid", Remid);
        WriteString("Cookie", "Sid", Sid);
        WriteString("Cookie", "AccessToken", AccessToken);
        WriteString("Cookie", "OriginPCAuth", OriginPCAuth);
        WriteString("Cookie", "OriginPCToken", OriginPCToken);
        WriteString("Cookie", "LSXAuthCode", LSXAuthCode);

        foreach (var item in Base.GameInfoDb)
        {
            var key = item.Key.ToString();

            WriteBoolean(key, "IsUseCustom", item.Value.IsUseCustom);

            //WriteString(key, "Dir", item.Value.Dir);
            WriteString(key, "Args", item.Value.Args);
            WriteString(key, "Dir2", item.Value.Dir2);
            WriteString(key, "Args2", item.Value.Args2);
        }
    }

    private static string ReadString(string section, string key)
    {
        return IniHelper.ReadString(section, key, _iniPath);
    }

    private static bool ReadBoolean(string section, string key)
    {
        return IniHelper.ReadBoolean(section, key, _iniPath);
    }

    private static void WriteString(string section, string key, string value)
    {
        IniHelper.WriteString(section, key, value, _iniPath);
    }

    private static void WriteBoolean(string section, string key, bool value)
    {
        IniHelper.WriteBoolean(section, key, value, _iniPath);
    }
}
