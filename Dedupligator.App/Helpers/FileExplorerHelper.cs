using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Dedupligator.App.Helpers
{
  public class FileExplorerHelper
  {
    public static async Task OpenFolderAsync(string folderPath, Func<string, Task> logError)
    {
      if (string.IsNullOrEmpty(folderPath))
      {
        await logError("Путь к папке не указан");
        return;
      }

      if (!Directory.Exists(folderPath))
      {
        await logError($"Папка не существует:\n{folderPath}");
        return;
      }

      try
      {
        OpenInExplorer(folderPath);
      }
      catch (Exception ex)
      {
        await logError($"Ошибка при открытии папки:\n{ex.Message}");
      }
    }

    public static async Task OpenFolderWithFileAsync(string filePath, Func<string, Task> logError)
    {
      if (string.IsNullOrEmpty(filePath))
      {
        await logError("Путь к файлу не указан");
        return;
      }

      if (!File.Exists(filePath))
      {
        await logError($"Файл не существует:\n{filePath}");
        return;
      }

      try
      {
        var directoryPath = Path.GetDirectoryName(filePath);
        if (string.IsNullOrEmpty(directoryPath))
        {
          await logError($"Не удалось определить папку для файла:\n{filePath}");
          return;
        }

        OpenInExplorer(directoryPath, filePath);
      }
      catch (Exception ex)
      {
        await logError($"Ошибка при открытии папки:\n{ex.Message}");
      }
    }

    private static void OpenInExplorer(string folderPath, string? selectedFilePath = null)
    {
      if (OperatingSystem.IsWindows())
      {
        var arguments = selectedFilePath is null
          ? $"\"{folderPath}\""
          : $"/select,\"{selectedFilePath}\"";

        Process.Start("explorer.exe", arguments);
        return;
      }

      Process.Start(new ProcessStartInfo
      {
        FileName = folderPath,
        UseShellExecute = true
      });
    }
  }
}
