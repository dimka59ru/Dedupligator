using Avalonia.Platform.Storage;
using Microsoft.Extensions.Logging;
using System;
using System.IO;

namespace Dedupligator.App.Helpers
{
  public static class StorageExtensions
  {
    public static string? GetSafeLocalPath(this IStorageFolder folder, ILogger? logger = null)
    {
      try
      {
        if (folder.TryGetLocalPath() is string localPath && !string.IsNullOrEmpty(localPath))
        {
          return EnsureDirectoryPath(localPath);
        }

        // Опционально: fallback для edge-кейсов (например, корень диска)
        if (folder.Name?.Length == 2 && folder.Name.EndsWith(':'))
        {
          return folder.Name + Path.DirectorySeparatorChar;
        }

        logger?.LogWarning("Failed to get local path from IStorageFolder");
        return null;
      }
      catch (Exception ex)
      {
        logger?.LogError(ex, "Error getting path from IStorageFolder");
        return null;
      }
    }

    private static string EnsureDirectoryPath(string path)
    {
      var separator = Path.DirectorySeparatorChar.ToString();
      if (!path.EndsWith(separator, StringComparison.Ordinal))
      {
        return path + separator;
      }
      return path;
    }
  }
}
