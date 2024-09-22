using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using EAappEmulater.Core;
using EAappEmulater.Models;
using EAappEmulater.Windows;

namespace EAappEmulater.Views;

/// <summary>
/// Interaction logic of AccountView.xaml
/// </summary>
public partial class AccountView : UserControl
{
    public AccountModel AccountModel { get; set; } = new();

    public AccountView()
    {
        InitializeComponent();

        ToDoList();
    }

    private void ToDoList()
    {
        AccountModel.AvatarId = Account.AvatarId;

        // The player avatar is empty (only data account)
        if (!string.IsNullOrWhiteSpace(Account.Remid) && string.IsNullOrWhiteSpace(Account.Avatar))
            AccountModel.Avatar = "Default";

        // Verify whether the player avatar is consistent with the player avatar ID
        if (!Account.Avatar.Contains(Account.AvatarId))
            AccountModel.Avatar = "Default";

        AccountModel.PlayerName = Account.PlayerName;
        AccountModel.PersonaId = Account.PersonaId;
        AccountModel.UserId = Account.UserId;

        AccountModel.Remid = Account.Remid;
        AccountModel.Sid = Account.Sid;
        AccountModel.Token = Account.AccessToken;

        //////////////////////////////////////////

        WeakReferenceMessenger.Default.Register<string, string>(this, "LoadAvatar", (s, e) =>
        {
            AccountModel.AvatarId = Account.AvatarId;
            AccountModel.Avatar = Account.Avatar;
        });
    }

    /// <summary>
    /// Change account
    /// </summary>
    [RelayCommand]
    private void SwitchAccount()
    {
        var accountWindow = new AccountWindow();

        //Transfer control of main program
        Application.Current.MainWindow = accountWindow;
        //Set the close flag
        MainWindow.IsCodeClose = true;
        // Close the main window
        MainWindow.MainWinInstance.Close();

        //Display the change account window
        accountWindow.Show();
    }
}
