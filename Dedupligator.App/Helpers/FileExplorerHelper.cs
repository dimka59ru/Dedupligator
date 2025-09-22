using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Dedupligator.App.Helpers
{
  public class FileExplorerHelper
  {
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

        if (OperatingSystem.IsWindows())
        {
          Process.Start("explorer.exe", $"/select,\"{filePath}\"");
        }
        else
        {
          // Альтернатива для других ОС
          Process.Start(new ProcessStartInfo
          {
            FileName = directoryPath,
            UseShellExecute = true
          });
        }
      }
      catch (Exception ex)
      {
        await logError($"Ошибка при открытии папки:\n{ex.Message}");
      }
    }
  }
}
