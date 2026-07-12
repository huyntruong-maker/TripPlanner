using Newtonsoft.Json;

namespace Domain.Helpers;

public static class JsonHelper
{
    public static string ToJson<T>(this T? obj)
        where T : class
    {
        return JsonConvert.SerializeObject(obj);
    }

    public static T? ToObject<T>(this string json)
        where T : class
    {
        return !string.IsNullOrEmpty(json)
            ? JsonConvert.DeserializeObject<T>(json)
            : null;
    }

    public static T? Copy<T>(this T? obj)
        where T : class
    {
        return obj?.ToJson().ToObject<T>();
    }

    public static bool IsEqual(this object source, object toCompare)
    {
        return source.ToJson().Equals(toCompare.ToJson());
    }
}