using EAappEmulater.Api;
using EAappEmulater.Helper;
using EAappEmulater.Models;
using EAappEmulater.Utils;

namespace EAappEmulater.Views;

/// <summary>
/// Interaction logic of FriendView.xaml
/// </summary>
public partial class FriendView : UserControl
{
    public ObservableCollection<FriendInfo> ObsCol_FriendInfos { get; set; } = new();

    private List<FriendInfo> _friendInfoList = new();

    public FriendView()
    {
        InitializeComponent();

        ToDoList();
    }

    private async void ToDoList()
    {
        Globals.IsGetFriendsSuccess = false;
        Globals.FriendsXmlString = string.Empty;
        Globals.QueryPresenceString = string.Empty;

        LoggerHelper.Info("Retrieving the friend list of the current account...");

        try
        {
            // Execute up to 4 times
            for (int i = 0; i <= 4; i++)
            {
                // When it still fails for the fourth time, terminate the program
                if (i > 3)
                {
                    LoggerHelper.Error("Failed to obtain the friend list of the current account, please check the network connection.");
                    return;
                }

                // Retry without prompting for the first time
                if (i > 0)
                {
                    LoggerHelper.Info($"Obtaining the friend list of the current account, starting the {i} retry...");
                }

                var friends = await EasyEaApi.GetUserFriends();
                if (friends is not null)
                {
                    LoggerHelper.Info("Successfully obtained the friend list of the current account");
                    LoggerHelper.Info($"The number of friends on the current account is {friends.entries.Count}");

                    foreach (var entry in friends.entries)
                    {
                        _friendInfoList.Add(new()
                        {
                            Avatar = "Default",
                            DiffDays = CoreUtil.GetDiffDays(entry.timestamp),

                            DisplayName = entry.displayName ?? string.Empty,
                            NickName = entry.nickName,
                            UserId = entry.userId,
                            PersonaId = entry.personaId,
                            FriendType = entry.friendType,
                            DateTime = CoreUtil.TimestampToDataTimeString(entry.timestamp)
                        });
                    }

                    // Sort in ascending order
                    // If DisplayName is null or other national characters, an exception may be thrown
                    _friendInfoList = _friendInfoList.OrderBy(p => p.DisplayName, StringComparer.InvariantCulture).ToList();

                    var index = 0;
                    foreach (var friendInfo in _friendInfoList)
                    {
                        friendInfo.Index = ++index;
                        ObsCol_FriendInfos.Add(friendInfo);
                    }

                    //Select the first one
                    if (ObsCol_FriendInfos.Count != 0)
                        ListBox_FriendInfo.SelectedIndex = 0;

                    // Generate friend list string
                    GenerateXmlString();
                    GenerateXmlStringForQueryPresence();

                    break;
                }
            }
        }
        catch (Exception ex)
        {
            LoggerHelper.Error("An exception occurred while obtaining the friend list of the current account.", ex);
        }
    }

    private void GenerateXmlString()
    {
        if (_friendInfoList.Count == 0)
            return;

        var doc = new XmlDocument();

        var lsx = doc.CreateElement("LSX");
        doc.AppendChild(lsx);

        var response = doc.CreateElement("Response");
        response.SetAttribute("id", "##ID##");
        response.SetAttribute("sender", "XMPP");
        lsx.AppendChild(response);

        var queryFreiResp = doc.CreateElement("QueryFriendsResponse");
        response.AppendChild(queryFreiResp);

        foreach (var friendInfo in _friendInfoList)
        {
            var friend = doc.CreateElement("Friend");

            var title = MiscUtil.GetRandomFriendTitle();

            friend.SetAttribute("RichPresence", title);
            friend.SetAttribute("AvatarId", "##AvatarId##");
            friend.SetAttribute("UserId", $"{friendInfo.UserId}");
            friend.SetAttribute("Group", "");
            friend.SetAttribute("Title", title);
            friend.SetAttribute("TitleId", "Origin.OFR.50.0004152");
            friend.SetAttribute("GamePresence", "");
            friend.SetAttribute("Persona", friendInfo.DisplayName);
            friend.SetAttribute("PersonaId", $"{friendInfo.PersonaId}");
            friend.SetAttribute("State", "MUTUAL");
            friend.SetAttribute("MultiplayerId", "196216");
            friend.SetAttribute("GroupId", "");
            friend.SetAttribute("Presence", "INGAME");

            queryFreiResp.AppendChild(friend);
        }

        Globals.FriendsXmlString = doc.InnerXml;
        Globals.IsGetFriendsSuccess = true;
    }
    private void GenerateXmlStringForQueryPresence()
    {
        if (_friendInfoList.Count == 0)
            return;

        var doc = new XmlDocument();

        var lsx = doc.CreateElement("LSX");
        doc.AppendChild(lsx);

        var response = doc.CreateElement("Response");
        response.SetAttribute("id", "##ID##");
        response.SetAttribute("sender", "XMPP");
        lsx.AppendChild(response);

        var queryFreiResp = doc.CreateElement("QueryPresenceResponse");
        response.AppendChild(queryFreiResp);

        foreach (var friendInfo in _friendInfoList)
        {
            var friend = doc.CreateElement("Friend");

            var title = MiscUtil.GetRandomFriendTitle();

            friend.SetAttribute("RichPresence", title);
            friend.SetAttribute("AvatarId", "##AvatarId##");
            friend.SetAttribute("UserId", $"{friendInfo.UserId}");
            friend.SetAttribute("Group", "");
            friend.SetAttribute("Title", title);
            friend.SetAttribute("TitleId", "Origin.OFR.50.0004152");
            friend.SetAttribute("GamePresence", "");
            friend.SetAttribute("Persona", friendInfo.DisplayName);
            friend.SetAttribute("PersonaId", $"{friendInfo.PersonaId}");
            friend.SetAttribute("State", "MUTUAL");
            friend.SetAttribute("MultiplayerId", "196216");
            friend.SetAttribute("GroupId", "");
            friend.SetAttribute("Presence", "INGAME");

            queryFreiResp.AppendChild(friend);
        }

        // Manually add an additional Friend element, which is the player's own information
        var extraFriend = doc.CreateElement("Friend");
        extraFriend.SetAttribute("RichPresence", "");
        extraFriend.SetAttribute("AvatarId", "");
        extraFriend.SetAttribute("UserId", "##UID##");
        extraFriend.SetAttribute("Group", "");
        extraFriend.SetAttribute("Title", "");
        extraFriend.SetAttribute("TitleId", "");
        extraFriend.SetAttribute("GamePresence", "");
        extraFriend.SetAttribute("Persona", "");
        extraFriend.SetAttribute("PersonaId", "------");
        extraFriend.SetAttribute("State", "NONE");
        extraFriend.SetAttribute("MultiplayerId", "");
        extraFriend.SetAttribute("GroupId", "");
        extraFriend.SetAttribute("Presence", "UNKNOWN");

        queryFreiResp.AppendChild(extraFriend);

        Globals.QueryPresenceString = doc.InnerXml;
    }
}
