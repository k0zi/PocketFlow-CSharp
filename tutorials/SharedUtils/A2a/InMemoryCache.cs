namespace A2aProtocol;

/// <summary>
/// Thread-safe, in-memory key-value cache.
/// Ported from <c>common/utils/in_memory_cache.py</c>.
/// </summary>
public class InMemoryCache<T>
{
    private readonly Dictionary<string, T> _cache = new();
    private readonly Lock _lock = new();

    /// <summary>Returns the cached value for <paramref name="key"/>, or <c>default</c>.</summary>
    public T? Get(string key)
    {
        lock (_lock)
            return _cache.TryGetValue(key, out var v) ? v : default;
    }

    /// <summary>Stores <paramref name="value"/> under <paramref name="key"/>.</summary>
    public void Set(string key, T value)
    {
        lock (_lock)
            _cache[key] = value;
    }

    /// <summary>Removes the entry for <paramref name="key"/> if it exists.</summary>
    public void Delete(string key)
    {
        lock (_lock)
            _cache.Remove(key);
    }

    /// <summary>
    /// Returns the cached value for <paramref name="key"/>.
    /// If the key is absent the <paramref name="factory"/> is called once and the
    /// result is stored before returning.
    /// </summary>
    public T GetOrSet(string key, Func<T> factory)
    {
        lock (_lock)
        {
            if (!_cache.TryGetValue(key, out var v))
            {
                v = factory();
                _cache[key] = v;
            }
            return v;
        }
    }

    /// <summary>Applies <paramref name="updater"/> to the existing value and stores the result.</summary>
    /// <exception cref="KeyNotFoundException">Thrown when <paramref name="key"/> is absent.</exception>
    public T Update(string key, Func<T, T> updater)
    {
        lock (_lock)
        {
            if (!_cache.TryGetValue(key, out var v))
                throw new KeyNotFoundException($"Key '{key}' not found in cache.");
            var updated = updater(v);
            _cache[key] = updated;
            return updated;
        }
    }

    /// <summary>Returns all current keys.</summary>
    public IReadOnlyList<string> Keys()
    {
        lock (_lock)
            return _cache.Keys.ToList();
    }

    /// <summary>Returns <c>true</c> when <paramref name="key"/> exists.</summary>
    public bool Contains(string key)
    {
        lock (_lock)
            return _cache.ContainsKey(key);
    }

    /// <summary>Removes all entries.</summary>
    public void Clear()
    {
        lock (_lock)
            _cache.Clear();
    }
}

