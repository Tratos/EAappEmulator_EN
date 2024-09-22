using CommunityToolkit.Mvvm.Input;
using EAappEmulater.Core;
using EAappEmulater.Enums;
using EAappEmulater.Helper;
using EAappEmulater.Models;

namespace EAappEmulater.Windows;

/// <summary>
///Interaction logic of AdvancedWindow.xaml
/// </summary>
public partial class AdvancedWindow
{
    public AdvancedModel AdvancedModel { get; set; } = new();
    public List<LocaleInfo> LocaleInfos { get; set; } = new();

    private readonly GameInfo _gameInfo;

    private bool _isGetRegeditSuccess;

    public AdvancedWindow(GameType gameType)
    {
        InitializeComponent();

        _gameInfo = Base.GameInfoDb[gameType];

        AdvancedModel.Name = _gameInfo.Name;
        AdvancedModel.Name2 = _gameInfo.Name2;
        AdvancedModel.Image = _gameInfo.Image;

        AdvancedModel.IsUseCustom = _gameInfo.IsUseCustom;

        AdvancedModel.GameDir = _gameInfo.Dir;
        AdvancedModel.GameArgs = _gameInfo.Args;
        AdvancedModel.GameDir2 = _gameInfo.Dir2;
        AdvancedModel.GameArgs2 = _gameInfo.Args2;

        /////////////////////////////////////////////////

        LocaleInfos.Add(Base.GameLocaleDb.First().Value);

        foreach (var locale in _gameInfo.Locales)
        {
            if (Base.GameLocaleDb.TryGetValue(locale, out LocaleInfo info))
            {
                LocaleInfos.Add(info);
            }
            else
            {
                LocaleInfos.Add(new()
                {
                    Code = locale
                });
            }
        }
    }

    /// <summary>
    /// Window loading completion event
    /// </summary>
    private void Window_Advanced_Loaded(object sender, RoutedEventArgs e)
    {
        GetGameLocale();
    }

    /// <summary>
    /// Window close event
    /// </summary>
    private void Window_Advanced_Closing(object sender, CancelEventArgs e)
    {
    }

    /// <summary>
    /// Select file
    /// </summary>
    [RelayCommand]
    private void SelcetFilePath()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Please select the current game main program exe file path",
            FileName = _gameInfo.AppName,
            DefaultExt = ".exe",
            Filter = "executable file (.exe)|*.exe",
            Multiselect = false,
            RestoreDirectory = true,
            AddExtension = true,
            CheckFileExists = true,
            CheckPathExists = true
        };

        if (dialog.ShowDialog() == true)
        {
            AdvancedModel.GameDir2 = Path.GetDirectoryName(dialog.FileName);
        }
    }

    /// <summary>
    /// Save settings
    /// </summary>
    [RelayCommand]
    private void SaveOption()
    {
        Base.GameInfoDb[_gameInfo.GameType].IsUseCustom = AdvancedModel.IsUseCustom;

        // GameDir registry acquisition, modification is prohibited
        Base.GameInfoDb[_gameInfo.GameType].Args = AdvancedModel.GameArgs;

        Base.GameInfoDb[_gameInfo.GameType].Dir2 = AdvancedModel.GameDir2;
        Base.GameInfoDb[_gameInfo.GameType].Args2 = AdvancedModel.GameArgs2;

        SetGameLocale();

        this.Close();
    }

    /// <summary>
    /// Cancel setting
    /// </summary>
    [RelayCommand]
    private void CancelOption()
    {
        this.Close();
    }

    /// <summary>
    /// Get registry game language information
    /// </summary>
    private void GetGameLocale()
    {
        var locale = RegistryHelper.GetRegistryLocale(_gameInfo.Regedit);
        if (!string.IsNullOrWhiteSpace(locale))
        {
            var index = LocaleInfos.FindIndex(x => x.Code == locale);
            ComboBox_LocaleInfos.SelectedIndex = index == -1 ? 0 : index;

            _isGetRegeditSuccess = true;
            return;
        }

        locale = RegistryHelper.GetRegistryLocale(_gameInfo.Regedit2);
        if (!string.IsNullOrWhiteSpace(locale))
        {
            var index = LocaleInfos.FindIndex(x => x.Code == locale);
            ComboBox_LocaleInfos.SelectedIndex = index == -1 ? 0 : index;

            _isGetRegeditSuccess = true;
            return;
        }

        ComboBox_LocaleInfos.SelectedIndex = 0;
    }

    /// <summary>
    ///Set registry game language information
    /// </summary>
    private void SetGameLocale()
    {
        if (!_isGetRegeditSuccess)
            return;

        if (string.IsNullOrWhiteSpace(AdvancedModel.GameDir))
            return;

        if (!Directory.Exists(AdvancedModel.GameDir))
            return;

        if (ComboBox_LocaleInfos.SelectedItem is not LocaleInfo item || item.Code == "NULL")
            return;

        RegistryHelper.SetRegistryTargetVaule(_gameInfo.Regedit, "Locale", item.Code);
        RegistryHelper.SetRegistryTargetVaule(_gameInfo.Regedit2, "Locale", item.Code);
    }

    /// <summary>
    /// Press and hold the left mouse button to move the window
    /// </summary>
    private void Image_Game_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        DragMove();
    }
}
