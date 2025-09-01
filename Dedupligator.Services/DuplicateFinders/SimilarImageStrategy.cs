using Dedupligator.Services.Cache;
using Dedupligator.Services.Hash;

namespace Dedupligator.Services.DuplicateFinders
{
  public class SimilarImageStrategy : IDuplicateMatchStrategy
  {
    private readonly int _threshold = 10;

    public Func<FileInfo, object> GroupingKeySelector => file =>
    {
      return RoughGrouper.CreateGroupKey(file.FullName);
    };

    public bool RequiresPreGrouping => true;

    public bool AreDuplicates(FileInfo file1, FileInfo file2)
    {
      ulong hash1 = PHashCache.GetOrCalculate(file1.FullName);
      ulong hash2 = PHashCache.GetOrCalculate(file2.FullName);

      int distance = PHashCalculator.HammingDistance(hash1, hash2);
      return distance <= _threshold;
    }

    /// <summary>
    /// Очищает кэш pHash (можно вызывать периодически)
    /// </summary>
    public static void ClearCache()
    {
      PHashCache.Clear();
    }

    /// <summary>
    /// Возвращает текущий размер кэша
    /// </summary>
    public static int GetCacheSize()
    {
      return PHashCache.Count;
    }
  }
}