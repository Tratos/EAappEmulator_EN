using EAappEmulater.Utils;
using EAappEmulater.Helper;

namespace EAappEmulater;

/// <summary>
/// App.xaml 的交互逻辑
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// 主程序互斥体
    /// </summary>
    public static Mutex AppMainMutex;
    /// <summary>
    /// 应用程序名称
    /// </summary>
    private readonly string AppName = ResourceAssembly.GetName().Name;

    /// <summary>
    /// 保证程序只能同时启动一个
    /// </summary>
    protected override void OnStartup(StartupEventArgs e)
    {
        LoggerHelper.Info($"welcome {AppName} program");

        // 注册异常捕获
        RegisterEvents();
        // 注册编码
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        //////////////////////////////////////////////////////

        AppMainMutex = new Mutex(true, AppName, out var createdNew);
        if (!createdNew)
        {
            LoggerHelper.Warn("Please do not open it again, the program is already running");
            MsgBoxHelper.Warning($"Please do not open it again, the program is already running\nIf you keep getting prompted, please go to\"Task Manager - Details\n\n（win7 is a process）\"inside\nForced end \"{AppName}.exe\" program");
            Current.Shutdown();
            return;
        }

        //////////////////////////////////////////////////////

        LoggerHelper.Info("WebView2 environment detection is in progress...");
        if (!CoreUtil.CheckWebView2Env())
        {
            if (MessageBox.Show("Not found WebView2 Operating environment, please go to Microsoft official website to download and install\nhttps://go.microsoft.com/fwlink/p/?LinkId=2124703",
                "WebView2 environment detection", MessageBoxButton.OKCancel, MessageBoxImage.Warning) == MessageBoxResult.OK)
            {
                ProcessHelper.OpenLink("https://go.microsoft.com/fwlink/p/?LinkId=2124703");
                Current.Shutdown();
                return;
            }
        }
        LoggerHelper.Info($"Current system WebVieww2 The environment is normal");

        LoggerHelper.Info("TCP port availability check in progress...");
        var ipProperties = IPGlobalProperties.GetIPGlobalProperties();
        var ipEndPoints = ipProperties.GetActiveTcpListeners();
        foreach (var endPoint in ipEndPoints)
        {
            if (endPoint.Port == 3216)
            {
                LoggerHelper.Error("It is detected that TCP port 3216 is occupied, please unoccupy the port.");
                MsgBoxHelper.Error("It is detected that TCP port 3216 is occupied, please unoccupy the port.", "Initialization error");
                Current.Shutdown();
                return;
            }

            if (endPoint.Port == 3215)
            {
                LoggerHelper.Error("It is detected that TCP port 3215 is occupied, please unoccupy the port.");
                MsgBoxHelper.Error("It is detected that TCP port 3215 is occupied, please unoccupy the port.", "Initialization error");
                Current.Shutdown();
                return;
            }

            if (endPoint.Port == 4219)
            {
                LoggerHelper.Error("It is detected that TCP port 4219 is occupied, please unoccupy the port.");
                MsgBoxHelper.Error("It is detected that TCP port 4219 is occupied, please unoccupy the port.", "Initialization error");
                Current.Shutdown();
                return;
            }
        }
        LoggerHelper.Info("Current system TCP port detection is normal");

        //////////////////////////////////////////////////////

        base.OnStartup(e);
    }

    /// <summary>
    /// 注册异常捕获事件
    /// </summary>
    private void RegisterEvents()
    {
        DispatcherUnhandledException += App_DispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
    }

    /// <summary>
    /// UI线程未捕获异常处理事件（UI主线程）
    /// </summary>
    private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        var msg = GetExceptionInfo(e.Exception, e.ToString());
        SaveCrashLog(msg);
    }

    /// <summary>
    /// 非UI线程未捕获异常处理事件（例如自己创建的一个子线程）
    /// </summary>
    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var msg = GetExceptionInfo(e.ExceptionObject as Exception, e.ToString());
        SaveCrashLog(msg);
    }

    /// <summary>
    /// Task线程内未捕获异常处理事件
    /// </summary>
    private void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
    {
        var msg = GetExceptionInfo(e.Exception, e.ToString());
        SaveCrashLog(msg);
    }

    /// <summary>
    /// 生成自定义异常消息
    /// </summary>
    private string GetExceptionInfo(Exception ex, string backStr)
    {
        var builder = new StringBuilder();

        builder.AppendLine($"Program Version: {CoreUtil.VersionInfo}");
        builder.AppendLine($"user name: {CoreUtil.UserName}");
        builder.AppendLine($"computer name: {CoreUtil.MachineName}");
        builder.AppendLine($"system version: {CoreUtil.OSVersion}");
        builder.AppendLine($"System directory: {CoreUtil.SystemDirectory}");
        builder.AppendLine($"Runtime platform: {CoreUtil.RuntimeVersion}");
        builder.AppendLine($"Runtime version: {CoreUtil.OSArchitecture}");
        builder.AppendLine($"Runtime environment: {CoreUtil.RuntimeIdentifier}");
        builder.AppendLine("------------------------------");
        builder.AppendLine($"crash time: {DateTime.Now}");

        if (ex is not null)
        {
            builder.AppendLine($"Exception type: {ex.GetType().Name}");
            builder.AppendLine($"Exception information: {ex.Message}");
            builder.AppendLine($"stack call: \n{ex.StackTrace}");
        }
        else
        {
            builder.AppendLine($"Unhandled exception: {backStr}");
        }

        return builder.ToString();
    }

    /// <summary>
    /// 保存崩溃日志
    /// </summary>
    private void SaveCrashLog(string log)
    {
        try
        {
            var path = Path.Combine(CoreUtil.Dir_Log_Crash, $"CrashReport-{DateTime.Now:yyyyMMdd_HHmmss_ffff}.log");
            File.WriteAllText(path, log);
        }
        catch { }
    }
}