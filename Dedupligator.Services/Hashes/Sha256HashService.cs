using System.Security.Cryptography;

namespace Dedupligator.Services.Hashes
{
  /// <summary>
  /// Сервис для вычисления и сравнения хэшей файлов с использованием SHA256.
  /// </summary>
  public class Sha256HashService : IHashService
  {
    /// <summary>
    /// Вычисляет SHA256 хэш содержимого файла.
    /// </summary>
    public string ComputeHash(FileInfo file)
    {
      using var stream = file.OpenRead();
      using var sha256 = SHA256.Create();
      var hashBytes = sha256.ComputeHash(stream);
      return BitConverter.ToString(hashBytes).Replace("-", "");
    }

    /// <summary>
    /// Сравнивает хэши двух файлов.
    /// </summary>
    public bool CompareHashes(FileInfo file1, FileInfo file2)
    {
      var hash1 = ComputeHash(file1);
      var hash2 = ComputeHash(file2);
      return AreHashesEqual(hash1, hash2);
    }

    /// <summary>
    /// Сравнивает два хэша на идентичность.
    /// </summary>
    public bool AreHashesEqual(string hash1, string hash2)
    {
      return string.Equals(hash1, hash2, StringComparison.OrdinalIgnoreCase);
    }
  }
}
