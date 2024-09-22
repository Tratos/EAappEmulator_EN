using EAappEmulater.Api;
using EAappEmulater.Enums;
using EAappEmulater.Helper;

namespace EAappEmulater.Core;

public static class LSXTcpServer
{
    private static TcpListener _tcpServer = null;

    private static readonly List<string> ScoketMsgBFV = new();
    private static readonly List<string> ScoketMsgBFH = new();
    private static readonly List<string> ScoketMsgTTF2 = new();

    static LSXTcpServer()
    {
        //Load XML string
        for (int i = 0; i <= 25; i++)
        {
            var text = FileHelper.GetEmbeddedResourceText($"LSX.BFV.{i:D2}.xml");

            // Avatar \AppData\Local\Origin\AvatarsCache (not sure why it is not displayed)
            text = text.Replace("##AvatarId##", Account.Avatar);

            ScoketMsgBFV.Add(text);
        }

        // The terminator must be added here
        ScoketMsgBFV[0] = string.Concat(ScoketMsgBFV[0], "\0");
        ScoketMsgBFV[1] = string.Concat(ScoketMsgBFV[1], "\0");

        //////////////////////////////////////////

        for (int i = 0; i <= 16; i++)
        {
            var text = FileHelper.GetEmbeddedResourceText($"LSX.BFH.{i:D2}.xml");
            ScoketMsgBFH.Add(text);
        }

        // The terminator must be added here
        ScoketMsgBFH[0] = string.Concat(ScoketMsgBFH[0], "\0");
        ScoketMsgBFH[1] = string.Concat(ScoketMsgBFH[1], "\0");

        //////////////////////////////////////////

        for (int i = 0; i <= 0; i++)
        {
            var text = FileHelper.GetEmbeddedResourceText($"LSX.TTF2.{i:D2}.xml");
            ScoketMsgTTF2.Add(text);
        }
    }

    /// <summary>
    /// Start TCP listening service
    /// </summary>
    public static void Run()
    {
        if (_tcpServer is not null)
        {
            LoggerHelper.Warn("The LSX listening service is already running, please do not start it again.");
            return;
        }

        _tcpServer = new TcpListener(IPAddress.Parse("127.0.0.1"), 3216);
        _tcpServer.Start();

        LoggerHelper.Info("Started LSX listening service successfully");
        LoggerHelper.Debug("The LSX service listening port is 3216");

        _tcpServer.BeginAcceptTcpClient(Result, null);
    }

    /// <summary>
    /// Stop TCP listening service
    /// </summary>
    public static void Stop()
    {
        _tcpServer?.Stop();
        _tcpServer = null;
        LoggerHelper.Info("Stopped LSX listening service successfully");
    }

    /// <summary>
    /// Get the player list Xml string
    /// </summary>
    private static string GetFriendsXmlString()
    {
        if (Globals.IsGetFriendsSuccess)
            return Globals.FriendsXmlString;

        return ScoketMsgBFV[11];
    }
    /// <summary>
    /// Get friends list xml
    /// </summary>
    private static string QueryPresenceResponse()
    {
        if (Globals.IsGetFriendsSuccess)
            return Globals.QueryPresenceString;

        return ScoketMsgBFV[13];
    }

