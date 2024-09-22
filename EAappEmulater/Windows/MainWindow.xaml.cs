using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using EAappEmulater.Api;
using EAappEmulater.Core;
using EAappEmulater.Helper;
using EAappEmulater.Models;
using EAappEmulater.Utils;
using EAappEmulater.Views;
using Hardcodet.Wpf.TaskbarNotification;
using Window = ModernWpf.Controls.Window;

namespace EAappEmulater.Windows;

/// <summary>
///Interaction logic of MainWindow.xaml
/// </summary>
public partial class MainWindow
{
    /// <summary>
    /// Navigation dictionary
    /// </summary>
    private readonly Dictionary<string, UserControl> NavDictionary = new();
    /// <summary>
    /// Data model
    /// </summary>
    public MainModel MainModel { get; set; } = new();

    /// <summary>
    /// Used to expose the main window instance to the outside world
    /// </summary>
    public static Window MainWinInstance { get; private set; }
    /// <summary>
    /// Window closing identification flag
    /// </summary>
    public static bool IsCodeClose { get; set; } = false;

    /// <summary>
    /// First notification flag
    /// </summary>
    private bool _isFirstNotice = false;

    public MainWindow()
    {
        InitializeComponent();

        CreateView();
    }

    /// <summary>
    /// Window loading completion event
    /// </summary>
    private async void Window_Main_Loaded(object sender, RoutedEventArgs e)
    {
        LoggerHelper.Info("Start main program successfully");

        Title = $"EA app emulator v{CoreUtil.VersionInfo}";

        // Expose the main window instance to the outside world
        MainWinInstance = this;
        //Reset window close flag
        IsCodeClose = false;

        //Home navigation
        Navigate(NavDictionary.First().Key);

        //////////////////////////////////////////

        // The player avatar is empty (only data account)
        if (!string.IsNullOrWhiteSpace(Account.Remid) && string.IsNullOrWhiteSpace(Account.Avatar))
            MainModel.Avatar = "Default";

        // Verify whether the player avatar is consistent with the player avatar ID
        if (!Account.Avatar.Contains(Account.AvatarId))
            MainModel.Avatar = "Default";

        // Display the current player's login account
        MainModel.PlayerName = Account.PlayerName;
        MainModel.PersonaId = Account.PersonaId;

        // Get updated avatar notification
        WeakReferenceMessenger.Default.Register<string, string>(this, "LoadAvatar", (s, e) =>
        {
            MainModel.Avatar = Account.Avatar;
        });

        //Initialization work
        Ready.Run();

        // Check for updates (executed last)
        await CheckUpdate();
    }

    /// <summary>
    /// Window close event
    /// </summary>
    private void Window_Main_Closing(object sender, CancelEventArgs e)
    {
        // Only executed when the user clicks to close from the UI
        if (!IsCodeClose)
        {
            // Cancel the close event and hide the main window
            e.Cancel = true;
            this.Hide();

            // Notify only for the first time
            if (!_isFirstNotice)
            {
                NotifyIcon_Main.ShowBalloonTip("EA app simulator has been minimized to the tray", "You can completely exit the program through the tray right-click menu", BalloonIcon.Info);
                _isFirstNotice = true;
            }

            return;
        }

        // Cleanup work
        Ready.Stop();

        // Release the tray icon
        NotifyIcon_Main?.Dispose();
        NotifyIcon_Main = null;

        LoggerHelper.Info("Close main program successfully");
    }

    /// <summary>
    /// Create page
    /// </summary>
    private void CreateView()
    {
        NavDictionary.Add("GameView", new GameView());
        NavDictionary.Add("Game2View", new Game2View());
        NavDictionary.Add("FriendView", new FriendView());
        NavDictionary.Add("LogView", new LogView());
        NavDictionary.Add("UpdateView", new UpdateView());
        NavDictionary.Add("AboutView", new AboutView());

        NavDictionary.Add("AccountView", new AccountView());
        NavDictionary.Add("SettingView", new SettingView());
    }

    /// <summary>
    /// View page navigation
    /// </summary>
    [RelayCommand]
    private void Navigate(string viewName)
    {
        if (!NavDictionary.ContainsKey(viewName))
            return;

        if (ContentControl_NavRegion.Content == NavDictionary[viewName])
            return;

        ContentControl_NavRegion.Content = NavDictionary[viewName];
    }

    /// <summary>
    /// Check for updates
    /// </summary>
    private async Task CheckUpdate()
    {
        LoggerHelper.Info("Detecting new version...");
        NotifierHelper.Notice("Detecting new version...");

        // Execute up to 4 times
        for (int i = 0; i <= 4; i++)
        {
            // When it still fails for the fourth time, terminate the program
            if (i > 3)
            {
                IconHyperlink_Update.Text = $"Found a new version, click to download the update";
                IconHyperlink_Update.Visibility = Visibility.Visible;

                LoggerHelper.Error("Failed to detect new version, please check network connection");
                NotifierHelper.Error("Failed to detect new version, please check network connection");
                return;
            }

            // Retry without prompting for the first time
            if (i > 0)
            {
                LoggerHelper.Warn($"Failed to detect new version, starting the {i} retry...");
            }

            var webVersion = await CoreApi.GetWebUpdateVersion();
            if (webVersion is not null)
            {
                if (CoreUtil.VersionInfo >= webVersion)
                {
                    LoggerHelper.Info($"Congratulations, this is the latest version {CoreUtil.VersionInfo}");
                    NotifierHelper.Info($"Congratulations, this is the latest version {CoreUtil.VersionInfo}");
                    return;
                }

                IconHyperlink_Update.Text = $"Found new version v{webVersion}，Click to download updates";
                IconHyperlink_Update.Visibility = Visibility.Visible;

                LoggerHelper.Info($"Found the latest version, please go to the official website to download the latest version {webVersion}");
                NotifierHelper.Warning($"Found the latest version, please go to the official website to download the latest version {webVersion}");
                return;
            }
        }
    }

    [RelayCommand]
    private void ShowWindow()
    {
        this.Show();

        if (this.WindowState == WindowState.Minimized)
            this.WindowState = WindowState.Normal;

        this.Activate();
        this.Focus();
    }

    [RelayCommand]
    private void SwitchAccount()
    {
        var accountWindow = new AccountWindow();

        //Transfer control of main program
        Application.Current.MainWindow = accountWindow;
        //Set the close flag
        IsCodeClose = true;
        // Close the main window
        this.Close();

        //Display the change account window
        accountWindow.Show();
    }

    [RelayCommand]
    private void ExitApp()
    {
        //Set the close flag
        IsCodeClose = true;
        this.Close();
    }
}