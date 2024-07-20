using EAappEmulater.Api;
using EAappEmulater.Core;
using EAappEmulater.Utils;
using EAappEmulater.Helper;
using RestSharp;
using CommunityToolkit.Mvvm.Input;

namespace EAappEmulater.Windows;

/// <summary>
/// LoadWindow.xaml 的交互逻辑
/// </summary>
public partial class LoadWindow
{
    public LoadWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// 窗口加载完成事件
    /// </summary>
    private void Window_Load_Loaded(object sender, RoutedEventArgs e)
    {
    }

    /// <summary>
    /// 窗口内容呈现完毕后事件
    /// </summary>
    private async void Window_Load_ContentRendered(object sender, EventArgs e)
    {
        // 读取账号配置文件
        Account.Read();
        // 开始验证Cookie有效性
        await CheckCookie();
    }

    /// <summary>
    /// 窗口关闭时事件
    /// </summary>
    private void Window_Load_Closing(object sender, CancelEventArgs e)
    {
    }

    /// <summary>
    /// 显示加载状态到UI界面
    /// </summary>
    private void DisplayLoadState(string log)
    {
        TextBlock_CheckState.Text = log;
    }

    /// <summary>
    /// 检查Cookie信息
    /// </summary>
    private async Task CheckCookie()
    {
        DisplayLoadState("Checking player cookie validity...");
        LoggerHelper.Info("Checking player cookie validity...");

        // 最多执行4次
        for (int i = 0; i <= 4; i++)
        {
            // 当第4次还是失败，终止程序
            if (i > 3)
            {
                Loading_Normal.Visibility = Visibility.Collapsed;
                IconFont_NetworkError.Visibility = Visibility.Visible;
                DisplayLoadState("Failed to detect player cookie validity, program terminated, please check network connection");
                LoggerHelper.Error("Failed to detect player cookie validity, program terminated, please check network connection");
                return;
            }

            // 第1次不提示重试
            if (i > 0)
            {
                DisplayLoadState($"Failed to detect player cookie validity, starting the {i} the retry...");
                LoggerHelper.Warn($"Failed to detect player cookie validity, starting the {i} the retry...");
            }

            var result = await EaApi.GetToken();
            // 代表请求完成，排除超时情况
            if (result.StatusText == ResponseStatus.Completed)
            {
                if (result.IsSuccess)
                {
                    LoggerHelper.Info("Detecting player cookie validity successfully");
                    LoggerHelper.Info("Player cookies are valid");

                    // 如果Cookie有效，则开始初始化
                    await InitGameInfo();

                    return;
                }
                else
                {
                    Loading_Normal.Visibility = Visibility.Collapsed;
                    IconFont_NetworkError.Visibility = Visibility.Visible;
                    DisplayLoadState("The player cookie is invalid and the program is terminated. Please update the cookie manually.");
                    LoggerHelper.Error("The player cookie is invalid and the program is terminated. Please update the cookie manually.");
                    return;
                }
            }
        }
    }

