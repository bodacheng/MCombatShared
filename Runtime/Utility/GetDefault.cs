using System.Collections.Generic;

public static class GetDefault
{
    public static TValue GetOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue = default)
    {
        return dictionary != null && dictionary.TryGetValue(key, out var value) ? value : defaultValue;
    }
}
