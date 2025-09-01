using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Dedupligator.Services.DuplicateFinders
{
  /// <summary>
  /// Класс для грубой группировки изображений по визуальным характеристикам
  /// Устойчив к ресайзу, цветокоррекции и небольшим изменениям
  /// </summary>
  public static class RoughGrouper
  {
    private const int THUMBNAIL_SIZE = 16;
    private const int BRIGHTNESS_BUCKETS = 8;
    private const int COLOR_BUCKETS = 4;
    private const int ASPECT_RATIO_BUCKETS = 5;

    /// <summary>
    /// Создает грубый ключ группировки для изображения
    /// </summary>
    /// <param name="imagePath">Путь к файлу изображения</param>
    /// <returns>Ключ группировки в формате "aspect_brightness_color"</returns>
    public static string CreateGroupKey(string imagePath)
    {
      try
      {
        using var image = Image.Load<Rgba32>(imagePath);
        return CreateGroupKey(image);
      }
      catch
      {
        return "error_0_0_0";
      }
    }

    /// <summary>
    /// Создает ключ группировки из уже загруженного изображения
    /// </summary>
    public static string CreateGroupKey(Image<Rgba32> image)
    {
      var aspectKey = GetAspectRatioKey(image.Width, image.Height);
      var brightnessKey = GetBrightnessKey(image);
      var colorKey = GetColorKey(image);

      return $"{aspectKey}_{brightnessKey}_{colorKey}";
    }

    /// <summary>
    /// Ключ на основе соотношения сторон (устойчив к ресайзу)
    /// </summary>
    private static string GetAspectRatioKey(int width, int height)
    {
      double aspectRatio = (double)width / height;

      int bucket = aspectRatio switch
      {
        < 0.7 => 0,   // Вертикальные (портретные)
        < 0.9 => 1,   // Почти квадратные вертикальные
        < 1.1 => 2,   // Квадратные
        < 1.5 => 3,   // Почти квадратные горизонтальные
        _ => 4         // Горизонтальные (ландшафтные)
      };

      return Math.Min(bucket, ASPECT_RATIO_BUCKETS - 1).ToString();
    }

    /// <summary>
    /// Ключ на основе усредненной яркости (устойчив к цветокоррекции)
    /// </summary>
    private static string GetBrightnessKey(Image<Rgba32> image)
    {
      using var grayscale = image.Clone();
      grayscale.Mutate(x => x.Resize(THUMBNAIL_SIZE, THUMBNAIL_SIZE).Grayscale());

      long totalBrightness = 0;

      for (int y = 0; y < grayscale.Height; y++)
      {
        for (int x = 0; x < grayscale.Width; x++)
        {
          totalBrightness += grayscale[x, y].R;
        }
      }

      int avgBrightness = (int)(totalBrightness / (grayscale.Width * grayscale.Height));
      int brightnessBucket = avgBrightness / (256 / BRIGHTNESS_BUCKETS);

      return Math.Min(brightnessBucket, BRIGHTNESS_BUCKETS - 1).ToString();
    }

    /// <summary>
    /// Упрощенная цветовая сигнатура (устойчива к ресайзу)
    /// </summary>
    private static string GetColorKey(Image<Rgba32> image)
    {
      using var resized = image.Clone();
      resized.Mutate(x => x.Resize(THUMBNAIL_SIZE, THUMBNAIL_SIZE));

      int[] colorBuckets = new int[COLOR_BUCKETS];

      for (int y = 0; y < resized.Height; y++)
      {
        for (int x = 0; x < resized.Width; x++)
        {
          var pixel = resized[x, y];

          int maxChannel = Math.Max(Math.Max(pixel.R, pixel.G), pixel.B);
          int minChannel = Math.Min(Math.Min(pixel.R, pixel.G), pixel.B);
          int saturation = maxChannel - minChannel;

          if (saturation < 30)
            colorBuckets[0]++; // Grayscale
          else if (pixel.R > pixel.G + 20 && pixel.R > pixel.B + 20)
            colorBuckets[1]++; // Красные тона
          else if (pixel.G > pixel.R + 20 && pixel.G > pixel.B + 20)
            colorBuckets[2]++; // Зеленые тона
          else
            colorBuckets[3]++; // Синие/смешанные тона
        }
      }

      int dominantBucket = 0;
      for (int i = 1; i < COLOR_BUCKETS; i++)
      {
        if (colorBuckets[i] > colorBuckets[dominantBucket])
          dominantBucket = i;
      }

      return dominantBucket.ToString();
    }
  }
}