    /// <summary>
    /// 初始化游戏信息
    /// </summary>
    private async Task InitGameInfo()
    {
        LoggerHelper.Info("Start initializing game information...");

        // 关闭服务进程
        await CoreUtil.CloseServiceProcess();

        DisplayLoadState("Releasing resource service process files...");
        LoggerHelper.Info("Releasing resource service process files...");
        FileHelper.ExtractResFile("Exec.EADesktop.exe", CoreUtil.File_Service_EADesktop);
        FileHelper.ExtractResFile("Exec.OriginDebug.exe", CoreUtil.File_Service_OriginDebug);

        /////////////////////////////////////////////////

        DisplayLoadState("Retrieving registry game information...");
        LoggerHelper.Info("Retrieving registry game information...");
        // 从注册表获取游戏安装信息
        foreach (var gameInfo in Base.GameInfoDb)
        {
            LoggerHelper.Info($"Start getting《{gameInfo.Value.Name}》Registry game information...");

            var installDir = RegistryHelper.GetRegistryInstallDir(gameInfo.Value.Regedit);
            if (!string.IsNullOrWhiteSpace(installDir))
            {
                gameInfo.Value.Dir = installDir;
                gameInfo.Value.IsInstalled = true;
                LoggerHelper.Info($"Get it from Regedit《{gameInfo.Value.Name}》Registry game information successful");
            }
            else
            {
                installDir = RegistryHelper.GetRegistryInstallDir(gameInfo.Value.Regedit2);
                if (!string.IsNullOrWhiteSpace(installDir))
                {
                    gameInfo.Value.Dir = installDir;
                    gameInfo.Value.IsInstalled = true;
                    LoggerHelper.Info($"Obtained from Regedit2《{gameInfo.Value.Name}》Registry game information successful");
                }
            }

            if (!gameInfo.Value.IsInstalled)
                LoggerHelper.Warn($"Not obtained《{gameInfo.Value.Name}》Registry game information");
        }

        /////////////////////////////////////////////////

        DisplayLoadState("Refreshing BaseToken data...");
        LoggerHelper.Info("Refreshing BaseToken data...");

        // 最多执行4次
        for (int i = 0; i <= 4; i++)
        {
            // 当第4次还是失败，终止程序
            if (i > 3)
            {
                Loading_Normal.Visibility = Visibility.Collapsed;
                IconFont_NetworkError.Visibility = Visibility.Visible;
                DisplayLoadState("Refreshing BaseToken data failed and the program terminated. Please check the network connection.");
                LoggerHelper.Error("Refreshing BaseToken data failed and the program terminated. Please check the network connection.");
                return;
            }

            // 第1次不提示重试
            if (i > 0)
            {
                DisplayLoadState($"Failed to refresh BaseToken data, start the {i} Retrying...");
                LoggerHelper.Warn($"Failed to refresh BaseToken data, start the {i} Retrying...");
            }

            if (await Ready.RefreshBaseTokens())
            {
                LoggerHelper.Info("Refreshing BaseToken data successfully");
                break;
            }
        }

        DisplayLoadState("Obtaining player account information...");
        LoggerHelper.Info("Obtaining player account information...");

        // 最多执行4次
        for (int i = 0; i <= 4; i++)
        {
            // 当第4次还是失败，终止程序
            if (i > 3)
            {
                Loading_Normal.Visibility = Visibility.Collapsed;
                IconFont_NetworkError.Visibility = Visibility.Visible;
                DisplayLoadState("Failed to obtain player account information. The program terminated. Please check the network connection.");
                LoggerHelper.Error("Failed to obtain player account information. The program terminated. Please check the network connection.");
                return;
            }

            // 第1次不提示重试
            if (i > 0)
            {
                DisplayLoadState($"Failed to obtain player account information, starting the {i} the retry...");
                LoggerHelper.Info($"Failed to obtain player account information, starting the {i} the retry...");
            }

            if (await Ready.GetLoginAccountInfo())
            {
                LoggerHelper.Info("Obtained player account information successfully");
                break;
            }
        }

        /////////////////////////////////////////////////

        // 保存账号配置文件
        Account.Write();

        DisplayLoadState("Initialization is complete, start the main program...");
        LoggerHelper.Info("Initialization is complete, start the main program...");

        var mainWindow = new MainWindow();

        // 转移主程序控制权
        Application.Current.MainWindow = mainWindow;
        // 关闭当前窗口
        this.Close();

        // 显示主窗口
        mainWindow.Show();
    }

    /// <summary>
    /// 打开配置文件
    /// </summary>
    [RelayCommand]
    private void OpenConfigFolder()
    {
        ProcessHelper.OpenDirectory(CoreUtil.Dir_Default);
    }

    /// <summary>
    /// 打开账号切换窗口
    /// </summary>
    [RelayCommand]
    private void RunAccountWindow()
    {
        var accountWindow = new AccountWindow();

        // 转移主程序控制权
        Application.Current.MainWindow = accountWindow;
        // 关闭当前窗口
        this.Close();

        // 显示切换账号窗口
        accountWindow.Show();
    }

    /// <summary>
    /// 退出程序
    /// </summary>
    [RelayCommand]
    private void ExitApplication()
    {
        Application.Current.Shutdown();
    }
}
