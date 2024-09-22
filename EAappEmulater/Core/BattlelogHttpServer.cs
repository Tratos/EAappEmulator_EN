using EAappEmulater.Enums;
using EAappEmulater.Helper;

namespace EAappEmulater.Core;

public static class BattlelogHttpServer
{
    private static HttpListener _httpListener = null;

    private static readonly Dictionary<string, string> _gameStatus = new();

    public static BattlelogType BattlelogType { get; set; }

    private static PipeServer _bf3PipeServer = null;
    private static PipeServer _bf4PipeServer = null;
    private static PipeServer _bfHPipeServer = null;

    private static bool _isBf3BattlelogGameStart = false;
    private static bool _isBf4BattlelogGameStart = false;
    private static bool _isBfHBattlelogGameStart = false;

    static BattlelogHttpServer()
    {
        BattlelogType = BattlelogType.None;

        _gameStatus["50182"] = FileHelper.GetEmbeddedResourceText($"Battlelog.50182.json");
        _gameStatus["000000"] = FileHelper.GetEmbeddedResourceText($"Battlelog.000000.json");
        _gameStatus["ping"] = FileHelper.GetEmbeddedResourceText($"Battlelog.ping.json");
        _gameStatus["2f4c24"] = FileHelper.GetEmbeddedResourceText($"Battlelog.2f4c24.json");
        _gameStatus["181931"] = FileHelper.GetEmbeddedResourceText($"Battlelog.181931.json");
        _gameStatus["76889"] = FileHelper.GetEmbeddedResourceText($"Battlelog.76889.json");
        _gameStatus["182288"] = FileHelper.GetEmbeddedResourceText($"Battlelog.182288.json");

        _gameStatus["status"] = FileHelper.GetEmbeddedResourceText($"Battlelog.status.json");
    }

    /// <summary>
    /// Start the Battlelog listening service
    /// </summary>
    public static void Run()
    {
        if (_httpListener is not null)
        {
            LoggerHelper.Warn("The Battlelog listening service is already running, please do not start it again.");
            return;
        }

        _httpListener = new HttpListener
        {
            AuthenticationSchemes = AuthenticationSchemes.Anonymous
        };

        _httpListener.Prefixes.Add("http://127.0.0.1:3215/");
        _httpListener.Prefixes.Add("http://127.0.0.1:4219/");
        _httpListener.Start();

        LoggerHelper.Info("Started Battlelog listening service successfully");
        LoggerHelper.Debug("The listening ports of the Battlelog service are 3215 and 4219");

        _httpListener.BeginGetContext(Result, null);

        /////////////////////////////////////////////////

        _bf3PipeServer = new PipeServer(BattlelogType.BF3);
        _bf4PipeServer = new PipeServer(BattlelogType.BF4);
        _bfHPipeServer = new PipeServer(BattlelogType.BFH);
    }

    /// <summary>
    /// Stop the Battlelog listening service
    /// </summary>
    public static void Stop()
    {
        _httpListener?.Stop();
        _httpListener = null;
        LoggerHelper.Info("Stopped Battlelog listening service successfully");

        /////////////////////////////////////////////////

        _bf3PipeServer?.Dispose();
        _bf3PipeServer = null;

        _bf4PipeServer?.Dispose();
        _bf4PipeServer = null;

        _bfHPipeServer?.Dispose();
        _bfHPipeServer = null;
    }

    /// <summary>
    /// Universal response output stream writing
    /// </summary>
    private static void WriteOutputStream(HttpListenerContext context, int code, bool isNeedHeader, string text)
    {
        context.Response.StatusCode = code;

        if (isNeedHeader)
            context.Response.Headers.Add("Access-Control-Allow-Origin", "https://battlelog.battlefield.com");

        var bytes = Encoding.UTF8.GetBytes(text);
        context.Response.OutputStream.Write(bytes, 0, bytes.Length);

        context.Response.Close();
    }

