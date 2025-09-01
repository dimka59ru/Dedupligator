using System.Runtime.Intrinsics.Arm;
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
      var hash1 = ComputeSha256(file1);
      var hash2 = ComputeSha256(file2);
      return string.Equals(hash1, hash2, StringComparison.OrdinalIgnoreCase);
    }

    private static string ComputeSha256(FileInfo file)
    {
      using var stream = file.OpenRead();
      using var sha = SHA256.Create();
      var hashBytes = sha.ComputeHash(stream);
      return BitConverter.ToString(hashBytes).Replace("-", "");
    }
  }
}
