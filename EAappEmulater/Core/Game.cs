using EAappEmulater.Enums;
using EAappEmulater.Helper;

namespace EAappEmulater.Core;

public static class Game
{
    /// <summary>
    /// Get the system environment variable collection
    /// </summary>
    private static Dictionary<string, string> GetEnvironmentVariables()
    {
        var environmentVariables = new Dictionary<string, string>();
        foreach (DictionaryEntry dirEnity in Environment.GetEnvironmentVariables())
        {
            environmentVariables.Add(dirEnity.Key.ToString(), dirEnity.Value.ToString());
        }
        return environmentVariables;
    }

    /// <summary>
    /// Start the game
    /// </summary>
    public static void RunGame(GameType gameType, string webArgs = "", bool isNotice = true)
    {
        try
        {
            var gameInfo = Base.GameInfoDb[gameType];

            ////////////////////////////////////////////////////////

            var execPath = string.Empty;        // Registry path
            var execPath2 = string.Empty;       //Customize startup path

            // Handle the special startup path of two people in a row
            if (gameInfo.GameType is GameType.ITT)
            {
                // Two people make a trip
                execPath = Path.Combine(gameInfo.Dir, "Nuts\\Binaries\\Win64", gameInfo.AppName);
                execPath2 = Path.Combine(gameInfo.Dir2, "Nuts\\Binaries\\Win64", gameInfo.AppName);
            }
            else if (gameInfo.GameType is GameType.SWJFO)
            {
                execPath = Path.Combine(gameInfo.Dir, "SwGame\\Binaries\\Win64", gameInfo.AppName);
                execPath2 = Path.Combine(gameInfo.Dir2, "SwGame\\Binaries\\Win64", gameInfo.AppName);
            }
            else
            {
                // other
                execPath = Path.Combine(gameInfo.Dir, gameInfo.AppName);
                execPath2 = Path.Combine(gameInfo.Dir2, gameInfo.AppName);
            }

            // Determine whether to use a custom path to start the game
            if (gameInfo.IsUseCustom)
            {
                // Custom game path

                // Determine the game path
                if (string.IsNullOrWhiteSpace(gameInfo.Dir2))
                {
                    LoggerHelper.Warn($"{gameType} The game path is empty, starting the game and terminating it {gameInfo.Dir}");
                    if (isNotice)
                        NotifierHelper.Warning($"{gameType} The game path is empty, starting the game and terminating it");

                    return;
                }

                // Determine game files
                if (!File.Exists(execPath2))
                {
                    LoggerHelper.Warn($"{gameType} The main program file of the game does not exist, and the game is terminated when starting it. {execPath2}");
                    if (isNotice)
                        NotifierHelper.Warning($"{gameType} The main program file of the game does not exist, and the game is terminated when starting it.");

                    return;
                }
            }
            else
            {
                // Registry game path

                // Determine the game path
                if (string.IsNullOrWhiteSpace(gameInfo.Dir))
                {
                    LoggerHelper.Warn($"{gameType} The game path is empty, starting the game and terminating it {gameInfo.Dir}");
                    if (isNotice)
                        NotifierHelper.Warning($"{gameType} The game path is empty, starting the game and terminating it");

                    return;
                }

                // Determine game files
                if (!File.Exists(execPath))
                {
                    LoggerHelper.Warn($"{gameType} The main program file of the game does not exist, and the game is terminated when starting it.{execPath}");
                    if (isNotice)
                        NotifierHelper.Warning($"{gameType} The main program file of the game does not exist, and the game is terminated when starting it.");

                    return;
                }
            }

            ////////////////////////////////////////////////////////

            if (string.IsNullOrWhiteSpace(Account.OriginPCToken))
            {
                LoggerHelper.Warn($"{gameType} OriginPCToken is empty and the game is terminated when it is started.");
                if (isNotice)
                    NotifierHelper.Warning($"{gameType} OriginPCToken is empty and the game is terminated when it is started.");

                return;
            }

            ////////////////////////////////////////////////////////

            // Handle old LSX
            if (gameInfo.IsOldLSX)
                BattlelogHttpServer.BattlelogType = BattlelogType.BFH;
            else
                BattlelogHttpServer.BattlelogType = gameType switch
                {
                    GameType.BF3 => BattlelogType.BF3,
                    GameType.BF4 => BattlelogType.BF4,
                    GameType.BFH => BattlelogType.BFH,
                    _ => BattlelogType.None,
                };

            LoggerHelper.Info($"{gameInfo.Name} Starting the game...");
            if (isNotice)
                NotifierHelper.Notice($"{gameInfo.Name} Starting the game...");

            // Get all environment variable names and values ​​of the current process
            var environmentVariables = GetEnvironmentVariables();

            //Assign directly to the dictionary. If the key already exists, update the corresponding value, otherwise create it.
            environmentVariables["EAFreeTrialGame"] = "false";
            environmentVariables["EAAuthCode"] = Account.OriginPCToken;
            environmentVariables["EALaunchOfflineMode"] = "false";
            environmentVariables["OriginSessionKey"] = "7102090b-ea9a-4531-9598-b2a7e943b544";
            environmentVariables["EAGameLocale"] = "en_US";
            environmentVariables["EALaunchEnv"] = "production";
            environmentVariables["EALaunchEAID"] = Account.PlayerName;
            environmentVariables["EALicenseToken"] = "114514";
            environmentVariables["EAEntitlementSource"] = "EA";
            environmentVariables["EAUseIGOAPI"] = "1";
            environmentVariables["EALaunchUserAuthToken"] = Account.OriginPCToken;
            environmentVariables["EAGenericAuthToken"] = Account.OriginPCToken;
            environmentVariables["EALaunchCode"] = "unavailable";
            environmentVariables["EARtPLaunchCode"] = EaCrypto.GetRTPHandshakeCode();
            environmentVariables["EALsxPort"] = "3216";
            environmentVariables["EAEgsProxyIpcPort"] = "1705";
            environmentVariables["EASteamProxyIpcPort"] = "1704";
            environmentVariables["EAExternalSource"] = "EA";
            environmentVariables["EASecureLaunchTokenTemp"] = "1001006949032";
            environmentVariables["SteamAppId"] = "";
            environmentVariables["ContentId"] = gameInfo.ContentId;
            environmentVariables["EAConnectionId"] = gameInfo.ContentId;

            //Fixed the problem that Titanfall 2 could not connect to the data center, and the idiot was reborn.
            if (gameInfo.GameType is GameType.TTF2)
                environmentVariables["OPENSSL_ia32cap"] = "~0x200000200000000";

            //Initialize process class instance
            var startInfo = new ProcessStartInfo
            {
                UseShellExecute = false
            };

            // Determine whether to use a custom path to start the game
            if (gameInfo.IsUseCustom)
            {
                // Custom game path

                if (gameInfo.GameType is GameType.ITT)
                {
                    // Two people make a trip
                    startInfo.FileName = Path.Combine(gameInfo.Dir2, "Nuts\\Binaries\\Win64", gameInfo.AppName);
                    startInfo.WorkingDirectory = Path.Combine(gameInfo.Dir2, "Nuts\\Binaries\\Win64", gameInfo.AppName);
                }
                else if (gameInfo.GameType is GameType.SWJFO)
                {
                    // Star Wars Jedi: Fallen Order
                    startInfo.FileName = Path.Combine(gameInfo.Dir2, "SwGame\\Binaries\\Win64", gameInfo.AppName);
                    startInfo.WorkingDirectory = Path.Combine(gameInfo.Dir2, "SwGame\\Binaries\\Win64", gameInfo.AppName);
                }
                else
                {
                    // other
                    startInfo.FileName = Path.Combine(gameInfo.Dir2, gameInfo.AppName);
                    startInfo.WorkingDirectory = gameInfo.Dir2;
                }

                //Startup parameters
                startInfo.Arguments = string.Concat(webArgs, " ", gameInfo.Args2).Trim();
            }
            else
            {
                // Registry game path

                if (gameInfo.GameType is GameType.ITT)
                {
                    // Two people make a trip
                    startInfo.FileName = Path.Combine(gameInfo.Dir, "Nuts\\Binaries\\Win64", gameInfo.AppName);
                    startInfo.WorkingDirectory = Path.Combine(gameInfo.Dir, "Nuts\\Binaries\\Win64");
                }
                else if (gameInfo.GameType is GameType.SWJFO)
                {
                    // Star Wars Jedi: Fallen Order
                    startInfo.FileName = Path.Combine(gameInfo.Dir, "SwGame\\Binaries\\Win64", gameInfo.AppName);
                    startInfo.WorkingDirectory = Path.Combine(gameInfo.Dir, "SwGame\\Binaries\\Win64");
                }
                else
                {
                    // other
                    startInfo.FileName = Path.Combine(gameInfo.Dir, gameInfo.AppName);
                    startInfo.WorkingDirectory = gameInfo.Dir;
                }

                //Startup parameters
                startInfo.Arguments = string.Concat(webArgs, " ", gameInfo.Args).Trim();
            }

            // Set process startup environment variables in batches
            foreach (var variable in environmentVariables)
            {
                startInfo.EnvironmentVariables[variable.Key] = variable.Value;
            }

            //Start program
            Process.Start(startInfo);

            LoggerHelper.Info($"Start the game {gameInfo.Name} success");
            if (isNotice)
                NotifierHelper.Success($"Start the game {gameInfo.Name} success");
        }
        catch (Exception ex)
        {
            LoggerHelper.Error($"An exception occurred when starting the game {gameType}", ex);
            if (isNotice)
                NotifierHelper.Error($"An exception occurred when starting the game {gameType} Please see the log for details");
        }
    }
}
