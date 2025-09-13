using Dedupligator.Common.Helpers;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Dedupligator.Services.DuplicateFinders
{
  /// <summary>
  /// Поиск дубликатов файлов.
  /// </summary>
  public class DuplicateFinder(IDuplicateMatchStrategy strategy)
  {
    private readonly int _maxParallelism = Environment.ProcessorCount;

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
    public List<List<FileInfo>> FindDuplicates(string directoryPath, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
    {
      const double SCAN_PHASE_WEIGHT = 0.1;    // 0% → 10%
      const double GROUP_PHASE_WEIGHT = 0.3;   // 10% → 40%
      const double COMPARE_PHASE_WEIGHT = 0.6; // 40% → 100%

      var normalizedPath = PathHelper.NormalizeAndValidateDirectoryPath(directoryPath);

      List<FileInfo> allFiles;
      try
      {
        //  1. Сканирование файлов
        allFiles = GetImageFiles(normalizedPath, progress, SCAN_PHASE_WEIGHT, _maxParallelism, cancellationToken);
      }
      catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
      {
        Console.WriteLine("Сканирование файлов было отменено");
        throw;
      }

      if (allFiles.Count == 0)
      {
        progress?.Report(100.0);
        return [];
      }

      // 2. Группировка (с вычислением ключей)
      var groupedFiles = GetGroupedFiles(
          allFiles,
          progress,
          SCAN_PHASE_WEIGHT,
          GROUP_PHASE_WEIGHT,
          _maxParallelism,
          cancellationToken);

      var totalCompareFiles = groupedFiles.Sum(g => g.Count());
      if (totalCompareFiles == 0)
      {
        progress?.Report(100.0);
        return [];
      }

      // 3. Поиск дубликатов в группах
      var duplicateGroups = FindDuplicatesInGroupsWithThrottling(
          groupedFiles,
          progress,
          SCAN_PHASE_WEIGHT + GROUP_PHASE_WEIGHT, // начало фазы
          COMPARE_PHASE_WEIGHT,
          _maxParallelism,
          cancellationToken);

      return duplicateGroups;
    }

    private List<List<FileInfo>> FindDuplicatesInGroupsWithThrottling(
        IEnumerable<IGrouping<object, FileInfo>> groupedFiles,
        IProgress<double>? progress,
        double startPhase,
        double phaseWeight,
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

      var options = new ParallelOptions
      {
        CancellationToken = cancellationToken,
        MaxDegreeOfParallelism = maxParallelism
      };

      var currentProgress = progress;

      Parallel.ForEach(groupedFiles, options, group =>
      {
        var groupFiles = group.ToList();
        var groupDuplicates = FindDuplicateGroupsInFileGroup(
          groupFiles, 
          cancellationToken,
          () =>
          {
            var processed = Interlocked.Increment(ref processedFilesCount);
            var progressValue = startPhase + phaseWeight * (double)processed / totalFiles;
            currentProgress?.Report(Math.Min(progressValue * 100, 100.0));
          });

        foreach (var duplicates in groupDuplicates)
          duplicateGroups.Add(duplicates);
      });
      
      return [.. duplicateGroups];
    }

    private List<List<FileInfo>> FindDuplicateGroupsInFileGroup(List<FileInfo> files, CancellationToken cancellationToken, Action? progressCallback = null)
    {
      var duplicateGroups = new List<List<FileInfo>>();
      var processedFiles = new HashSet<string>();

      for (int i = 0; i < files.Count; i++)
      {
        var currentFile = files[i];
        progressCallback?.Invoke();

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
    private List<IGrouping<object, FileInfo>> GetGroupedFiles(List<FileInfo> allFiles,
      IProgress<double>? progress,
      double startProgress,
      double phaseWeight,
      int maxParallelism = 4,
      CancellationToken cancellationToken = default)
    {
      if (allFiles.Count == 0)
        return [];

      if (!_strategy.RequiresPreGrouping)
      {
        progress?.Report((startProgress + phaseWeight) * 100);
        return [allFiles.GroupBy(_ => (object)"ungrouped").First()];
      }

      var fileKeys = new ConcurrentDictionary<FileInfo, object>();
      long processed = 0;
      var total = allFiles.Count;

      var options = new ParallelOptions
      {
        CancellationToken = cancellationToken,
        MaxDegreeOfParallelism = maxParallelism
      };

      Parallel.ForEach(allFiles, options, file =>
      {
        try
        {
          var key = _strategy.GroupingKeySelector(file);
          fileKeys[file] = key;
        }
        catch
        {
          fileKeys[file] = "error";
        }

        var current = Interlocked.Increment(ref processed);
        var progressValue = startProgress + phaseWeight * (double)current / total;
        progress?.Report(progressValue * 100);

      });

      return [.. fileKeys
      .GroupBy(kvp => kvp.Value, kvp => kvp.Key)
      .Where(g => g.Count() > 1)];
    }

    /// <summary>
    /// Получает все поддерживаемые изображения из директории.
    /// </summary>
    /// <param name="directoryPath">Путь к директории.</param>
    /// <returns>Список файлов изображений.</returns>
    private static List<FileInfo> GetImageFiles(
      string directoryPath, 
      IProgress<double>? progress, 
      double phaseWeight, 
      int maxParallelism = 4, 
      CancellationToken cancellationToken = default)
    {
      var allFiles = new ConcurrentBag<FileInfo>();

      var rootDirs = Directory.GetDirectories(directoryPath);

      var totalDirs = rootDirs.Length + 1; // +1 для корня
      long processedDirs = 0;

      var enumerationOptions = new EnumerationOptions
      {
        RecurseSubdirectories = true,
        IgnoreInaccessible = true,
        AttributesToSkip = FileAttributes.System | FileAttributes.Temporary
      };

      var parallelOptions = new ParallelOptions
      {
        CancellationToken = cancellationToken,
        MaxDegreeOfParallelism = maxParallelism,
      };

      Parallel.ForEach(rootDirs, parallelOptions, dir =>
      {
        var files = AddImageFilesFromDirectory(dir, enumerationOptions, cancellationToken);
        allFiles.AddRange(files);

        var currentProgress = (double)Interlocked.Increment(ref processedDirs) / totalDirs;
        progress?.Report(currentProgress * phaseWeight * 100);
      });

      // Обрабатываем файлы из корневой директории
      cancellationToken.ThrowIfCancellationRequested();
      var rootOptions = new EnumerationOptions
      {
        RecurseSubdirectories = false,
        IgnoreInaccessible = true,
        AttributesToSkip = FileAttributes.System | FileAttributes.Temporary
      };

      var rootFiles = AddImageFilesFromDirectory(directoryPath, rootOptions, cancellationToken);
      allFiles.AddRange(rootFiles);

      var currentProgress2 = (double)Interlocked.Increment(ref processedDirs) / totalDirs;
      progress?.Report(currentProgress2 * phaseWeight * 100);

      return [.. allFiles];
    }

    private static List<FileInfo> AddImageFilesFromDirectory(string directoryPath, EnumerationOptions options, CancellationToken cancellationToken)
    {
      cancellationToken.ThrowIfCancellationRequested();

      try
      {
        return [.. Directory.EnumerateFiles(directoryPath, "*", options)
            .Where(IsImageFile)
            .Select(filePath =>
            {
              cancellationToken.ThrowIfCancellationRequested();
              return new FileInfo(filePath);
            })];
      }
      catch (UnauthorizedAccessException)
      {
        // Пропускаем папки без доступа
        return [];
      }
      catch (IOException)
      {
        // Пропускаем папки с ошибками ввода-вывода
        return [];
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
