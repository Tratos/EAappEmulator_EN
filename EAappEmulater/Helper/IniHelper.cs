namespace EAappEmulater.Helper;

public static class IniHelper
{
    [DllImport("kernel32", CharSet = CharSet.Unicode)]
    private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);

    [DllImport("kernel32", CharSet = CharSet.Unicode)]
    private static extern long WritePrivateProfileString(string section, string key, string val, string filePath);

    /// <summary>
    /// Read node value
    /// </summary>
    private static string ReadValue(string section, string key, string iniPath)
    {
        var strBuilder = new StringBuilder(1024);
        _ = GetPrivateProfileString(section, key, string.Empty, strBuilder, strBuilder.Capacity, iniPath);
        return strBuilder.ToString();
    }

    /// <summary>
    ///Write node value
    /// </summary>
    private static void WriteValue(string section, string key, string value, string iniPath)
    {
        WritePrivateProfileString(section, key, value, iniPath);
    }

    #region read operation
    /// <summary>
    /// Read string
    /// </summary>
    public static string ReadString(string section, string key, string iniPath)
    {
        return ReadValue(section, key, iniPath);
    }

    /// <summary>
    /// Read Boolean value
    /// </summary>
    public static bool ReadBoolean(string section, string key, string iniPath)
    {
        var value = ReadValue(section, key, iniPath);
        return value.Equals("true", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Read integer value
    /// </summary>
    public static int ReadInt(string section, string key, string iniPath)
    {
        var value = ReadValue(section, key, iniPath);

        if (int.TryParse(value, out int result))
            return result;

        return default;
    }
    #endregion

    #region write operation
    /// <summary>
    /// Write string
    /// </summary>
    public static void WriteString(string section, string key, string value, string iniPath)
    {
        WriteValue(section, key, value, iniPath);
    }

    /// <summary>
    /// Write Boolean value
    /// </summary>
    public static void WriteBoolean(string section, string key, bool value, string iniPath)
    {
        WriteValue(section, key, value ? "true" : "false", iniPath);
    }

    /// <summary>
    /// Write integer value
    /// </summary>
    public static void WriteInt(string section, string key, int value, string iniPath)
    {
        WriteValue(section, key, $"{value}", iniPath);
    }
    #endregion
}
