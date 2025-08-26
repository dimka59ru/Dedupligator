using Avalonia.Platform.Storage;
using System;
using System.IO;

namespace Dedupligator.App.Helpers
{
  public static class StorageExtensions
  {
    public static string? GetSafeLocalPath(this IStorageFolder folder)
    {
      try
      {
        // Первый способ: через TryGetLocalPath
        if (folder.TryGetLocalPath() is string localPath && !string.IsNullOrEmpty(localPath))
        {
          return EnsureDirectoryPath(localPath);
        }

        // Второй способ: через Path
        if (folder.Path is { IsAbsoluteUri: true } uriPath)
        {
          var path = uriPath.LocalPath;
          return EnsureDirectoryPath(path);
        }

        // Третий способ: для корней дисков
        if (folder.Name is string name && name.Length == 2 && name.EndsWith(":"))
        {
          return name + "\\";
        }

        return null;
      }
      catch (Exception ex)
      {
        // Логируем ошибку, но не падаем
        Console.WriteLine($"Error getting path from IStorageFolder: {ex.Message}");
        return null;
      }
    }

    private static string EnsureDirectoryPath(string path)
    {
      // Убеждаемся, что путь заканчивается на directory separator
      if (!path.EndsWith(Path.DirectorySeparatorChar.ToString()) &&
          !path.EndsWith(Path.AltDirectorySeparatorChar.ToString()))
      {
        return path + Path.DirectorySeparatorChar;
      }
      return path;
    }
  }
}
