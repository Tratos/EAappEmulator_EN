using EAappEmulater.Api;
using EAappEmulater.Utils;
using EAappEmulater.Helper;
using CommunityToolkit.Mvvm.Messaging;

namespace EAappEmulater.Core;

public static class Ready
{
    private static Timer _autoUpdateTimer;

    public static async void Run()
    {
        // 打开服务进程
        LoggerHelper.Info("Starting service process...");
        ProcessHelper.OpenProcess(CoreUtil.File_Service_EADesktop, true);
        ProcessHelper.OpenProcess(CoreUtil.File_Service_OriginDebug, true);

        LoggerHelper.Info("Starting LSX listening service...");
        LSXTcpServer.Run();

        LoggerHelper.Info("Starting Battlelog listening service...");
        BattlelogHttpServer.Run();

        // 加载玩家头像
        await LoadAvatar();

        // 检查EA App注册表
        RegistryHelper.CheckAndAddEaAppRegistryKey();

        // 定时刷新 BaseToken 数据
        LoggerHelper.Info("Starting the scheduled refresh BaseToken service...");
        _autoUpdateTimer = new Timer(AutoUpdateBaseToken, null, TimeSpan.FromHours(2), TimeSpan.FromHours(2));
        LoggerHelper.Info("Start the scheduled refresh BaseToken service successfully");
    }

    public static async void Stop()
    {
        // 保存全局配置文件
        Globals.Write();

        // 保存账号配置文件
        Account.Write();

        LoggerHelper.Info("Closing LSX listening service...");
        LSXTcpServer.Stop();

        LoggerHelper.Info("Closing the Battlelog listening service...");
        BattlelogHttpServer.Stop();

        LoggerHelper.Info("Closing the scheduled refresh BaseToken service...");
        _autoUpdateTimer?.Dispose();
        _autoUpdateTimer = null;
        LoggerHelper.Info("Close regularly refresh BaseToken service successfully");

        // 关闭服务进程
        await CoreUtil.CloseServiceProcess();
    }

    /// <summary>
    /// 定时刷新 BaseToken 数据
    /// </summary>
    private static async void AutoUpdateBaseToken(object obj)
    {
        // 最多执行4次
        for (int i = 0; i <= 4; i++)
        {
            // 当第4次还是失败，终止程序
            if (i > 3)
            {
                LoggerHelper.Error("Failed to refresh BaseToken data regularly. Please check the network connection.");
                return;
            }

            // 第1次不提示重试
            if (i > 0)
            {
                LoggerHelper.Warn($"The scheduled refresh of BaseToken data failed, and the first {i} Retrying...");
            }

            if (await RefreshBaseTokens(false))
            {
                LoggerHelper.Info("Regularly refreshing BaseToken data successfully");
                break;
            }
        }
    }

    /// <summary>
    /// 非常重要，Api请求前置条件
    /// 刷新基础请求必备Token (多个)
    /// </summary>
    public static async Task<bool> RefreshBaseTokens(bool isInit = true)
    {
        // 根据情况刷新 Access Token
        if (!isInit)
        {
            // 如果是初始化，则这一步可以省略（因为重复了）
            // 但是定时刷新还是需要（因为有效期只有4小时）
            var result = await EaApi.GetToken();
            if (!result.IsSuccess)
            {
                LoggerHelper.Warn("Failed to refresh Token");
                return false;
            }
            LoggerHelper.Info("Refresh Token successfully");
        }

        //////////////////////////////////////

        // 刷新 OriginPCAuth
        {
            var result = await EaApi.GetOriginPCAuth();
            if (!result.IsSuccess)
            {
                LoggerHelper.Warn("Refreshing OriginPCAuth failed");
                return false;
            }
            LoggerHelper.Info("Refresh OriginPCAuth successfully");
        }

        // OriginPCToken
        {
            var result = await EaApi.GetOriginPCToken();
            if (!result.IsSuccess)
            {
                LoggerHelper.Warn("Failed to refresh OriginPCToken");
                return false;
            }
            LoggerHelper.Info("Refresh OriginPCToken successfully");
        }

        return true;
    }

