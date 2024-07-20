namespace EAappEmulater.Helper;

public static class ProcessHelper
{
    /// <summary>
    /// 判断进程是否运行
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
    /// 打开http链接
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
    /// 打开文件夹路径
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
    /// 打开指定进程（支持静默）
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
            // 如果应在启动进程时使用 shell，则为 true；如果直接从可执行文件创建进程，则为 false。
            // 默认值为 true .NET Framework 应用和 false .NET Core 应用。
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
    /// 根据名字关闭指定进程
    /// </summary>
    public static async Task CloseProcess(string appName)
    {
        if (string.IsNullOrWhiteSpace(appName))
            return;

        if (appName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            appName = appName[..^4];

        try
        {
            var isSuccess = false;

            foreach (var process in Process.GetProcessesByName(appName))
            {
                process.Kill(true);
                await process.WaitForExitAsync();
                isSuccess = true;
            }

            if (isSuccess)
                LoggerHelper.Info($"Close process successfully {appName}.exe");
            else
                LoggerHelper.Warn($"Process information not found {appName}.exe");
        }
        catch (Exception ex)
        {
            LoggerHelper.Error($"Close process exception {appName}", ex);
        }
    }
}
