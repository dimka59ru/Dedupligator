using Dedupligator.Services.Hash;

namespace Dedupligator.Services.Cache
{
  /// <summary>
  /// Кэш для pHash значений
  /// </summary>
  public static class PHashCache
  {
    private static readonly LruCache<string, ulong> _cache = new(10000);

    /// <summary>
    /// Получает pHash из кэша или вычисляет его
    /// </summary>
    public static ulong GetOrCalculate(string filePath)
    {
      return _cache.GetOrAdd(filePath, key => PHashCalculator.CalculatePHash(key));
    }

    /// <summary>
    /// Пытается получить pHash из кэша
    /// </summary>
    public static bool TryGet(string filePath, out ulong hash)
    {
      return _cache.TryGet(filePath, out hash);
    }

    /// <summary>
    /// Добавляет pHash в кэш
    /// </summary>
    public static void Add(string filePath, ulong hash)
    {
      _cache.Add(filePath, hash);
    }

    /// <summary>
    /// Очищает кэш
    /// </summary>
    public static void Clear()
    {
      _cache.Clear();
    }

    /// <summary>
    /// Возвращает количество элементов в кэше
    /// </summary>
    public static int Count => _cache.Count;
  }
}