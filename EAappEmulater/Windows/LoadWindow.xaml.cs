using CommunityToolkit.Mvvm.Input;
using EAappEmulater.Api;
using EAappEmulater.Core;
using EAappEmulater.Helper;
using EAappEmulater.Utils;
using RestSharp;

namespace EAappEmulater.Windows;

/// <summary>
/// Interaction logic of LoadWindow.xaml
/// </summary>
public partial class LoadWindow
{
    public LoadWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Window loading completion event
    /// </summary>
    private void Window_Load_Loaded(object sender, RoutedEventArgs e)
    {
    }

    /// <summary>
    /// Event after window content is rendered
    /// </summary>
    private async void Window_Load_ContentRendered(object sender, EventArgs e)
    {
        //Read account configuration file
        Account.Read();
        // Start verifying cookie validity
        await CheckCookie();
    }

    /// <summary>
    /// Event when the window is closed
    /// </summary>
    private void Window_Load_Closing(object sender, CancelEventArgs e)
    {
    }

    /// <summary>
    /// Display loading status to UI interface
    /// </summary>
    private void DisplayLoadState(string log)
    {
        TextBlock_CheckState.Text = log;
    }

    /// <summary>
    /// Check cookie information
    /// </summary>
    private async Task CheckCookie()
    {
        DisplayLoadState("Checking player cookie validity...");
        LoggerHelper.Info("Checking player cookie validity...");

        // Execute up to 4 times
        for (int i = 0; i <= 4; i++)
        {
            // When it still fails for the 4th time, terminate the program // Execute up to 4 times
            if (i > 3)
            {
                Loading_Normal.Visibility = Visibility.Collapsed;
                IconFont_NetworkError.Visibility = Visibility.Visible;
                DisplayLoadState("Failed to detect player cookie validity, program terminated, please check network connection");
                LoggerHelper.Error("Failed to detect player cookie validity, program terminated, please check network connection");
                return;
            }

            // Retry without prompting for the first time
            if (i > 0)
            {
                DisplayLoadState($"Failed to detect player cookie validity, starting the {i} retry...");
                LoggerHelper.Warn($"Failed to detect player cookie validity, starting the {i} retry...");
            }

            var result = await EaApi.GetToken();
            // Indicates that the request is completed, excluding timeout situations
            if (result.StatusText == ResponseStatus.Completed)
            {
                if (result.IsSuccess)
                {
                    LoggerHelper.Info("Detecting player cookie validity successfully");
                    LoggerHelper.Info("Player cookies are valid");

                    // If the cookie is valid, start initialization
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
    /// Initialize game information
    /// </summary>
    private async Task InitGameInfo()
    {
        LoggerHelper.Info("Start initializing game information...");

        // Close the service process
        CoreUtil.CloseServiceProcess();

        DisplayLoadState("Releasing resource service process files...");
        LoggerHelper.Info("Releasing resource service process files...");
        FileHelper.ExtractResFile("Exec.EADesktop.exe", CoreUtil.File_Service_EADesktop);
        FileHelper.ExtractResFile("Exec.OriginDebug.exe", CoreUtil.File_Service_OriginDebug);

        /////////////////////////////////////////////////

        DisplayLoadState("Retrieving registry game information...");
        LoggerHelper.Info("Retrieving registry game information...");
        // Get game installation information from the registry
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

        // Execute up to 4 times
        for (int i = 0; i <= 4; i++)
        {
            // When it still fails for the fourth time, terminate the program
            if (i > 3)
            {
                Loading_Normal.Visibility = Visibility.Collapsed;
                IconFont_NetworkError.Visibility = Visibility.Visible;
                DisplayLoadState("Refreshing BaseToken data failed and the program terminated. Please check the network connection.");
                LoggerHelper.Error("Refreshing BaseToken data failed and the program terminated. Please check the network connection.");
                return;
            }

            // Retry without prompting for the first time
            if (i > 0)
            {
                DisplayLoadState($"Failed to refresh BaseToken data, starting the {i} retry...");
                LoggerHelper.Warn($"Failed to refresh BaseToken data, starting the {i} retry...");
            }

            if (await Ready.RefreshBaseTokens())
            {
                LoggerHelper.Info("Refreshing BaseToken data successfully");
                break;
            }
        }

        DisplayLoadState("Obtaining player account information...");
        LoggerHelper.Info("Obtaining player account information...");

        // Execute up to 4 times
        for (int i = 0; i <= 4; i++)
        {
            // When it still fails for the fourth time, terminate the program
            if (i > 3)
            {
                Loading_Normal.Visibility = Visibility.Collapsed;
                IconFont_NetworkError.Visibility = Visibility.Visible;
                DisplayLoadState("Failed to obtain player account information. The program terminated. Please check the network connection.");
                LoggerHelper.Error("Failed to obtain player account information. The program terminated. Please check the network connection.");
                return;
            }

            // Retry without prompting for the first time
            if (i > 0)
            {
                DisplayLoadState($"Failed to obtain player account information, starting the {i} retry...");
                LoggerHelper.Info($"Failed to obtain player account information, starting the {i} retry...");
            }

            if (await Ready.GetLoginAccountInfo())
            {
                LoggerHelper.Info("Obtained player account information successfully");
                break;
            }
        }

        /////////////////////////////////////////////////

        //Save account configuration file
        Account.Write();

        DisplayLoadState("Initialization is complete, start the main program...");
        LoggerHelper.Info("Initialization is complete, start the main program...");

        var mainWindow = new MainWindow();

        //Transfer control of main program
        Application.Current.MainWindow = mainWindow;
        // Close the current window
        this.Close();

        //Show the main window
        mainWindow.Show();
    }

    /// <summary>
    /// Open configuration file
    /// </summary>
    [RelayCommand]
    private void OpenConfigFolder()
    {
        ProcessHelper.OpenDirectory(CoreUtil.Dir_Default);
    }

    /// <summary>
    /// Open the account switching window
    /// </summary>
    [RelayCommand]
    private void RunAccountWindow()
    {
        var accountWindow = new AccountWindow();

        //Transfer control of main program
        Application.Current.MainWindow = accountWindow;
        // Close the current window
        this.Close();

        //Display the switch account window
        accountWindow.Show();
    }

    /// <summary>
    ///Exit the program
    /// </summary>
    [RelayCommand]
    private void ExitApplication()
    {
        Application.Current.Shutdown();
    }
}
