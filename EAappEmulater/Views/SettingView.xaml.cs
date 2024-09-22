using CommunityToolkit.Mvvm.Input;
using EAappEmulater.Helper;
using EAappEmulater.Utils;

namespace EAappEmulater.Views;

/// <summary>
/// Interaction logic of SettingView.xaml
/// </summary>
public partial class SettingView : UserControl
{
    public SettingView()
    {
        InitializeComponent();

        ToDoList();
    }

    private void ToDoList()
    {
        FormLabel_VersionInfo.Content = CoreUtil.VersionInfo.ToString();

        FormLabel_UserName.Content = CoreUtil.UserName;
        FormLabel_MachineName.Content = CoreUtil.MachineName;
        FormLabel_OSVersion.Content = CoreUtil.OSVersion;
        FormLabel_SystemDirectory.Content = CoreUtil.SystemDirectory;

        FormLabel_RuntimeVersion.Content = CoreUtil.RuntimeVersion;
        FormLabel_OSArchitecture.Content = CoreUtil.OSArchitecture;
        FormLabel_RuntimeIdentifier.Content = CoreUtil.RuntimeIdentifier;
    }

    /// <summary>
    /// Open configuration file
    /// </summary>
    [RelayCommand]
    private void OpenConfigFolder()
    {
        ProcessHelper.OpenDirectory(CoreUtil.Dir_Default);
    }
}
