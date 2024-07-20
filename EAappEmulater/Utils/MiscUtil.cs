namespace EAappEmulater.Utils;

public static class MiscUtil
{
    private static readonly List<string> _gameNameList = new();
    private static readonly List<string> _gameModeList = new();

    static MiscUtil()
    {
        _gameNameList.Add("Battlefield 3");
        _gameNameList.Add("Battlefield 4");
        _gameNameList.Add("Battlefield Hardline");
        _gameNameList.Add("Battlefield 1");
        _gameNameList.Add("Battlefield V");
        _gameNameList.Add("Battlefield 2042");

        _gameModeList.Add("conquer");
        _gameModeList.Add("raid");
        _gameModeList.Add("action");
        _gameModeList.Add("front");
        _gameModeList.Add("fight to the death");
    }

    private static string GetRandomGameName()
    {
        var random = new Random();
        var index = random.Next(_gameNameList.Count - 1);
        return _gameNameList[index];
    }

    private static string GetRandomGamMode()
    {
        var random = new Random();
        var index = random.Next(_gameModeList.Count - 1);
        return _gameModeList[index];
    }

    public static string GetRandomFriendTitle()
    {
        return $"Playing now《{GetRandomGameName()}》{GetRandomGamMode()}model...";
    }
}
