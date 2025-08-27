namespace Dedupligator.App.Helpers
{
  public static class FileSizeExtensions
  {
    public static string ToFileSizeString(this long bytes)
    {
      string[] sizes = { "B", "KB", "MB", "GB" };
      int order = 0;
      double len = bytes;

      while (len >= 1024 && order < sizes.Length - 1)
      {
        order++;
        len /= 1024;
      }

      return $"{len:0.##} {sizes[order]}";
    }
  }
}
