using System.Collections.Concurrent;

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
    public async Task<List<List<FileInfo>>> FindDuplicatesAsync(string directoryPath, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
    {
      var normalizedPath = PathHelper.NormalizeAndValidateDirectoryPath(directoryPath);

      // 1. Получаем все подходящие файлы.
      var allFiles = GetImageFiles(normalizedPath);

      // 2. Группируем файлы по ключу (например, по размеру) для первого быстрого отсева.
      var groupedFiles = GetGroupedFiles(allFiles);

      var maxParallelism = Environment.ProcessorCount;
      var duplicateGroups = await FindDuplicatesInGroupsWithThrottling(groupedFiles, progress, maxParallelism, cancellationToken);

      return duplicateGroups;
    }

    private async Task<List<List<FileInfo>>> FindDuplicatesInGroupsWithThrottling(
        IEnumerable<IGrouping<object, FileInfo>> groupedFiles,
        IProgress<double>? progress = null,
        int maxParallelism = 4,
        CancellationToken cancellationToken = default)
    {
      var duplicateGroups = new ConcurrentBag<List<FileInfo>>();
      var totalFiles = groupedFiles.Sum(x => x.Count());
      long processedFilesCount = 0;

      if (totalFiles == 0)
      {
        progress?.Report(100.0);
        return [.. duplicateGroups];
      }

      var semaphore = new SemaphoreSlim(maxParallelism);
      var tasks = new List<Task>();

      try
      {
        foreach (var group in groupedFiles)
        {
          cancellationToken.ThrowIfCancellationRequested();
          await semaphore.WaitAsync(cancellationToken);

          var task = Task.Run(() =>
          {
            try
            {
              var groupFiles = group.ToList();
              var groupDuplicates = FindDuplicateGroupsInFileGroup(groupFiles, cancellationToken);

              foreach (var duplicates in groupDuplicates)
                duplicateGroups.Add(duplicates);

              // Атомарно увеличиваем счётчик обработанных файлов
              var groupCount = groupFiles.Count;
              Interlocked.Add(ref processedFilesCount, groupCount);

              // Отчёт о прогрессе — только при значимом изменении
              var currentProgress = (double)Interlocked.Read(ref processedFilesCount) / totalFiles * 100.0;

              // Чтобы не спамить, можно добавить порог (например, обновлять каждые ~1%)
              // Но для простоты: просто отчитываемся
              progress?.Report(currentProgress);
            }
            finally
            {
              semaphore.Release();
            }
          }, cancellationToken);

          tasks.Add(task);
        }

        await Task.WhenAll(tasks);
      }
      catch (OperationCanceledException)
      {
        Console.WriteLine("Операция поиска дубликатов была отменена");
        throw;
      }
      finally
      {
        semaphore?.Release(maxParallelism); // Подстраховка
      }

      return [.. duplicateGroups];
    }

    private List<List<FileInfo>> FindDuplicateGroupsInFileGroup(List<FileInfo> files, CancellationToken cancellationToken)
    {
      var duplicateGroups = new List<List<FileInfo>>();
      var processedFiles = new HashSet<string>();

      for (int i = 0; i < files.Count; i++)
      {
        var currentFile = files[i];

        if (processedFiles.Contains(currentFile.FullName))
          continue;

        var currentGroup = new List<FileInfo>() { currentFile };

        for (int j = i + 1; j < files.Count; j++)
        {
          cancellationToken.ThrowIfCancellationRequested();

          var otherFile = files[j];
          if (processedFiles.Contains(otherFile.FullName))
            continue;

          if (FilesAreDuplicates(currentFile, otherFile))
          {
            currentGroup.Add(otherFile);
            processedFiles.Add(otherFile.FullName);
          }
        }

        if (currentGroup.Count > 1)
        {
          duplicateGroups.Add(currentGroup);
          processedFiles.Add(currentFile.FullName);
        }
      }

      return duplicateGroups;
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
    /// Группирует файлы по ключу, определенному стратегией.
    /// </summary>
    /// <param name="allFiles">Все файлы для обработки.</param>
    /// <returns>Сгруппированные файлы.</returns>
    private List<IGrouping<object, FileInfo>> GetGroupedFiles(List<FileInfo> allFiles)
    {
      if (allFiles.Count == 0)
        return [];

      if (_strategy.RequiresPreGrouping)
      {
        return [.. allFiles
            .GroupBy(_strategy.GroupingKeySelector)
            .Where(group => group.Count() > 1)];
      }
      else
      {
        // Если группировка не требуется, передаём все файлы как одну группу
        return [allFiles.GroupBy(_ => (object)"ungrouped").First()];
      }
    }

    /// <summary>
    /// Получает все поддерживаемые изображения из директории.
    /// </summary>
    /// <param name="directoryPath">Путь к директории.</param>
    /// <returns>Список файлов изображений.</returns>
    private static List<FileInfo> GetImageFiles(string directoryPath)
    {
      var allFiles = new List<FileInfo>();

      var rootDirs = Directory.GetDirectories(directoryPath);
      foreach (var dir in rootDirs)
      {
        AddImageFilesFromDirectory(allFiles, dir, new EnumerationOptions
        {
          RecurseSubdirectories = true,
          IgnoreInaccessible = true,
          AttributesToSkip = FileAttributes.System | FileAttributes.Temporary
        });
      }

      // Также добавляем файлы из исходной папки.
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
