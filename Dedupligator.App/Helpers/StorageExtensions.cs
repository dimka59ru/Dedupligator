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
        if (folder.TryGetLocalPath() is string localPath && !string.IsNullOrEmpty(localPath))
        {
          return EnsureDirectoryPath(localPath);
        }

        // Опционально: fallback для edge-кейсов (например, корень диска)
        if (folder.Name?.Length == 2 && folder.Name.EndsWith(':'))
        {
          return folder.Name + Path.DirectorySeparatorChar;
        }

        return null;
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Error getting path from IStorageFolder: {ex.Message}");
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
