using EAappEmulater.Core;
using EAappEmulater.Helper;

namespace EAappEmulater.Api;

public static class EasyEaApi
{
    /// <summary>
    /// Get the AutuCode required by the LSX listening service
    /// </summary>
    public static async Task<string> GetLSXAutuCode(string settingId)
    {
        var result = await EaApi.GetLSXAutuCode(settingId);
        if (!result.IsSuccess)
            return string.Empty;

        return Account.LSXAuthCode;
    }

    /// <summary>
    /// Obtain the license required for the LSX listening service License
    /// </summary>
    public static async Task<string> GetLSXLicense(string requestToken, string contentId)
    {
        var result = await EaApi.GetLSXLicense(requestToken, contentId);
        if (!result.IsSuccess)
            return string.Empty;

        return result.Content;
    }

    /// <summary>
    /// Get logged in player account information
    /// </summary>
    public static async Task<Identity> GetLoginAccountInfo()
    {
        var result = await EaApi.GetIdentityMe();
        if (!result.IsSuccess)
            return null;

        return JsonHelper.JsonDeserialize<Identity>(result.Content);
    }

    /// <summary>
    /// Get player avatars in batches
    /// </summary>
    public static async Task<Avatars> GetAvatarByUserIds(List<string> userIds)
    {
        var result = await EaApi.GetAvatarByUserIds(userIds);
        if (!result.IsSuccess)
            return null;

        return JsonHelper.JsonDeserialize<Avatars>(result.Content);
    }

    /// <summary>
    /// Get the logged in player's friend list
    /// </summary>
    public static async Task<Friends> GetUserFriends()
    {
        var result = await EaApi.GetUserFriends();
        if (!result.IsSuccess)
            return null;

        return JsonHelper.JsonDeserialize<Friends>(result.Content);
    }

    /// <summary>
    /// Download player avatar and generate lsx response
    /// </summary>
    public static async Task<string> GetQueryImageXml(string id, string userid, string width, string imageid)
    {
        var savePath = string.Empty;
        string[] files = Directory.GetFiles(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Origin", "AvatarsCache"), $"{userid}.*");
        string link = string.Empty;
        if (files.Length > 0)
        {
            LoggerHelper.Info($"Found local player avatar image cache, skipping network download operation {files[0]}");
            savePath = files[0];
        }
        else
        {
            var result = await EaApi.GetAvatarByUserId(userid);
            if (!result.IsSuccess)
                return string.Empty;
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(result.Content);
            XmlNode linkNode = xmlDoc.SelectSingleNode("//link");
            link = linkNode.InnerText;
            string fileName = link.Substring(link.LastIndexOf('/') + 1);
            savePath = savePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Origin", "AvatarsCache", fileName.Replace("208x208", userid));
            if (!await CoreApi.DownloadWebImage(link, savePath))
            {
                LoggerHelper.Warn($"Failed to download avatar of currently logged in player {userid}");
            }
        }
        var doc = new XmlDocument();
        var lsx = doc.CreateElement("LSX");
        doc.AppendChild(lsx);

        var response = doc.CreateElement("Response");
        response.SetAttribute("id", id);
        response.SetAttribute("sender", "EbisuSDK");
        lsx.AppendChild(response);

        var queryImageResponse = doc.CreateElement("QueryImageResponse");
        queryImageResponse.SetAttribute("Result", "0");
        response.AppendChild(queryImageResponse);

        var image = doc.CreateElement("Image");
        image.SetAttribute("Width", width);
        image.SetAttribute("ImageId", imageid);
        image.SetAttribute("Height", width);
        image.SetAttribute("ResourcePath", savePath);
        queryImageResponse.AppendChild(image);

        return doc.InnerXml;
    }
}
