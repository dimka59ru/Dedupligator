namespace Dedupligator.Services.DuplicateFinders
{
  /// <summary>
  /// Стратегия поиска дубликатов.
  /// </summary>
  public interface IDuplicateMatchStrategy
  {

    /// <summary>
    /// Селектор ключа, для предварительной группировки файлов.
    /// </summary>
    Func<FileInfo, object> GroupingKeySelector { get; }

    /// <summary>
    /// Требудется ли предварительная группировка файлов.
    /// </summary>
    bool RequiresPreGrouping { get; }

    /// <summary>
    /// Определить, являются ли два файла дубликатами.
    /// </summary>
    /// <param name="file1">Первый файл.</param>
    /// <param name="file2">Второй файл.</param>
    /// <returns>true, если файлы являются дубликатами.</returns>
    bool AreDuplicates(FileInfo file1, FileInfo file2);
  }
}