    /// <summary>
    /// Handle TCP client connection
    /// </summary>
    private static async void Result(IAsyncResult asyncResult)
    {
        // Avoid throwing exceptions when the service is shut down
        if (_tcpServer is null)
            return;

        // Complete the asynchronous operation that retrieves the incoming client request
        var client = _tcpServer.EndAcceptTcpClient(asyncResult);
        // Start asynchronous retrieval of incoming request (next request)
        _tcpServer.BeginAcceptTcpClient(Result, null);

        //Save the client connection IP and address
        var clientIp = string.Empty;

        try
        {
            //If the connection is disconnected, end
            if (!client.Connected)
                return;

            clientIp = client.Client.RemoteEndPoint.ToString();
            LoggerHelper.Debug($"Discover TCP client connections {clientIp}");

            /////////////////////////////////////////////////

            // Establish and connect the client's data stream (transmit data)
            var networkStream = client.GetStream();
            //Set the read and write timeout to 3600 seconds
            networkStream.ReadTimeout = 3600000;
            networkStream.WriteTimeout = 3600000;

            var startKey = "cacf897a20b6d612ad0c05e011df52bb";
            var buffer = Encoding.UTF8.GetBytes(ScoketMsgBFV[0].Replace("##KEY##", startKey));

            //Asynchronously write to network stream
            await networkStream.WriteAsync(buffer);

            var tcpString = await ReadTcpString(client, networkStream);
            var partArray = tcpString.Split('\"');

            // Adapt to FC24
            var doc = XDocument.Parse(tcpString.Replace("version=\"\"", "version1=\"\""));
            var request = doc.Element("LSX").Element("Request");
            var contentId = request.Element("ChallengeResponse").Element("ContentId").Value;

            var response = string.Empty;
            var key = string.Empty;

            LoggerHelper.Debug($"Current BattlelogType {BattlelogHttpServer.BattlelogType}");

            // Process the Battlelog game (default represents other games)
            // Battlefield Hardline is different from the lsx request in bf4debug mode
            switch (BattlelogHttpServer.BattlelogType)
            {
                case BattlelogType.BFH:
                case BattlelogType.BF4Debug:
                    response = partArray[7];
                    key = partArray[9];
                    break;
                case BattlelogType.BF4:
                default:
                    response = partArray[5];
                    key = partArray[7];
                    break;
            }

            LoggerHelper.Debug($"The Challenge Response for this startup is {response}");
            LoggerHelper.Info($"The ContentId of this startup is {contentId}");
            LoggerHelper.Info("Getting ready to start the game...");

            // Check Challenge response
            if (!EaCrypto.CheckChallengeResponse(response, startKey))
            {
                LoggerHelper.Fatal("Challenge Response Fatal error!");
                return;
            }

            // Handle decryption Challenge response
            var newResponse = EaCrypto.MakeChallengeResponse(key);
            LoggerHelper.Debug($"The Challenge Response for this startup is {newResponse}");

            var seed = (ushort)((newResponse[0] << 8) | newResponse[1]);
            LoggerHelper.Debug($"Processing Decryption Challenge Response Seed {newResponse}");

            // handle the request
            buffer = Encoding.UTF8.GetBytes(ScoketMsgBFH[1].Replace("##RESPONSE##", newResponse).Replace("##ID##", partArray[3]));

            //Asynchronously write to network stream
            await networkStream.WriteAsync(buffer);

            // Pay attention to the infinite loop here
            // Only run when client is connected
            while (client.Connected)
            {
                try
                {
                    switch (BattlelogHttpServer.BattlelogType)
                    {
                        case BattlelogType.BFH:
                            {
                                var data = await ReadTcpString(client, networkStream);
                                data = EaCrypto.LSXDecryptBFH(data);

                                data = await LSXRequestHandleForBFH(data);
                                LoggerHelper.Debug($"current {BattlelogHttpServer.BattlelogType} LSX Reply {data}");

                                data = EaCrypto.LSXEncryptBFH(data);
                                await WriteTcpString(client, networkStream, $"{data}\0");
                            }
                            break;
                        default:
                            {
                                var data = await ReadTcpString(client, networkStream);
                                data = EaCrypto.LSXDecryptBF4(data, seed);

                                data = await LSXRequestHandleForBFV(data, contentId);
                                LoggerHelper.Debug($"current {BattlelogHttpServer.BattlelogType} LSX Reply {data}");

                                data = EaCrypto.LSXEncryptBF4(data, seed);
                                await WriteTcpString(client, networkStream, $"{data}\0");
                            }
                            break;
                    }
                }
                catch (TimeoutException ex)
                {
                    LoggerHelper.Error("Handling TCP Battlelog client connection timeout exception", ex);
                }
            }
        }
        catch (Exception ex)
        {
            LoggerHelper.Error("Handle TCP client connection exception", ex);
        }
        finally
        {
            client.Close();
            LoggerHelper.Debug($"TCP client connection processing ends {clientIp}");
        }
    }

    /// <summary>
    /// Asynchronously read TCP network stream string
    /// </summary>
    private static async Task<string> ReadTcpString(TcpClient client, NetworkStream stream)
    {
        // If the client connection is disconnected, return an empty string
        if (!client.Connected)
            return string.Empty;

                /**
                 * Simulate NetworkStream.ReadByte() asynchronously
                 * This is the only way to fix the stupid rebirth game, rebirth your mother is dead
                 */

        var strBuilder = new StringBuilder();
        var buffer = new byte[1];       // single byte buffer
        int readLength;                 // actual read length

        try
        {
            // Execute only when the read length is greater than 0
            // This exception will occur when the game is closed (the remote host forcibly closed an existing connection)
            while ((readLength = await stream.ReadAsync(buffer)) > 0)
            {
                var b = buffer[0];
                if (b == 0)             // terminator
                    break;

                strBuilder.Append((char)b);
            }
        }
        catch (Exception ex)
        {
            //Exception handling
            LoggerHelper.Error("Exception while reading TCP string asynchronously", ex);
        }

        return strBuilder.ToString();
    }

