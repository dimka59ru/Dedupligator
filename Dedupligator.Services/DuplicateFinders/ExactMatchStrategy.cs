using Dedupligator.Services.Cache;
using System.Security.Cryptography;

namespace Dedupligator.Services.DuplicateFinders
{
  /// <summary>
  /// Стратегия для поиска точных дубликатов по хэшу содержимого.
  /// </summary>
  public class ExactMatchStrategy : ICachedDuplicateMatchStrategy
  {
    private static readonly LruCache<string, string> _cache = new(10000);

    public Func<FileInfo, object> GroupingKeySelector => file => file.Length;

    public bool RequiresPreGrouping => true;

    public bool AreDuplicates(FileInfo file1, FileInfo file2)
    {
      if (file1.Length != file2.Length)
        return false;

      var hash1 = GetOrCalculateHash(file1);
      var hash2 = GetOrCalculateHash(file2);
      return string.Equals(hash1, hash2, StringComparison.OrdinalIgnoreCase);
    }
    public void ClearCache()
    {
      _cache.Clear();
    }

    private static string ComputeSha256(FileInfo file)
    {
      using var stream = file.OpenRead();
      using var sha = SHA256.Create();
      var hashBytes = sha.ComputeHash(stream);
      return BitConverter.ToString(hashBytes).Replace("-", "");
    }

    private static string GetOrCalculateHash(FileInfo file)
    {
      try
      {
        var cacheKey = $"{file.FullName}|{file.Length}|{file.LastWriteTime:u}";
        return _cache.GetOrAdd(cacheKey, key => ComputeSha256(file));
      }
      catch (Exception)
      {
        return string.Empty;
      }
    }
  }
}