    /// <summary>
    /// Handle incoming requests
    /// </summary>
    private static void Result(IAsyncResult asyncResult)
    {
        try
        {
            // Avoid throwing exceptions when closing
            if (_httpListener is null)
                return;

            // Complete the asynchronous operation that retrieves the incoming client request
            var context = _httpListener.EndGetContext(asyncResult);
            // Start asynchronous retrieval of incoming request (next request)
            _httpListener.BeginGetContext(Result, null);

            // Handle GET request
            if (context.Request.HttpMethod == "GET")
            {
                if (context.Request.RawUrl != "/")
                    LoggerHelper.Debug($"Battlelog handles GET request URL {context.Request.Url}");

                // Handle 4219 port request
                if (context.Request.UserHostName == "127.0.0.1:4219")
                {
                    if (context.Request.RawUrl == "/killgame")
                    {
                        WriteOutputStream(context, 200, true, "");
                    }
                    else
                    {
                        switch (BattlelogType)
                        {
                            case BattlelogType.BF3:
                                {
                                    if (_bf3PipeServer.GameState == null)
                                    {
                                        // Pipe pipeline service no game state
                                        WriteOutputStream(context, 502, false, "null");
                                    }
                                    else if (_bf3PipeServer.GameState != null && ProcessHelper.IsAppRun("bf3") || ProcessHelper.IsAppRun("bf3debug"))
                                    {
                                        // Pipe pipeline service has game status, bf3.exe or bf3debug.exe is running
                                        var newState = _bf3PipeServer.GameState.Replace(" : S", "\tS").Replace(" ", "\t").Replace(":\tERR", "\tERR");
                                        WriteOutputStream(context, 200, true, $"VENICE-GAME\t{newState}");

                                        _isBf3BattlelogGameStart = true;
                                    }
                                    else if (!ProcessHelper.IsAppRun("bf3") || !ProcessHelper.IsAppRun("bf3debug") && _isBf3BattlelogGameStart == true)
                                    {
                                        // bf3.exe or bf3debug.exe is not running, Battlelog Game has started
                                        WriteOutputStream(context, 200, true, "VENICE-GAME\tStateChanged\tGAMEISGONE");

                                        BattlelogType = BattlelogType.None;
                                        _isBf3BattlelogGameStart = false;
                                        _bf3PipeServer.GameState = null;
                                    }
                                }
                                break;
                            case BattlelogType.BF4:
                                {
                                    if (_bf4PipeServer.GameState == null)
                                    {
                                        WriteOutputStream(context, 502, false, "null");
                                    }
                                    else if (_bf4PipeServer.GameState != null && ProcessHelper.IsAppRun("bf4") || ProcessHelper.IsAppRun("bf4debug"))
                                    {
                                        var newState = _bf4PipeServer.GameState.Replace(" : S", "\tS").Replace(" ", "\t").Replace(":\tERR", "\tERR");
                                        WriteOutputStream(context, 200, true, $"WARSAW-GAME\t{newState}");

                                        _isBf4BattlelogGameStart = true;
                                    }
                                    else if (!ProcessHelper.IsAppRun("bf4") || !ProcessHelper.IsAppRun("bf4debug") && _isBf4BattlelogGameStart == true)
                                    {
                                        WriteOutputStream(context, 200, true, "WARSAW-GAME\tStateChanged\tGAMEISGONE");

                                        BattlelogType = BattlelogType.None;
                                        _isBf4BattlelogGameStart = false;
                                        _bf4PipeServer.GameState = null;
                                    }
                                }
                                break;
                            case BattlelogType.BFH:
                                {
                                    if (_bfHPipeServer.GameState == null)
                                    {
                                        WriteOutputStream(context, 502, false, "null");
                                    }
                                    else if (_bfHPipeServer.GameState != null && ProcessHelper.IsAppRun("bfh") || ProcessHelper.IsAppRun("bfhdebug"))
                                    {
                                        var newState = _bfHPipeServer.GameState.Replace(" : S", "\tS").Replace(" ", "\t").Replace(":\tERR", "\tERR");
                                        WriteOutputStream(context, 200, true, $"OMAHA-MAINLINE-GAME\t{newState}");

                                        _isBfHBattlelogGameStart = true;
                                    }
                                    else if (!ProcessHelper.IsAppRun("bfh") || !ProcessHelper.IsAppRun("bfhdebug") && _isBfHBattlelogGameStart == true)
                                    {
                                        WriteOutputStream(context, 200, true, "OMAHA-MAINLINE-GAME\tStateChanged\tGAMEISGONE");

                                        BattlelogType = BattlelogType.None;
                                        _isBfHBattlelogGameStart = false;
                                        _bfHPipeServer.GameState = null;
                                    }
                                }
                                break;
                        }
                    }
                }
                else
                {
                    switch (context.Request.RawUrl)
                    {
                        case "/game/status?masterTitleId=50182":
                            LoggerHelper.Info("Battlelog Get Battlefield 3 installation status");
                            WriteOutputStream(context, 200, true, _gameStatus["50182"]);
                            break;
                        case "/game/status?masterTitleId=000000":
                            WriteOutputStream(context, 404, true, _gameStatus["000000"]);
                            break;
                        case "/ping":
                            WriteOutputStream(context, 200, true, _gameStatus["ping"]);
                            break;
                        case "/game/launch/status/cee4e0c885634dc2bfcb7ee88e2f4c24":
                            LoggerHelper.Info("Battlelog Get status");
                            WriteOutputStream(context, 200, true, _gameStatus["2f4c24"]);
                            break;
                        case "/game/status?masterTitleId=181931":
                            LoggerHelper.Info("Battlelog Get Medal of Honor Warfighter installation status");
                            WriteOutputStream(context, 200, true, _gameStatus["181931"]);
                            break;
                        case "/game/status?masterTitleId=76889":
                            LoggerHelper.Info("Battlelog Get Battlefield 4 installation status");
                            WriteOutputStream(context, 200, true, _gameStatus["76889"]);
                            break;
                        case "/game/status?masterTitleId=182288":
                            LoggerHelper.Info("Battlelog Gets Battlefield installation status");
                            WriteOutputStream(context, 200, true, _gameStatus["182288"]);
                            break;
                        default:
                            context.Response.StatusCode = 200;
                            context.Response.Close();
                            break;
                    }
                }

                // Avoid too much nesting of if-else
                return;
            }

            // Handle POST request
            if (context.Request.HttpMethod == "POST")
            {
                if (context.Request.RawUrl != "/")
                    LoggerHelper.Debug($"Battlelog handles POST request URLs {context.Request.Url}");

                var nameValCol = context.Request.QueryString;
                var cmdParams = nameValCol["cmdParams"];
                var ipAddrList = nameValCol["ipAddrList"];

                if (!string.IsNullOrWhiteSpace(ipAddrList))
                {
                    var ipArray = ipAddrList.Split(',');

                    var strBuilder = new StringBuilder();
                    foreach (var ip in ipArray)
                    {
                        strBuilder.Append($"{{\"ip\":\"{ip}\",\"time\":1}},");
                    }

                    LoggerHelper.Info("Battlelog Returns Ping Information");
                    var responseStr = $"[{strBuilder}]";
                    WriteOutputStream(context, 200, true, responseStr.Replace("},]", "}]"));
                }
                else if (nameValCol["offerIds"] == "DR:224766400")
                {
                    LoggerHelper.Info("Battlelog ready to launch Battlefield 3");
                    BattlelogType = BattlelogType.BF3;

                    Game.RunGame(GameType.BF3, cmdParams, false);
                    WriteOutputStream(context, 202, true, _gameStatus["status"]);
                }
                else if (nameValCol["offerIds"] == "OFB-EAST:109552316@subscription")
                {
                    LoggerHelper.Info("Battlelog ready to launch Battlefield 4");

                    Game.RunGame(GameType.BF4, cmdParams, false);
                    WriteOutputStream(context, 202, true, _gameStatus["status"]);
                }
                else if (nameValCol["offerIds"] == "Origin.OFR.50.0000846@subscription")
                {
                    LoggerHelper.Info("Battlelog is ready to start a tough battle in the battlefield");

                    Game.RunGame(GameType.BFH, cmdParams, false);
                    WriteOutputStream(context, 202, true, _gameStatus["status"]);
                }
            }
        }
        catch (Exception ex)
        {
            LoggerHelper.Error("Handle Battlelog client connection exception", ex);
        }
    }
}
