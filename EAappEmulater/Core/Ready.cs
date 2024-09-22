using CommunityToolkit.Mvvm.Messaging;
using EAappEmulater.Api;
using EAappEmulater.Helper;
using EAappEmulater.Utils;

namespace EAappEmulater.Core;

public static class Ready
{
    private static Timer _autoUpdateTimer;

    public static async void Run()
    {
        //Open the service process
        LoggerHelper.Info("Starting service process...");
        ProcessHelper.OpenProcess(CoreUtil.File_Service_EADesktop, true);
        ProcessHelper.OpenProcess(CoreUtil.File_Service_OriginDebug, true);

        LoggerHelper.Info("Starting LSX listening service...");
        LSXTcpServer.Run();

        LoggerHelper.Info("Starting Battlelog listening service...");
        BattlelogHttpServer.Run();

        //Load player avatar
        await LoadAvatar();

        // Check EA App registry
        RegistryHelper.CheckAndAddEaAppRegistryKey();

        //Refresh BaseToken data regularly
        LoggerHelper.Info("Starting the scheduled refresh BaseToken service...");
        _autoUpdateTimer = new Timer(AutoUpdateBaseToken, null, TimeSpan.FromHours(2), TimeSpan.FromHours(2));
        LoggerHelper.Info("Start the scheduled refresh BaseToken service successfully");
    }

    public static void Stop()
    {
        //Save global configuration file
        Globals.Write();

        //Save account configuration file
        Account.Write();

        LoggerHelper.Info("Closing LSX listening service...");
        LSXTcpServer.Stop();

        LoggerHelper.Info("Closing the Battlelog listening service...");
        BattlelogHttpServer.Stop();

        LoggerHelper.Info("Closing the scheduled refresh BaseToken service...");
        _autoUpdateTimer?.Dispose();
        _autoUpdateTimer = null;
        LoggerHelper.Info("Close regularly refresh BaseToken service successfully");

        // Close the service process
        CoreUtil.CloseServiceProcess();
    }

    /// <summary>
    /// Refresh BaseToken data regularly
    /// </summary>
    private static async void AutoUpdateBaseToken(object obj)
    {
        // Execute up to 4 times
        for (int i = 0; i <= 4; i++)
        {
            // When it still fails for the fourth time, terminate the program
            if (i > 3)
            {
                LoggerHelper.Error("Failed to refresh BaseToken data regularly. Please check the network connection.");
                return;
            }

            // Retry without prompting for the first time
            if (i > 0)
            {
                LoggerHelper.Warn($"Failed to refresh BaseToken data regularly, starting the {i} retry...");
            }

            if (await RefreshBaseTokens(false))
            {
                LoggerHelper.Info("Regularly refreshing BaseToken data successfully");
                break;
            }
        }
    }

    /// <summary>
    /// Very important, Api request preconditions
    /// Token necessary for refreshing basic requests (multiple)
    /// </summary>
    public static async Task<bool> RefreshBaseTokens(bool isInit = true)
    {
        // Refresh Access Token according to the situation
        if (!isInit)
        {
            // If it is initialization, this step can be omitted (because it is repeated)
            // But regular refresh is still needed (because the validity period is only 4 hours)
            var result = await EaApi.GetToken();
            if (!result.IsSuccess)
            {
                LoggerHelper.Warn("Failed to refresh Token");
                return false;
            }
            LoggerHelper.Info("Refresh Token successfully");
        }

        //////////////////////////////////////

        // Refresh OriginPCAuth
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
    /// Get the currently logged in player information
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
        LoggerHelper.Info($"Player Pid {Account.PersonaId}");

        Account.UserId = persona.pidId.ToString();
        LoggerHelper.Info($"Player UserId {Account.UserId}");

        return true;
    }

    /// <summary>
    /// Load logged in player avatar
    /// </summary>
    private static async Task LoadAvatar()
    {
        LoggerHelper.Info("Retrieving the avatar of the currently logged in player...");

        // Execute up to 4 times
        for (int i = 0; i <= 4; i++)
        {
            // When it still fails for the fourth time, terminate the program
            if (i > 3)
            {
                LoggerHelper.Error("Failed to obtain the avatar of the currently logged in player, please check the network connection.");
                return;
            }

            // Retry without prompting for the first time
            if (i > 0)
            {
                LoggerHelper.Info($"Get the avatar of the currently logged in player, starting the {i} retry...");
            }

            // Only if the avatar ID is empty can it be obtained from the network
            if (string.IsNullOrWhiteSpace(Account.AvatarId))
            {
                // Start getting the avatar player ID
                if (await GetAvatarByUserIds())
                {
                    // Download the avatar after successfully obtaining the avatar ID.
                    if (await DownloadAvatar())
                    {
                        return;
                    }
                }
            }
            else
            {
                // Download the avatar after successfully obtaining the avatar ID.
                if (await DownloadAvatar())
                {
                    return;
                }
            }
        }
    }

    /// <summary>
    /// Get player avatar IDs in batches
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

        //Get only the first one in the array
        var avatar = result.users.First().avatar;
        Account.AvatarId = avatar.avatarId.ToString();

        LoggerHelper.Info("Successfully obtained the avatar ID of the currently logged in player");
        LoggerHelper.Info($"Player AvatarId {Account.AvatarId}");

        return true;
    }

    /// <summary>
    /// Download player avatar
    /// </summary>
    private static async Task<bool> DownloadAvatar(bool isOverride = true)
    {
        string[] files = Directory.GetFiles(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Origin", "AvatarsCache"), $"{Account.UserId}.*");
        var savePath = string.Empty;
        string link = string.Empty;
        if (files.Length > 0)
        {
            Account.Avatar = files[0];
            LoggerHelper.Info($"Found local player avatar image cache, skipping network download operation {Account.Avatar}");
            WeakReferenceMessenger.Default.Send("", "LoadAvatar");
            return true;
        }

        var result = await EaApi.GetAvatarByUserId(Account.UserId);
        if (!result.IsSuccess)
        {
            LoggerHelper.Warn($"Failed to download avatar of currently logged in player {Account.UserId}");
            return false;
        }
        XmlDocument xmlDoc = new XmlDocument();
        xmlDoc.LoadXml(result.Content);
        XmlNode linkNode = xmlDoc.SelectSingleNode("//link");
        link = linkNode.InnerText;
        string fileName = link.Substring(link.LastIndexOf('/') + 1);
        savePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Origin", "AvatarsCache", fileName.Replace("208x208", Account.UserId));
        if (!await CoreApi.DownloadWebImage(link, savePath))
        {
            LoggerHelper.Warn($"Failed to download avatar of currently logged in player {Account.UserId}");
            return false;
        }
        Account.Avatar = savePath;

        LoggerHelper.Info($"Successfully downloaded the avatar of the currently logged in player");
        LoggerHelper.Info($"Player Avatar {Account.Avatar}");

        WeakReferenceMessenger.Default.Send("", "LoadAvatar");

        return true;
    }
}
