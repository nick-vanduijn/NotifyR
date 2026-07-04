using System.Collections.Concurrent;

namespace NotifyR;

internal static class WrapperCache<TWrapper>
    where TWrapper : class
{
    private static readonly ConcurrentDictionary<Type, TWrapper> s_cache = new();

    internal static TWrapper GetOrCreate(Type key, Func<Type, TWrapper> factory) =>
        s_cache.GetOrAdd(key, factory);
}
