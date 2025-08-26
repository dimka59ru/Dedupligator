namespace Dedupligator.Services
{
  public static class PathHelper
  {
    public static string NormalizeAndValidateDirectoryPath(string folderPath)
    {
      if (string.IsNullOrWhiteSpace(folderPath))
        throw new ArgumentException("Путь к папке не может быть пустым", nameof(folderPath));

      // Нормализуем путь
      folderPath = Path.GetFullPath(folderPath);

      // Корректируем путь для корней дисков
      if (folderPath.Length == 2 && folderPath[1] == ':')
      {
        // Добавляем недостающий backslash (C: -> C:\)
        folderPath += Path.DirectorySeparatorChar;
      }
      else if (folderPath.Length == 3 && folderPath[1] == ':' && folderPath[2] == '\\')
      {
        // Это уже корректный путь к диску (C:\), ничего не меняем
      }
      else
      {
        // Для обычных путей убеждаемся, что заканчивается на directory separator
        if (!folderPath.EndsWith(Path.DirectorySeparatorChar.ToString()) &&
            !folderPath.EndsWith(Path.AltDirectorySeparatorChar.ToString()))
        {
          folderPath += Path.DirectorySeparatorChar;
        }
      }

      // Проверяем существование директории
      if (!Directory.Exists(folderPath))
        throw new DirectoryNotFoundException($"Директория не найдена: {folderPath}");

      return folderPath;
    }
  }
}
