using System;
using System.IO;
using Dedupligator.Common.Constants;

namespace Dedupligator.App.Helpers
{
  public static class FileTrashHelper
  {
    public static string GetTrashRootPath(string scanRootPath)
    {
      ArgumentException.ThrowIfNullOrWhiteSpace(scanRootPath);
      return Path.Combine(Path.TrimEndingDirectorySeparator(scanRootPath), AppFolders.TrashFolderName);
    }

    public static string MoveToTrashFolder(string filePath, string scanRootPath)
    {
      ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
      ArgumentException.ThrowIfNullOrWhiteSpace(scanRootPath);

      var normalizedRoot = Path.GetFullPath(Path.TrimEndingDirectorySeparator(scanRootPath));
      var normalizedFilePath = Path.GetFullPath(filePath);
      var trashRootPath = GetTrashRootPath(normalizedRoot);

      if (!IsPathInsideRoot(normalizedFilePath, normalizedRoot))
      {
        throw new InvalidOperationException(
          $"Файл '{normalizedFilePath}' находится вне выбранной папки '{normalizedRoot}'.");
      }

      if (normalizedFilePath.StartsWith(trashRootPath, StringComparison.OrdinalIgnoreCase))
      {
        return normalizedFilePath;
      }

      var relativePath = Path.GetRelativePath(normalizedRoot, normalizedFilePath);
      var destinationPath = Path.Combine(trashRootPath, relativePath);
      var destinationDirectory = Path.GetDirectoryName(destinationPath)
        ?? throw new InvalidOperationException("Не удалось определить папку для переноса файла.");

      Directory.CreateDirectory(destinationDirectory);
      destinationPath = EnsureUniquePath(destinationPath);
      File.Move(normalizedFilePath, destinationPath);

      return destinationPath;
    }

    public static bool IsInTrashFolder(string filePath, string scanRootPath)
    {
      if (string.IsNullOrWhiteSpace(filePath) || string.IsNullOrWhiteSpace(scanRootPath))
        return false;

      var normalizedRoot = Path.GetFullPath(Path.TrimEndingDirectorySeparator(scanRootPath));
      var trashRootPath = GetTrashRootPath(normalizedRoot);
      var normalizedFilePath = Path.GetFullPath(filePath);

      return IsPathInsideRoot(normalizedFilePath, trashRootPath);
    }

    private static string EnsureUniquePath(string destinationPath)
    {
      if (!File.Exists(destinationPath))
        return destinationPath;

      var directory = Path.GetDirectoryName(destinationPath)
        ?? throw new InvalidOperationException("Не удалось определить папку назначения.");
      var fileName = Path.GetFileNameWithoutExtension(destinationPath);
      var extension = Path.GetExtension(destinationPath);

      for (var index = 1; ; index++)
      {
        var candidate = Path.Combine(directory, $"{fileName} ({index}){extension}");
        if (!File.Exists(candidate))
          return candidate;
      }
    }

    private static bool IsPathInsideRoot(string path, string rootPath)
    {
      var normalizedRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(rootPath));
      var normalizedPath = Path.GetFullPath(path);
      var relativePath = Path.GetRelativePath(normalizedRoot, normalizedPath);

      return relativePath != ".." &&
             !relativePath.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal) &&
             !Path.IsPathRooted(relativePath);
    }
  }
}