    /// <summary>
    /// 获取当前登录玩家信息
    /// </summary>
    public static async Task<bool> GetLoginAccountInfo()
    {
        LoggerHelper.Info("Retrieving currently logged in player information...");
        var result = await EasyEaApi.GetLoginAccountInfo();
        if (result is null)
        {
            LoggerHelper.Warn("Failed to obtain currently logged in player information");
            return false;
        }

        LoggerHelper.Info("Successfully obtained the currently logged in player information");
        var persona = result.personas.persona[0];

        Account.PlayerName = persona.displayName;
        LoggerHelper.Info($"player name {Account.PlayerName}");

        Account.PersonaId = persona.personaId.ToString();
        LoggerHelper.Info($"Player PId {Account.PersonaId}");

        Account.UserId = persona.pidId.ToString();
        LoggerHelper.Info($"Player UserId {Account.UserId}");

        return true;
    }

    /// <summary>
    /// 加载登录玩家头像
    /// </summary>
    private static async Task LoadAvatar()
    {
        LoggerHelper.Info("Retrieving the avatar of the currently logged in player...");

        // 最多执行4次
        for (int i = 0; i <= 4; i++)
        {
            // 当第4次还是失败，终止程序
            if (i > 3)
            {
                LoggerHelper.Error("Failed to obtain the avatar of the currently logged in player, please check the network connection.");
                return;
            }

            // 第1次不提示重试
            if (i > 0)
            {
                LoggerHelper.Info($"Get the avatar of the currently logged in player Get the avatar of the currently logged in player and start the chapter {i} Retrying...");
            }

            // 只有头像Id为空才网络获取
            if (string.IsNullOrWhiteSpace(Account.AvatarId))
            {
                // 开始获取头像玩家Id
                if (await GetAvatarByUserIds())
                {
                    // 获取头像Id成功后下载头像
                    if (await DownloadAvatar())
                    {
                        return;
                    }
                }
            }
            else
            {
                // 获取头像Id成功后下载头像
                if (await DownloadAvatar())
                {
                    return;
                }
            }
        }
    }

    /// <summary>
    /// 批量获取玩家头像Id
    /// </summary>
    private static async Task<bool> GetAvatarByUserIds()
    {
        LoggerHelper.Info("Retrieving the avatar ID of the currently logged in player...");

        var userIds = new List<string>
        {
            Account.UserId
        };

        var result = await EasyEaApi.GetAvatarByUserIds(userIds);
        if (result is null)
        {
            LoggerHelper.Warn("Failed to obtain avatar ID of currently logged in player");
            return false;
        }

        // 仅获取数组第一个
        var avatar = result.users.First().avatar;
        Account.AvatarId = avatar.avatarId.ToString();

        LoggerHelper.Info("Successfully obtained the avatar ID of the currently logged in player");
        LoggerHelper.Info($"Player AvatarId {Account.AvatarId}");

        return true;
    }

    /// <summary>
    /// 下载玩家头像
    /// </summary>
    private static async Task<bool> DownloadAvatar(bool isOverride = true)
    {
        var savePath = Path.Combine(CoreUtil.Dir_Avatar, $"{Account.AvatarId}.png");
        if (File.Exists(savePath) && isOverride)
        {
            Account.Avatar = savePath;

            LoggerHelper.Info($"Found local player avatar image cache, skipping network download operation {Account.Avatar}");
            WeakReferenceMessenger.Default.Send("", "LoadAvatar");

            return true;
        }

        var avatarLink = $"https://secure.download.dm.origin.com/production/avatar/prod/userAvatar/{Account.AvatarId}/208x208.JPEG ";

        // 开始缓存玩家头像到本地
        if (!await CoreApi.DownloadWebImage(avatarLink, savePath))
        {
            LoggerHelper.Warn($"Failed to download avatar of currently logged in player {Account.AvatarId}");
            return false;
        }

        Account.Avatar = savePath;

        LoggerHelper.Info($"Successfully downloaded the avatar of the currently logged in player");
        LoggerHelper.Info($"Player Avatar {Account.Avatar}");

        WeakReferenceMessenger.Default.Send("", "LoadAvatar");

        return true;
    }
}
