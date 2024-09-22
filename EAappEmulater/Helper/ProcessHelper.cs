namespace EAappEmulater.Helper;

public static class ProcessHelper
{
    /// <summary>
    /// Determine whether the process is running
    /// </summary>
    public static bool IsAppRun(string appName)
    {
        if (string.IsNullOrWhiteSpace(appName))
            return false;

        if (appName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            appName = appName[..^4];

        return Process.GetProcessesByName(appName).Length > 0;
    }

    /// <summary>
    /// Open http link
    /// </summary>
    public static void OpenLink(string url)
    {
        if (!url.StartsWith("http"))
        {
            LoggerHelper.Warn($"The link is not in http format {url}");
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            LoggerHelper.Error($"Exception when opening http link {url}", ex);
        }
    }

    /// <summary>
    /// Open the folder path
    /// </summary>
    public static void OpenDirectory(string dirPath)
    {
        if (!Directory.Exists(dirPath))
        {
            LoggerHelper.Warn($"Folder path does not exist {dirPath}");
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(dirPath) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            LoggerHelper.Error($"Exception when opening folder {dirPath}", ex);
        }
    }

    /// <summary>
    /// Open the specified process (supports silent)
    /// </summary>
    public static void OpenProcess(string appPath, bool isSilent = false)
    {
        if (!File.Exists(appPath))
        {
            LoggerHelper.Warn($"Program path does not exist {appPath}");
            return;
        }

        var fileInfo = new FileInfo(appPath);

        try
        {
            // true if the shell should be used when starting the process; false if the process is created directly from the executable file.
            // Default is true for .NET Framework apps and false for .NET Core apps.
            var processInfo = new ProcessStartInfo
            {
                UseShellExecute = true,
                FileName = fileInfo.FullName,
                WorkingDirectory = fileInfo.DirectoryName
            };

            if (isSilent)
            {
                processInfo.WindowStyle = ProcessWindowStyle.Hidden;
                processInfo.CreateNoWindow = true;
            }

            Process.Start(processInfo);
            LoggerHelper.Info($"Start program successfully {appPath}");
        }
        catch (Exception ex)
        {
            LoggerHelper.Error($"Startup program exception {appPath}", ex);
        }
    }

    /// <summary>
    /// Close the specified process based on its name
    /// </summary>
    public static void CloseProcess(string appName)
    {
        if (string.IsNullOrWhiteSpace(appName))
            return;

        if (appName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            appName = appName[..^4];

        try
        {
            var isFind = false;

            foreach (var process in Process.GetProcessesByName(appName))
            {
                //Close the process tree
                process.Kill(true);
                LoggerHelper.Info($"Close process successfully {appName}.exe");

                isFind = true;
            }

            if (!isFind)
                LoggerHelper.Warn($"Process information not found {appName}.exe");
        }
        catch (Exception ex)
        {
            LoggerHelper.Error($"Close process exception {appName}", ex);
        }
    }
}
