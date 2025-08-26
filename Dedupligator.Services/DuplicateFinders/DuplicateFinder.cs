using System.Threading;
using System.Linq;

namespace Dedupligator.Services.DuplicateFinders
{
  /// <summary>
  /// Поиск дубликатов файлов.
  /// </summary>
  public class DuplicateFinder(IDuplicateMatchStrategy strategy)
  {
    /// <summary>
    /// Стратегия поиска дубликатов файлов.
    /// </summary>
    private readonly IDuplicateMatchStrategy _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));

    /// <summary>
    /// Находит дубликаты файлов в указанной директории и её поддиректориях.
    /// </summary>
    /// <param name="directoryPath">Путь к директории для поиска.</param>
    /// <param name="progressCallback">Колбэк для прогресса.</param>
    /// <returns>Список групп дубликатов.</returns>
    public List<List<FileInfo>> FindDuplicates(string directoryPath, Action<double>? progressCallback = null)
    {
      var normalizedPath = PathHelper.NormalizeAndValidateDirectoryPath(directoryPath);

      // 1. Получаем все подходящие файлы.
      var allFiles = GetImageFiles(normalizedPath);

      // 2. Группируем файлы по ключу (например, по размеру) для первого быстрого отсева.
      var groupedFiles = GetGroupedFiles(allFiles);

      return FindDuplicatesInGroups(groupedFiles, progressCallback);
    }

    /// <summary>
    /// Ищет дубликаты в группах.
    /// </summary>
    /// <param name="groupedFiles">Сгруппированные файлы.</param>
    /// <param name="progressCallback">Колбэк для прогресса.</param>
    /// <returns>Найденные группы дубликатов</returns>
    private List<List<FileInfo>> FindDuplicatesInGroups(IEnumerable<IGrouping<object, FileInfo>> groupedFiles, Action<double>? progressCallback = null)
    {
      var duplicateGroups = new List<List<FileInfo>>();
      var filesCount = groupedFiles.Sum(x => x.Count());

      var processedFilesCount = 0;
      foreach (var group in groupedFiles)
      {
        ProcessGroup(group, duplicateGroups);

        processedFilesCount += group.Count();
        progressCallback?.Invoke((double)(processedFilesCount) / filesCount * 100);
      }

      return duplicateGroups;
    }

    /// <summary>
    /// Обрабатывает одну группу файлов для поиска дубликатов.
    /// </summary>
    /// <param name="group">Группа файлов.</param>
    /// <param name="duplicateGroups">Коллекция для результатов.</param>
    private void ProcessGroup(IGrouping<object, FileInfo> group, List<List<FileInfo>> duplicateGroups)
    {
      var files = group.ToList();

      for (int i = 0; i < files.Count; i++)
        ProcessFile(i, files, duplicateGroups);
    }

    /// <summary>
    /// Обрабатывает один файл в группе для поиска его дубликатов.
    /// </summary>
    /// <param name="index">Индекс файла в группе.</param>
    /// <param name="files">Список файлов группы.</param>
    /// <param name="duplicateGroups">Коллекция для результатов.</param>
    private void ProcessFile(int index, List<FileInfo> files, List<List<FileInfo>> duplicateGroups)
    {
      if (IsFileAlreadyProcessed(files[index], duplicateGroups))
        return;

      var duplicates = FindDuplicatesForFile(index, files, duplicateGroups);
      AddDuplicatesToResults(duplicates, duplicateGroups);
    }

    /// <summary>
    /// Находит все дубликаты для указанного файла в группе.
    /// </summary>
    /// <param name="index">Индекс исходного файла.</param>
    /// <param name="files">Список файлов группы.</param>
    /// <param name="duplicateGroups">Коллекция для проверки обработанных файлов.</param>
    /// <returns>Список найденных дубликатов.</returns>
    private List<FileInfo> FindDuplicatesForFile(int index, List<FileInfo> files, List<List<FileInfo>> duplicateGroups)
    {
      var duplicates = new List<FileInfo>();

      for (int j = index + 1; j < files.Count; j++)
        CheckFilePair(index, j, files, duplicates, duplicateGroups);

      return duplicates;
    }

    /// <summary>
    /// Проверяет пару файлов на дубликаты и добавляет в результат при совпадении.
    /// </summary>
    /// <param name="indexI">Индекс первого файла.</param>
    /// <param name="indexJ">Индекс второго файла.</param>
    /// <param name="files">Список файлов группы.</param>
    /// <param name="duplicates">Текущий список дубликатов.</param>
    /// <param name="duplicateGroups">Коллекция для проверки обработанных файлов.</param>
    private void CheckFilePair(int indexI, int indexJ, List<FileInfo> files, List<FileInfo> duplicates, List<List<FileInfo>> duplicateGroups)
    {
      if (IsFileAlreadyProcessed(files[indexJ], duplicateGroups))
        return;

      if (FilesAreDuplicates(files[indexI], files[indexJ]))
        AddDuplicatePair(files[indexI], files[indexJ], duplicates);
    }

    /// <summary>
    /// Проверяет, являются ли два файла дубликатами с обработкой ошибок.
    /// </summary>
    /// <param name="file1">Первый файл.</param>
    /// <param name="file2">Второй файл.</param>
    /// <returns>True если файлы дубликаты.</returns>
    private bool FilesAreDuplicates(FileInfo file1, FileInfo file2)
    {
      try
      {
        return _strategy.AreDuplicates(file1, file2);
      }
      catch (Exception ex)
      {
        LogComparisonError(file1, file2, ex);
        return false;
      }
    }

    /// <summary>
    /// Добавляет пару файлов в список дубликатов.
    /// </summary>
    /// <param name="sourceFile">Исходный файл.</param>
    /// <param name="duplicateFile">Дубликат.</param>
    /// <param name="duplicates">Список дубликатов.</param>
    private static void AddDuplicatePair(FileInfo sourceFile, FileInfo duplicateFile, List<FileInfo> duplicates)
    {
      if (!duplicates.Contains(sourceFile))
        duplicates.Add(sourceFile);

      duplicates.Add(duplicateFile);
    }

    /// <summary>
    /// Добавляет найденные дубликаты в общую коллекцию результатов.
    /// </summary>
    /// <param name="duplicates">Список дубликатов.</param>
    /// <param name="duplicateGroups">Общая коллекция результатов.</param>
    private static void AddDuplicatesToResults(List<FileInfo> duplicates, List<List<FileInfo>> duplicateGroups)
    {
      if (duplicates.Count > 0)
        duplicateGroups.Add(duplicates);
    }

    /// <summary>
    /// Логирует ошибку сравнения файлов.
    /// </summary>
    /// <param name="file1">Первый файл.</param>
    /// <param name="file2">Второй файл.</param>
    /// <param name="ex">Исключение.</param>
    private static void LogComparisonError(FileInfo file1, FileInfo file2, Exception ex)
    {
      Console.WriteLine($"Ошибка сравнения {file1.Name} и {file2.Name}: {ex.Message}");
    }

    /// <summary>
    /// Проверяет, был ли файл уже обработан и добавлен в группы дубликатов.
    /// </summary>
    /// <param name="file">Файл для проверки.</param>
    /// <param name="duplicateGroups">Коллекция групп дубликатов.</param>
    /// <returns>True если файл уже обработан.</returns>
    private static bool IsFileAlreadyProcessed(FileInfo file, List<List<FileInfo>> duplicateGroups)
    {
      return duplicateGroups.Any(group => group.Contains(file));
    }

    /// <summary>
    /// Группирует файлы по ключу, определенному стратегией.
    /// </summary>
    /// <param name="allFiles">Все файлы для обработки.</param>
    /// <returns>Сгруппированные файлы.</returns>
    private List<IGrouping<object, FileInfo>> GetGroupedFiles(List<FileInfo> allFiles)
    {
      List<IGrouping<object, FileInfo>> groupedFiles = [];

      if (_strategy.RequiresPreGrouping)
      {
        groupedFiles = [.. allFiles
            .GroupBy(_strategy.GroupingKeySelector)
            .Where(group => group.Count() > 1)];
      }
      return groupedFiles;
    }

    /// <summary>
    /// Получает все поддерживаемые изображения из директории.
    /// </summary>
    /// <param name="directoryPath">Путь к директории.</param>
    /// <returns>Список файлов изображений.</returns>
    private static List<FileInfo> GetImageFiles(string directoryPath)
    {
      var allFiles = new List<FileInfo>();

      var options = new EnumerationOptions
      {
        RecurseSubdirectories = true,
        IgnoreInaccessible = true,
        AttributesToSkip = FileAttributes.System | FileAttributes.Temporary
      };

      var rootDirs = Directory.GetDirectories(directoryPath);
      foreach (var dir in rootDirs)
      {
        AddImageFilesFromDirectory(allFiles, dir, options);
      }

      // Также добавляем файлы из самого корня
      AddImageFilesFromDirectory(allFiles, directoryPath, new EnumerationOptions
      {
        RecurseSubdirectories = false,
        IgnoreInaccessible = true,
        AttributesToSkip = FileAttributes.System | FileAttributes.Temporary
      });

      return allFiles;
    }

    private static void AddImageFilesFromDirectory(List<FileInfo> allFiles, string directoryPath, EnumerationOptions options)
    {
      try
      {
        var files = Directory.EnumerateFiles(directoryPath, "*", options)
            .Where(IsImageFile)
            .Select(filePath => new FileInfo(filePath));

        allFiles.AddRange(files);
      }
      catch (UnauthorizedAccessException)
      {
        // Пропускаем папки без доступа
      }
      catch (IOException)
      {
        // Пропускаем папки с ошибками ввода-вывода
      }
    }

    /// <summary>
    /// Проверяет, является ли файл поддерживаемым форматом изображения.
    /// </summary>
    /// <param name="filePath">Путь к файлу.</param>
    /// <returns>True если формат поддерживается.</returns>
    private static bool IsImageFile(string filePath)
    {
      string[] imageExtensions = { ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".webp" };
      string extension = Path.GetExtension(filePath).ToLower();
      return imageExtensions.Contains(extension);
    }
  }
}
