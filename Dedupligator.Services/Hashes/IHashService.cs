namespace Dedupligator.Services.Hashes
{
  /// <summary>
  /// Интерфейс для вычисления и сравнения хэшей файлов.
  /// </summary>
  public interface IHashService
  {
    /// <summary>
    /// Вычисляет хэш для указанного файла.
    /// </summary>
    /// <param name="file">Файл для хэширования.</param>
    /// <returns>Хэш файла в виде строки.</returns>
    string ComputeHash(FileInfo file);

    /// <summary>
    /// Сравнивает хэши двух файлов.
    /// </summary>
    /// <param name="file1">Первый файл.</param>
    /// <param name="file2">Второй файл.</param>
    /// <returns>True если хэши совпадают.</returns>
    bool CompareHashes(FileInfo file1, FileInfo file2);

    /// <summary>
    /// Сравнивает два хэша на равенство.
    /// </summary>
    /// <param name="hash1">Первый хэш.</param>
    /// <param name="hash2">Второй хэш.</param>
    /// <returns>True если хэши равны.</returns>
    bool AreHashesEqual(string hash1, string hash2);
  }
}
