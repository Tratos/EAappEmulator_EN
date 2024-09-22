using EAappEmulater.Helper;
using EAappEmulater.Utils;

namespace EAappEmulater;

/// <summary>
/// Interaction logic of App.xaml
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// Main program mutex
    /// </summary>
    public static Mutex AppMainMutex;
    /// <summary>
    /// Application name
    /// </summary>
    private readonly string AppName = ResourceAssembly.GetName().Name;

    /// <summary>
    /// Ensure that only one program can be started at the same time
    /// </summary>
    protected override void OnStartup(StartupEventArgs e)
    {
        LoggerHelper.Info($"Welcome to the {AppName} program");

        //Register exception capture
        RegisterEvents();
        //Registration code
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        //////////////////////////////////////////////////////

        AppMainMutex = new Mutex(true, AppName, out var createdNew);
        if (!createdNew)
        {
            LoggerHelper.Warn("Please do not open it again, the program is already running");
            MsgBoxHelper.Warning($"Please do not open it repeatedly, the program is already running\nIf it keeps prompting, please go to \"Task Manager-Details (win7 is a process)\"\nForce end the \"{AppName}.exe\" program");
            Current.Shutdown();
            return;
        }

        //////////////////////////////////////////////////////

        LoggerHelper.Info("WebVieww2 environment detection is in progress...");
        if (!CoreUtil.CheckWebView2Env())
        {
            if (MessageBox.Show("The WebView2 operating environment was not found. Please go to Microsoft's official website to download and install\nhttps://go.microsoft.com/fwlink/p/?LinkId=2124703",
                "WebView2 environment detection", MessageBoxButton.OKCancel, MessageBoxImage.Warning) == MessageBoxResult.OK)
            {
                ProcessHelper.OpenLink("https://go.microsoft.com/fwlink/p/?LinkId=2124703");
                Current.Shutdown();
                return;
            }
        }
        LoggerHelper.Info($"The current system WebVieww2 environment is normal");

        LoggerHelper.Info("TCP port availability check in progress...");
        var ipProperties = IPGlobalProperties.GetIPGlobalProperties();
        var ipEndPoints = ipProperties.GetActiveTcpListeners();
        foreach (var endPoint in ipEndPoints)
        {
            if (endPoint.Port == 3216)
            {
                LoggerHelper.Error("It is detected that TCP port 3216 is occupied, please unoccupy the port.");
                MsgBoxHelper.Error("It is detected that TCP port 3216 is occupied, please unoccupy the port.\nUnder normal circumstances, you only need to close the Origin and EaApp programs", "Initialization error");
                Current.Shutdown();
                return;
            }

            if (endPoint.Port == 3215)
            {
                LoggerHelper.Error("It is detected that TCP port 3215 is occupied, please unoccupy the port.");
                MsgBoxHelper.Error("It is detected that TCP port 3215 is occupied, please unoccupy the port.\nUnder normal circumstances, you only need to close the Origin and EaApp programs", "Initialization error");
                Current.Shutdown();
                return;
            }

            if (endPoint.Port == 4219)
            {
                LoggerHelper.Error("It is detected that TCP port 4219 is occupied, please unoccupy the port.");
                MsgBoxHelper.Error("It is detected that TCP port 4219 is occupied, please unoccupy the port.\nUnder normal circumstances, you only need to close the Origin and EaApp programs", "Initialization error");
                Current.Shutdown();
                return;
            }
        }
        LoggerHelper.Info("Current system TCP port detection is normal");

        //////////////////////////////////////////////////////

        base.OnStartup(e);
    }

    /// <summary>
    /// Register exception capture event
    /// </summary>
    private void RegisterEvents()
    {
        DispatcherUnhandledException += App_DispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
    }

    /// <summary>
    /// The UI thread did not catch the exception handling event (UI main thread)
    /// </summary>
    private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        var msg = GetExceptionInfo(e.Exception, e.ToString());
        SaveCrashLog(msg);
    }

    /// <summary>
    /// The non-UI thread does not capture the exception handling event (such as a child thread created by yourself)
    /// </summary>
    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var msg = GetExceptionInfo(e.ExceptionObject as Exception, e.ToString());
        SaveCrashLog(msg);
    }

    /// <summary>
    /// Exception handling events are not captured in the Task thread
    /// </summary>
    private void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
    {
        // 2024/07/25
        // This exception cannot be resolved currently, so stop generating the corresponding crash log.
        if (e.Exception.Message.Equals("A Task's exception(s) were not observed either by Waiting on the Task or accessing its Exception property. As a result, the unobserved exception was rethrown by the finalizer thread. (The I/O operation was aborted due to thread exit or application request.)"))
        {
            LoggerHelper.Error("Task thread caught unhandled exception", e.Exception);
            return;
        }

        var msg = GetExceptionInfo(e.Exception, e.ToString());
        SaveCrashLog(msg);
    }

    /// <summary>
    /// Generate custom exception message
    /// </summary>
    private string GetExceptionInfo(Exception ex, string backStr)
    {
        var builder = new StringBuilder();

        builder.AppendLine($"Program version: {CoreUtil.VersionInfo}");
        builder.AppendLine($"Username: {CoreUtil.UserName}");
        builder.AppendLine($"Computer name: {CoreUtil.MachineName}");
        builder.AppendLine($"System version: {CoreUtil.OSVersion}");
        builder.AppendLine($"System directory: {CoreUtil.SystemDirectory}");
        builder.AppendLine($"Runtime platform: {CoreUtil.RuntimeVersion}");
        builder.AppendLine($"Runtime version: {CoreUtil.OSArchitecture}");
        builder.AppendLine($"Runtime environment: {CoreUtil.RuntimeIdentifier}");
        builder.AppendLine("------------------------------");
        builder.AppendLine($"Crash time: {DateTime.Now}");

        if (ex is not null)
        {
            builder.AppendLine($"Exception type: {ex.GetType().Name}");
            builder.AppendLine($"Exception information: {ex.Message}");
            builder.AppendLine($"Stack call: \n{ex.StackTrace}");
        }
        else
        {
            builder.AppendLine($"Unhandled exception: {backStr}");
        }

        return builder.ToString();
    }

    /// <summary>
    /// Save crash log
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