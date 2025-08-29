namespace Dedupligator.Services
{
  public static class PathHelper
  {
    public static string NormalizeAndValidateDirectoryPath(string folderPath)
    {
      if (string.IsNullOrWhiteSpace(folderPath))
        throw new ArgumentException("Путь к папке не может быть пустым", nameof(folderPath));

      // Нормализуем путь
      string fullPath;
      try
      {
        fullPath = Path.GetFullPath(folderPath);
      }
      catch (Exception ex) when (
          ex is ArgumentException ||
          ex is PathTooLongException ||
          ex is NotSupportedException)
      {
        throw new ArgumentException($"Некорректный путь: {folderPath}", nameof(folderPath), ex);
      }

      return fullPath;
    }
  }
}
