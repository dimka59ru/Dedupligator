using System.Collections.Concurrent;

namespace Dedupligator.Services.Cache
{
  /// <summary>
  /// Потокобезопасный LRU кэш с ограничением размера
  /// </summary>
  /// <typeparam name="TKey">Тип ключа</typeparam>
  /// <typeparam name="TValue">Тип значения</typeparam>
  public class LruCache<TKey, TValue> where TKey : notnull
  {
    private readonly ConcurrentDictionary<TKey, (TValue Value, long AccessTime)> _cache;
    private readonly object _lock = new();
    private readonly int _capacity;

    /// <summary>
    /// Инициализирует новый экземпляр LRU кэша
    /// </summary>
    /// <param name="capacity">Максимальное количество элементов</param>
    public LruCache(int capacity = 10000)
    {
      _capacity = capacity;
      _cache = new ConcurrentDictionary<TKey, (TValue, long)>();
    }

    /// <summary>
    /// Получает значение из кэша или вычисляет его с помощью factory функции
    /// </summary>
    public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)
    {
      // Пытаемся получить из кэша
      if (TryGet(key, out var value))
      {
        return value;
      }

      // Вычисляем значение
      var newValue = valueFactory(key);

      // Добавляем в кэш
      Add(key, newValue);

      return newValue;
    }

    /// <summary>
    /// Пытается получить значение из кэша
    /// </summary>
    public bool TryGet(TKey key, out TValue value)
    {
      long currentTime = DateTime.UtcNow.Ticks;

      if (_cache.TryGetValue(key, out var cacheEntry))
      {
        // Обновляем время доступа
        _cache[key] = (cacheEntry.Value, currentTime);
        value = cacheEntry.Value;
        return true;
      }

      value = default!;
      return false;
    }

    /// <summary>
    /// Добавляет или обновляет значение в кэше
    /// </summary>
    public void Add(TKey key, TValue value)
    {
      long currentTime = DateTime.UtcNow.Ticks;

      lock (_lock)
      {
        // Если кэш достиг лимита, удаляем самый старый элемент
        if (_cache.Count >= _capacity)
        {
          RemoveOldestItem();
        }

        _cache[key] = (value, currentTime);
      }
    }

    /// <summary>
    /// Удаляет элемент из кэша
    /// </summary>
    public bool Remove(TKey key)
    {
      return _cache.TryRemove(key, out _);
    }

    /// <summary>
    /// Очищает кэш
    /// </summary>
    public void Clear()
    {
      lock (_lock)
      {
        _cache.Clear();
      }
    }

    /// <summary>
    /// Возвращает количество элементов в кэше
    /// </summary>
    public int Count => _cache.Count;

    /// <summary>
    /// Удаляет самый старый элемент из кэша
    /// </summary>
    private void RemoveOldestItem()
    {
      var oldestEntry = _cache
          .OrderBy(x => x.Value.AccessTime)
          .FirstOrDefault();

      if (!EqualityComparer<TKey>.Default.Equals(oldestEntry.Key, default))
      {
        _cache.TryRemove(oldestEntry.Key, out _);
      }
    }

    /// <summary>
    /// Возвращает время самого старого элемента в кэше
    /// </summary>
    public DateTime GetOldestItemTime()
    {
      if (_cache.IsEmpty)
        return DateTime.MinValue;

      var oldestTime = _cache
          .Select(x => x.Value.AccessTime)
          .Min();

      return new DateTime(oldestTime);
    }
  }
}