    /// <summary>
    /// Asynchronously write TCP network stream string
    /// </summary>
    private static async Task WriteTcpString(TcpClient client, NetworkStream stream, string tcpStr)
    {
        //If the client connection is disconnected, end
        if (!client.Connected)
            return;

        //Do not use try catch to catch exceptions
        // Mainly to avoid infinite execution of the infinite loop (use exceptions to interrupt the infinite loop)
        var buffer = Encoding.UTF8.GetBytes(tcpStr);
        await stream.WriteAsync(buffer);
    }

    /// <summary>
    /// Handle BFV LSX requests
    /// </summary>
    private static async Task<string> LSXRequestHandleForBFV(string request, string contentId)
    {
        if (string.IsNullOrWhiteSpace(request))
            return string.Empty;

        LoggerHelper.Debug($"BFV LSX Request {request}");

        var partArray = request.Split('\"');
        LoggerHelper.Debug($"BFV LSX request partArray length {partArray.Length}");

        var id = partArray[3];
        var requestType = partArray[4];

        LoggerHelper.Debug($"BFV LSX Request ID {id}");
        LoggerHelper.Debug($"BFV LSX request RequestType {requestType}");

        return requestType switch
        {
            "><GetConfig version=" => ScoketMsgBFV[2].Replace("##ID##", id),
            "><GetAuthCode ClientId=" => ScoketMsgBFV[3].Replace("##ID##", id).Replace("##AuthCode##", await EasyEaApi.GetLSXAutuCode(partArray[5])),
            "><GetAuthCode UserId=" => ScoketMsgBFV[3].Replace("##ID##", id).Replace("##AuthCode##", await EasyEaApi.GetLSXAutuCode(partArray[7])),
            "><GetBlockList version=" => ScoketMsgBFV[4].Replace("##ID##", id),
            "><GetGameInfo GameInfoId=" => partArray[5] switch
            {
                "FREETRIAL" => ScoketMsgBFV[19].Replace("##ID##", id),
                "UPTODATE" => ScoketMsgBFV[20].Replace("##ID##", id).Replace("##Locale##", "true"),
                "INSTALLED_LANGUAGE" => ScoketMsgBFV[20].Replace("##ID##", id).Replace("##Locale##", RegistryHelper.GetLocaleByContentId(contentId)),
                _ => ScoketMsgBFV[5].Replace("##ID##", id),
            },
            "><GetInternetConnectedState version=" => ScoketMsgBFV[6].Replace("##ID##", id),
            "><GetPresence UserId=" => ScoketMsgBFV[7].Replace("##ID##", id),
            "><GetProfile index=" => ScoketMsgBFV[8].Replace("##ID##", id).Replace("##PID##", Account.PersonaId).Replace("##DSNM##", Account.PlayerName).Replace("##UID##", Account.UserId),
            "><RequestLicense UserId=" => ScoketMsgBFV[15].Replace("##ID##", id).Replace("##License##", await EasyEaApi.GetLSXLicense(partArray[7], contentId)),
            "><GetSetting SettingId=" => partArray[5] switch
            {
                "ENVIRONMENT" => ScoketMsgBFV[9].Replace("##ID##", id),
                "IS_IGO_AVAILABLE" => ScoketMsgBFV[10].Replace("##ID##", id),
                "IS_IGO_ENABLED" => ScoketMsgBFV[10].Replace("##ID##", id),
                _ => string.Empty,
            },
            "><QueryFriends UserId=" => GetFriendsXmlString().Replace("##ID##", id),
            "><QueryImage ImageId=" => await EasyEaApi.GetQueryImageXml(id, partArray[5].Replace("user:", ""), partArray[7], partArray[5]),
            "><QueryPresence UserId=" => QueryPresenceResponse().Replace("##ID##", id).Replace("##UID##", Account.UserId),
            "><SetPresence UserId=" => ScoketMsgBFV[14].Replace("##ID##", id),
            "><GetAllGameInfo version=" => contentId switch
            {
                "1039093" => ScoketMsgTTF2[0].Replace("##ID##", id).Replace("##SystemTime##", $"{DateTime.Now:s}").Replace("##Locale##", RegistryHelper.GetLocaleByContentId(contentId)).Replace("##Version##", "1.0.1.3"),
                "16115019" => ScoketMsgTTF2[0].Replace("##ID##", id).Replace("##SystemTime##", $"{DateTime.Now:s}").Replace("##Locale##", RegistryHelper.GetLocaleByContentId(contentId)).Replace("##Version##", "1.0.83.40087"),
                "198235" => ScoketMsgTTF2[0].Replace("##ID##", id).Replace("##SystemTime##", $"{DateTime.Now:s}").Replace("##Locale##", RegistryHelper.GetLocaleByContentId(contentId)).Replace("##Version##", "1.0.87.30122"),
                "16425635" => ScoketMsgTTF2[0].Replace("##ID##", id).Replace("##SystemTime##", $"{DateTime.Now:s}").Replace("##Locale##", RegistryHelper.GetLocaleByContentId(contentId)).Replace("##Version##", "1.0.87.30122"),
                _ => ScoketMsgTTF2[0].Replace("##ID##", id).Replace("##SystemTime##", $"{DateTime.Now:s}").Replace("##Locale##", RegistryHelper.GetLocaleByContentId(contentId)).Replace("##Version##", "1.0.108.2038")
            },
            "><IsProgressiveInstallationAvailable ItemId=" => ScoketMsgBFV[17].Replace("##ID##", id).Replace("Origin.OFR.50.0004342", "Origin.OFR.50.0001455"),
            "><QueryContent UserId=" => ScoketMsgBFV[18].Replace("##ID##", id),
            "><QueryEntitlements UserId=" => ScoketMsgBFV[21].Replace("##ID##", id),
            "><QueryOffers UserId=" => ScoketMsgBFV[22].Replace("##ID##", id),
            "><SetDownloaderUtilization Utilization=" => ScoketMsgBFV[23].Replace("##ID##", id),
            "><QueryChunkStatus ItemId=" => ScoketMsgBFV[24].Replace("##ID##", id),
            "><GetPresenceVisibility UserId=" => ScoketMsgBFV[25].Replace("##ID##", id),
            _ => string.Empty,
        };
    }

