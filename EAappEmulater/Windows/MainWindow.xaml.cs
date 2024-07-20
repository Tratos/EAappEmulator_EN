﻿using EAappEmulater.Api;
using EAappEmulater.Core;
using EAappEmulater.Utils;
using EAappEmulater.Views;
using EAappEmulater.Helper;
using EAappEmulater.Models;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Hardcodet.Wpf.TaskbarNotification;
using Window = ModernWpf.Controls.Window;

namespace EAappEmulater.Windows;

/// <summary>
/// MainWindow.xaml 的交互逻辑
/// </summary>
public partial class MainWindow
{
    /// <summary>
    /// 导航字典
    /// </summary>
    private readonly Dictionary<string, UserControl> NavDictionary = new();
    /// <summary>
    /// 数据模型
    /// </summary>
    public MainModel MainModel { get; set; } = new();

    /// <summary>
    /// 用于向外暴露主窗口实例
    /// </summary>
    public static Window MainWinInstance { get; private set; }
    /// <summary>
    /// 窗口关闭识别标志
    /// </summary>
    public static bool IsCodeClose { get; set; } = false;

    /// <summary>
    /// 第一次通知标志
    /// </summary>
    private bool _isFirstNotice = false;

    public MainWindow()
    {
        InitializeComponent();

        CreateView();
    }

    /// <summary>
    /// 窗口加载完成事件
    /// </summary>
    private async void Window_Main_Loaded(object sender, RoutedEventArgs e)
    {
        LoggerHelper.Info("Start main program successfully");

        Title = $"EA app emulator v{CoreUtil.VersionInfo}";

        // 向外暴露主窗口实例
        MainWinInstance = this;
        // 重置窗口关闭标志
        IsCodeClose = false;

        // 首页导航
        Navigate(NavDictionary.First().Key);

        //////////////////////////////////////////

        // 玩家头像为空处理（仅有数据账号）
        if (!string.IsNullOrWhiteSpace(Account.Remid) && string.IsNullOrWhiteSpace(Account.Avatar))
            MainModel.Avatar = "Default";

        // 验证玩家头像与玩家头像Id是否一致
        if (!Account.Avatar.Contains(Account.AvatarId))
            MainModel.Avatar = "Default";

        // 显示当前玩家登录账号
        MainModel.PlayerName = Account.PlayerName;
        MainModel.PersonaId = Account.PersonaId;

        // 获取更新头像通知
        WeakReferenceMessenger.Default.Register<string, string>(this, "LoadAvatar", (s, e) =>
        {
            MainModel.Avatar = Account.Avatar;
        });

        // 初始化工作
        Ready.Run();

        // 检查更新（放到最后执行）
        await CheckUpdate();
    }

    /// <summary>
    /// 窗口关闭事件
    /// </summary>
    private void Window_Main_Closing(object sender, CancelEventArgs e)
    {
        // 当用户从UI点击关闭时才执行
        if (!IsCodeClose)
        {
            // 取消关闭事件，隐藏主窗口
            e.Cancel = true;
            this.Hide();

            // 仅第一次通知
            if (!_isFirstNotice)
            {
                NotifyIcon_Main.ShowBalloonTip("EA app The emulator is minimized to the tray", "You can completely exit the program through the tray right-click menu", BalloonIcon.Info);
                _isFirstNotice = true;
            }

            return;
        }

        // 清理工作
        Ready.Stop();

        // 释放托盘图标
        NotifyIcon_Main?.Dispose();
        NotifyIcon_Main = null;

        LoggerHelper.Info("Close main program successfully");
    }

    /// <summary>
    /// 创建页面
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
    /// View页面导航
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
    /// 检查更新
    /// </summary>
    private async Task CheckUpdate()
    {
        LoggerHelper.Info("Detecting new version...");
        NotifierHelper.Notice("Detecting new version...");

        // 最多执行4次
        for (int i = 0; i <= 4; i++)
        {
            // 当第4次还是失败，终止程序
            if (i > 3)
            {
                IconHyperlink_Update.Text = $"Found a new version, click to download the update";
                IconHyperlink_Update.Visibility = Visibility.Visible;

                LoggerHelper.Error("Failed to detect new version, please check network connection");
                NotifierHelper.Error("Failed to detect new version, please check network connection");
                return;
            }

            // 第1次不提示重试
            if (i > 0)
            {
                LoggerHelper.Warn($"Failed to detect new version, start the next {i} Retrying...");
            }

            var webVersion = await CoreApi.GetWebUpdateVersion();
            if (webVersion is not null)
            {
                if (CoreUtil.VersionInfo >= webVersion)
                {
                    LoggerHelper.Info($"Congratulations，Currently the latest version {CoreUtil.VersionInfo}");
                    NotifierHelper.Info($"Congratulations，Currently the latest version {CoreUtil.VersionInfo}");
                    return;
                }

                IconHyperlink_Update.Text = $"new version found v{webVersion}，Click to download updates";
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

        // 转移主程序控制权
        Application.Current.MainWindow = accountWindow;
        // 设置关闭标志
        IsCodeClose = true;
        // 关闭主窗口
        this.Close();

        // 显示更换账号窗口
        accountWindow.Show();
    }

    [RelayCommand]
    private void ExitApp()
    {
        // 设置关闭标志
        IsCodeClose = true;
        this.Close();
    }
}