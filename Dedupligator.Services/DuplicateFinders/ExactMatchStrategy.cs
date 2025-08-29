using System.Security.Cryptography;

namespace Dedupligator.Services.DuplicateFinders
{
  /// <summary>
  /// Стратегия для поиска точных дубликатов по хэшу содержимого.
  /// </summary>
  public class ExactMatchStrategy : IDuplicateMatchStrategy
  {
    public Func<FileInfo, object> GroupingKeySelector => file => file.Length;

    public bool RequiresPreGrouping => true;

    public bool AreDuplicates(FileInfo file1, FileInfo file2)
    {
      // Только если размер совпадает — проверяем содержимое
      return ComputeSha256(file1) == ComputeSha256(file2);
    }

    private static byte[] ComputeSha256(FileInfo file)
    {
      using var stream = file.OpenRead();
      using var sha = SHA256.Create();
      return sha.ComputeHash(stream);
    }
  }
}
