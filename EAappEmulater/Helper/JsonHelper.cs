namespace EAappEmulater.Helper;

public static class JsonHelper
{
    /// <summary>
    ///Deserialization configuration
    /// </summary>
    private static readonly JsonSerializerOptions OptionsDeserialize = new()
    {
        IncludeFields = true,
        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Serialization configuration
    /// </summary>
    private static readonly JsonSerializerOptions OptionsSerialize = new()
    {
        WriteIndented = true,
        IncludeFields = true,
        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
    };

    /// <summary>
    /// Deserialize, convert json string into json class
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="result"></param>
    /// <returns></returns>
    public static T JsonDeserialize<T>(string result)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(result, OptionsDeserialize);
        }
        catch (Exception ex)
        {
            LoggerHelper.Error("An exception occurred during deserialization", ex);
            return default;
        }
    }

    /// <summary>
    /// Serialization, convert json class into json string
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="jsonClass"></param>
    /// <returns></returns>
    public static string JsonSerialize<T>(T jsonClass)
    {
        try
        {
            return JsonSerializer.Serialize(jsonClass, OptionsSerialize);
        }
        catch (Exception ex)
        {
            LoggerHelper.Error("Serialization exception occurred", ex);
            return default;
        }
    }
}
