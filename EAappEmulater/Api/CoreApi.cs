using EAappEmulater.Helper;
using RestSharp;

namespace EAappEmulater.Api;

public static class CoreApi
{
    private static readonly RestClient _client;

    static CoreApi()
    {
        var options = new RestClientOptions()
        {
            Timeout = TimeSpan.FromSeconds(20),
            FollowRedirects = false,
            ThrowOnAnyError = false,
            ThrowOnDeserializationError = false
        };

        _client = new RestClient(options);
    }

    public static async Task<Version> GetWebUpdateVersion()
    {
        try
        {
            var request = new RestRequest("https://api.github.com/repos/Tratos/EAappEmulator_EN/releases", Method.Get);

            var response = await _client.ExecuteAsync(request);
            LoggerHelper.Info($"GetWebUpdateVersion request ended, status {response.ResponseStatus}");
            LoggerHelper.Info($"GetWebUpdateVersion request ends, status code {response.StatusCode}");

            if (response.ResponseStatus == ResponseStatus.TimedOut)
            {
                LoggerHelper.Info($"GetWebUpdateVersion request timed out");
                return null;
            }

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var jsonNode = JsonNode.Parse(response.Content);

                var tagName = jsonNode["tag_name"].GetValue<string>();
                if (Version.TryParse(tagName, out Version version))
                {
                    LoggerHelper.Info($"Obtained server update version number successfully {version}");
                    return version;
                }
            }

            LoggerHelper.Warn($"Failed to obtain server update version number {response.Content}");
            return null;
        }
        catch (Exception ex)
        {
            LoggerHelper.Error("An exception occurred while obtaining the server update version number.", ex);
            return null;
        }
    }

    /// <summary>
    /// Download Internet pictures
    /// </summary>
    public static async Task<bool> DownloadWebImage(string imgUrl, string savePath)
    {
        try
        {
            var request = new RestRequest(imgUrl, Method.Get);

            var bytes = await _client.DownloadDataAsync(request);
            if (bytes == null || bytes.Length == 0)
            {
                LoggerHelper.Warn($"Failed to download network pictures {imgUrl}");
                return false;
            }

            await File.WriteAllBytesAsync(savePath, bytes);
            return true;
        }
        catch (Exception ex)
        {
            LoggerHelper.Error($"An exception occurred while downloading network images. {imgUrl}", ex);
            return false;
        }
    }
}
