namespace Lib;

public static class DictionaryExtension
{
    public static void AddOrCreate<TKey, TValue, TList>(this Dictionary<TKey, TList> e, TKey key, TValue dec)
        where TKey : notnull where TList : IList<TValue>, new()
    {
        if (e.TryGetValue(key, out var v))
            v.Add(dec);
        else
            e[key] = [dec];
    }
}