    /// <summary>
    /// Handle BFH LSX requests
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    private static async Task<string> LSXRequestHandleForBFH(string request)
    {
        if (string.IsNullOrWhiteSpace(request))
            return string.Empty;

        LoggerHelper.Debug($"BFH LSX 请求 Request {request}");

        var partArray = request.Split('\"');
        LoggerHelper.Debug($"BFH LSX 请求 partArray 长度 {partArray.Length}");

        var id = partArray[3];
        var requestType = partArray[4];

        LoggerHelper.Debug($"BFH LSX 请求 Id {id}");
        LoggerHelper.Debug($"BFH LSX 请求 RequestType {requestType}");

        return requestType switch
        {
            "><GetConfig version=" => ScoketMsgBFH[2].Replace("##ID##", id),
            "><GetAuthCode version=" => ScoketMsgBFH[3].Replace("##ID##", id).Replace("##AuthCode##", await EasyEaApi.GetLSXAutuCode(partArray[7])),
            "><GetAuthCode UserId=" => ScoketMsgBFV[3].Replace("##ID##", id).Replace("##AuthCode##", await EasyEaApi.GetLSXAutuCode(partArray[7])),
            "><GetBlockList version=" => ScoketMsgBFH[4].Replace("##ID##", id),
            "><GetGameInfo version=" => ScoketMsgBFH[5].Replace("##ID##", id),
            "><GetInternetConnectedState version=" => ScoketMsgBFH[6].Replace("##ID##", id),
            "><GetPresence version=" => ScoketMsgBFH[7].Replace("##ID##", id),
            "><GetProfile version=" => ScoketMsgBFH[8].Replace("##ID##", id).Replace("##PID##", Account.PersonaId).Replace("##DSNM##", Account.PlayerName).Replace("##UID##", Account.UserId),
            "><RequestLicense UserId=" => ScoketMsgBFH[15].Replace("##ID##", id),
            "><GetSetting version=" => partArray[7] switch
            {
                "ENVIRONMENT" => ScoketMsgBFH[9].Replace("##ID##", id),
                "IS_IGO_AVAILABLE" => ScoketMsgBFH[10].Replace("##ID##", id),
                " SettingId=" => ScoketMsgBFH[10].Replace("##ID##", id),
                _ => string.Empty,
            },
            "><QueryFriends UserId=" => ScoketMsgBFH[11].Replace("##ID##", id),
            "><QueryImage ImageId=" => ScoketMsgBFH[12].Replace("##ID##", id),
            "><QueryPresence UserId=" => ScoketMsgBFH[13].Replace("##ID##", id),
            "><SetPresence version=" => ScoketMsgBFH[14].Replace("##ID##", id),
            "><GetAuthToken version=" => ScoketMsgBFH[16].Replace("##ID##", id).Replace("##AuthCode##", await EasyEaApi.GetLSXAutuCode("GOS-BlazeServer-HAVANA-PC")),
            //"><QueryFriends version=" => GetFriendsXmlString().Replace("##ID##", id),
            _ => string.Empty,
        };
    }
}
