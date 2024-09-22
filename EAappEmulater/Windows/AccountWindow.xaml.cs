using CommunityToolkit.Mvvm.Input;
using EAappEmulater.Core;
using EAappEmulater.Helper;
using EAappEmulater.Models;
using EAappEmulater.Utils;

namespace EAappEmulater.Windows;

/// <summary>
/// Interaction logic of AccountWindow.xaml
/// </summary>
public partial class AccountWindow
{
    public ObservableCollection<AccountInfo> ObsCol_AccountInfos { get; set; } = new();

    public AccountWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Window loading completion event
    /// </summary>
    private void Window_Account_Loaded(object sender, RoutedEventArgs e)
    {
        Title = $"EA app emulator v{CoreUtil.VersionInfo}";

        // Traverse and read 10 configuration file slots
        foreach (var item in Account.AccountPathDb)
        {
            var account = new AccountInfo()
            {
                //Account slot
                AccountSlot = item.Key,
                // only for display
                PlayerName = IniHelper.ReadString("Account", "PlayerName", item.Value),
                AvatarId = IniHelper.ReadString("Account", "AvatarId", item.Value),
                Avatar = IniHelper.ReadString("Account", "Avatar", item.Value),
                // can be modified
                Remid = IniHelper.ReadString("Cookie", "Remid", item.Value),
                Sid = IniHelper.ReadString("Cookie", "Sid", item.Value)
            };

            // The player avatar is empty (only cookie data)
            if (!string.IsNullOrWhiteSpace(account.Remid) && string.IsNullOrWhiteSpace(account.Avatar))
                account.Avatar = "Default";

            //Add to dynamic collection
            ObsCol_AccountInfos.Add(account);
        }

        ////////////////////////////////

        //Read global configuration file
        Globals.Read();
        //Set the last selected configuration slot
        ListBox_AccountInfo.SelectedIndex = (int)Globals.AccountSlot;
    }

    /// <summary>
    /// Event after window content is rendered
    /// </summary>
    private void Window_Account_ContentRendered(object sender, EventArgs e)
    {
    }

    /// <summary>
    /// Event when the window is closed
    /// </summary>
    private void Window_Account_Closing(object sender, CancelEventArgs e)
    {
        SaveAccountCookie();
    }

    /// <summary>
    /// Save account cookie
    /// </summary>
    private bool SaveAccountCookie(bool isReset = false)
    {
        if (ListBox_AccountInfo.SelectedItem is not AccountInfo account)
            return false;

        //Set the currently selected configuration slot
        Globals.AccountSlot = account.AccountSlot;

        // Only use when changing to new account
        if (isReset)
        {
            account.PlayerName = string.Empty;
            account.AvatarId = string.Empty;
            account.Avatar = string.Empty;

            account.Remid = string.Empty;
            account.Sid = string.Empty;
        }

        foreach (var item in ObsCol_AccountInfos)
        {
            var path = Account.AccountPathDb[item.AccountSlot];

            IniHelper.WriteString("Cookie", "Remid", item.Remid, path);
            IniHelper.WriteString("Cookie", "Sid", item.Sid, path);
        }

        //Save global configuration file
        Globals.Write();

        return true;
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
    /// Log in to the selected account
    /// </summary>
    [RelayCommand]
    private void LoginAccount()
    {
        // save data
        if (!SaveAccountCookie())
            return;

        ////////////////////////////////

        var loadWindow = new LoadWindow();

        //Transfer control of main program
        Application.Current.MainWindow = loadWindow;
        // Close the current window
        this.Close();

        //Show initialization window
        loadWindow.Show();
    }

    /// <summary>
    /// Get Cookie
    /// </summary>
    [RelayCommand]
    private void GetCookie()
    {
        // save data
        if (!SaveAccountCookie())
            return;

        ////////////////////////////////

        var loginWindow = new LoginWindow(false);

        //Transfer control of main program
        Application.Current.MainWindow = loginWindow;
        // Close the current window
        this.Close();

        //Show login window
        loginWindow.Show();
    }

    /// <summary>
    /// Change account
    /// </summary>
    [RelayCommand]
    private void ChangeAccount()
    {
        // save data
        if (!SaveAccountCookie(true))
            return;

        // Clear current account information
        Account.Reset();
        Account.Write();

        ////////////////////////////////

        var loginWindow = new LoginWindow(true);

        //Transfer control of main program
        Application.Current.MainWindow = loginWindow;
        // Close the current window
        this.Close();

        //Show login window
        loginWindow.Show();
    }
}
