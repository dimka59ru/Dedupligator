using Dedupligator.Common.Helpers;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Dedupligator.Services.DuplicateFinders
{
  /// <summary>
  /// Поиск дубликатов файлов.
  /// </summary>
  public class DuplicateFinder
  {
    private readonly int _maxParallelism = Environment.ProcessorCount;

    /// <summary>
    /// Логгер.
    /// </summary>
    private readonly ILogger<DuplicateFinder> _logger;

    /// <summary>
    /// Стратегия поиска дубликатов файлов.
    /// </summary>
    private IDuplicateMatchStrategy? _strategy;

    public void SetStrategy(IDuplicateMatchStrategy strategy)
    {
      _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
      _logger.LogDebug("Strategy set to: {StrategyType}", strategy.GetType().Name);
    }

    /// <summary>
    /// Находит дубликаты файлов в указанной директории и её поддиректориях.
    /// </summary>
    /// <param name="directoryPath">Путь к директории для поиска.</param>
    /// <param name="progressCallback">Колбэк для прогресса.</param>
    /// <returns>Список групп дубликатов.</returns>
    public List<List<FileInfo>> FindDuplicates(string directoryPath, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
    {
      if (_strategy == null)
      {
        throw new InvalidOperationException("Strategy not set. Call SetStrategy first.");
      }

      _logger.LogInformation("Начало поиска дубликатов в директории: {DirectoryPath}", directoryPath);

      const double SCAN_PHASE_WEIGHT = 0.01;    // 0% → 1%
      const double GROUP_PHASE_WEIGHT = 0.49;   // 1% → 50%
      const double COMPARE_PHASE_WEIGHT = 0.5; // 50% → 100%

      var normalizedPath = PathHelper.NormalizeAndValidateDirectoryPath(directoryPath);

      List<FileInfo> allFiles;
      try
      {
        //  1. Сканирование файлов
        _logger.LogDebug("Фаза 1: Сканирование файлов");
        allFiles = GetImageFiles(normalizedPath, progress, SCAN_PHASE_WEIGHT, _maxParallelism, cancellationToken);
        _logger.LogInformation("Найдено файлов: {FileCount}", allFiles.Count);
      }
      catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
      {
        _logger.LogWarning("Сканирование файлов было отменено");
        return [];
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Ошибка при сканировании файлов в директории: {DirectoryPath}", directoryPath);
        throw;
      }

      if (allFiles.Count == 0)
      {
        progress?.Report(100.0);
        _logger.LogInformation("Файлы не найдены в директории: {DirectoryPath}", directoryPath);
        return [];
      }

      // 2. Группировка (с вычислением ключей)
      _logger.LogDebug("Фаза 2: Группировка файлов");
      var groupedFiles = GetGroupedFiles(
          allFiles,
          progress,
          SCAN_PHASE_WEIGHT,
          GROUP_PHASE_WEIGHT,
          _maxParallelism,
          cancellationToken);

      var totalCompareFiles = groupedFiles.Sum(g => g.Count());
      _logger.LogDebug("Файлов для сравнения: {CompareFileCount}", totalCompareFiles);

      if (totalCompareFiles == 0)
      {
        progress?.Report(100.0);
        _logger.LogInformation("Нет файлов для сравнения после группировки");
        return [];
      }

      // 3. Поиск дубликатов в группах
      _logger.LogDebug("Фаза 3: Поиск дубликатов в группах");
      var duplicateGroups = FindDuplicatesInGroupsWithThrottling(
          groupedFiles,
          progress,
          SCAN_PHASE_WEIGHT + GROUP_PHASE_WEIGHT, // начало фазы
          COMPARE_PHASE_WEIGHT,
          _maxParallelism,
          cancellationToken);

      _logger.LogInformation("Найдено групп дубликатов: {DuplicateGroupCount}", duplicateGroups.Count);
      _logger.LogInformation("Общее количество дубликатов: {TotalDuplicates}",
          duplicateGroups.Sum(g => g.Count));

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

      _logger.LogDebug("Начало сравнения {TotalFiles} файлов в {GroupCount} группах",
          totalFiles, groupedFiles.Count());

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

      try
      {
        Parallel.ForEach(groupedFiles, options, group =>
          {
            var groupFiles = group.ToList();
            _logger.LogTrace("Обработка группы из {GroupSize} файлов", groupFiles.Count);

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
            {
              duplicateGroups.Add(duplicates);
            }
          });
      }
      catch (AggregateException ex)
      {
        foreach (var innerEx in ex.InnerExceptions)
        {
          _logger.LogError(innerEx, "Ошибка в параллельной обработке");
        }
        throw new Exception("Ошибка при параллельной обработке групп", ex);
      }
      catch (OperationCanceledException)
      {
        _logger.LogWarning("Сравнение файлов было отменено");
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Ошибка при сравнении файлов в группах");
        throw;
      }

      return [.. duplicateGroups];
    }

    private List<List<FileInfo>> FindDuplicateGroupsInFileGroup(List<FileInfo> files, CancellationToken cancellationToken, Action? progressCallback = null)
    {
      var duplicateGroups = new List<List<FileInfo>>();
      var processedFiles = new HashSet<string>();

      _logger.LogTrace("Поиск дубликатов в группе из {FileCount} файлов", files.Count);

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

            _logger.LogTrace("Найден дубликат: {File1} == {File2}", currentFile.Name, otherFile.Name);
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
        var areDuplicates = _strategy!.AreDuplicates(file1, file2);
        if (areDuplicates)
        {
          _logger.LogTrace("Файлы являются дубликатами: {File1} == {File2}", file1.Name, file2.Name);
        }
        return areDuplicates;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Ошибка сравнения файлов {File1} и {File2}", file1.Name, file2.Name);
        return false;
      }
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

      _logger.LogDebug("Группировка {FileCount} файлов", allFiles.Count);

      if (!_strategy!.RequiresPreGrouping)
      {
        progress?.Report((startProgress + phaseWeight) * 100);
        _logger.LogDebug("Стратегия не требует предварительной группировки");
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

      try
      {
        Parallel.ForEach(allFiles, options, file =>
          {
            try
            {
              var key = _strategy.GroupingKeySelector(file);
              fileKeys[file] = key;
              _logger.LogTrace("Вычислен ключ для файла {FileName}: {Key}", file.Name, key);
            }
            catch (Exception ex)
            {
              fileKeys[file] = "error";
              _logger.LogError(ex, "Ошибка вычисления ключа для файла {FileName}", file.Name);
            }

            var current = Interlocked.Increment(ref processed);
            var progressValue = startProgress + phaseWeight * (double)current / total;
            progress?.Report(progressValue * 100);

          });
      }
      catch (OperationCanceledException)
      {
        _logger.LogWarning("Группировка файлов была отменена");
      }

      var result = fileKeys
         .GroupBy(kvp => kvp.Value, kvp => kvp.Key)
         .Where(g => g.Count() > 1)
         .ToList();

      _logger.LogDebug("Создано {GroupCount} групп после фильтрации", result.Count);

      return result;
    }

    /// <summary>
    /// Получает все поддерживаемые изображения из директории.
    /// </summary>
    /// <param name="directoryPath">Путь к директории.</param>
    /// <returns>Список файлов изображений.</returns>
    private List<FileInfo> GetImageFiles(
      string directoryPath, 
      IProgress<double>? progress, 
      double phaseWeight, 
      int maxParallelism = 4, 
      CancellationToken cancellationToken = default)
    {
      _logger.LogDebug("Сканирование изображений в директории: {DirectoryPath}", directoryPath);

      var allFiles = new ConcurrentBag<FileInfo>();

      List<string>? rootDirs = null;
      try
      {
        rootDirs = GetDirectories(directoryPath, cancellationToken);
      }
      catch (OperationCanceledException)
      {
        _logger.LogWarning("Сканирование директорий было отменено");
        return [];
      }

      var totalDirs = rootDirs.Capacity + 1; // +1 для корня
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

      try
      {
        Parallel.ForEach(rootDirs, parallelOptions, dir =>
          {
            var files = AddImageFilesFromDirectory(dir, enumerationOptions, cancellationToken);
            allFiles.AddRange(files);
            _logger.LogTrace("Найдено {FileCount} файлов в директории {Directory}", files.Count, dir);

            var currentProgress = (double)Interlocked.Increment(ref processedDirs) / totalDirs;
            progress?.Report(currentProgress * phaseWeight * 100);
          });
      }
      catch (OperationCanceledException)
      {
        _logger.LogWarning("Сканирование директорий было отменено");
      }

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
      _logger.LogTrace("Найдено {FileCount} файлов в директории {Directory}", rootFiles.Count, directoryPath);

      var currentProgress2 = (double)Interlocked.Increment(ref processedDirs) / totalDirs;
      progress?.Report(currentProgress2 * phaseWeight * 100);

      _logger.LogDebug("Всего найдено файлов: {TotalFiles}", allFiles.Count);
      return [.. allFiles];
    }

    public List<string> GetDirectories(string root, CancellationToken cancellationToken = default)
    {
      var result = new List<string>();
      var options = new EnumerationOptions
      {
        IgnoreInaccessible = true,
        RecurseSubdirectories = false
      };

      var directories = new Queue<string>();
      directories.Enqueue(root);
      int processedCount = 0;

      while (directories.Count > 0)
      {
        cancellationToken.ThrowIfCancellationRequested();

        string currentDir = directories.Dequeue();

        try
        {
          string[] subDirs = Directory.GetDirectories(currentDir, "*", options);

          for (int i = 0; i < subDirs.Length; i++)
          {
            // Проверяем отмену каждые 10 обработанных директорий
            if (processedCount++ % 10 == 0)
              cancellationToken.ThrowIfCancellationRequested();

            string dir = subDirs[i];

            if (ShouldSkipDirectory(dir))
              continue;

            directories.Enqueue(dir);
            result.Add(dir);
          }
        }
        catch (UnauthorizedAccessException ex)
        {
          _logger.LogWarning("Нет доступа к директории {Directory}: {Message}", currentDir, ex.Message);
          continue;
        }
        catch (IOException ex)
        {
          _logger.LogWarning("Ошибка доступа к директории {Directory}: {Message}", currentDir, ex.Message);
          continue;
        }
      }

      return result;
    }

    private static bool ShouldSkipDirectory(string path)
    {
      string[] skipPatterns = ["/proc", "/sys", "/dev", "/run"];
      return skipPatterns.Any(path.StartsWith);
    }

    private List<FileInfo> AddImageFilesFromDirectory(string directoryPath, EnumerationOptions options, CancellationToken cancellationToken)
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
      catch (UnauthorizedAccessException ex)
      {
        _logger.LogWarning("Нет доступа к директории {Directory}: {Message}", directoryPath, ex.Message);
        return [];
      }
      catch (IOException ex)
      {
        _logger.LogWarning("Ошибка доступа к директории {Directory}: {Message}", directoryPath, ex.Message);
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

    public DuplicateFinder(ILogger<DuplicateFinder> logger)
    {
      _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
  }
}
