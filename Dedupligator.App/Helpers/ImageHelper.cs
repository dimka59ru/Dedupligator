using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Dedupligator.App.Models;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Dedupligator.App.Helpers
{
  public static class ImageHelper
  {
    public static async Task<ImageLoadResult> LoadImageAsync(string filePath, int maxWidth = 150)
    {
      var result = new ImageLoadResult();

      if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
      {
        result.Bitmap = CreatePlaceholderWithGraphics(100, 100, Colors.LightGray);
        return result;
      }

      try
      {
        // 1. Получаем размеры БЕЗ загрузки всего изображения (быстро и экономично)
        var dimensions = await GetImageDimensionsAsync(filePath);
        result.Width = dimensions.Width;
        result.Height = dimensions.Height;
        result.IsSuccess = true;

        // 2. Загружаем только превью для отображения
        await using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);

        result.Bitmap = await Task.Run(() =>
                Bitmap.DecodeToWidth(fileStream, maxWidth)
                ?? CreatePlaceholderWithGraphics(100, 100, Colors.LightGray)
            );
      }
      catch
      {
        result.Bitmap = CreatePlaceholderWithGraphics(100, 100, Colors.LightGray);
      }

      return result;
    }

    public static async Task<(int Width, int Height)> GetImageDimensionsAsync(string filePath)
    {
      if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
        return (0, 0);

      try
      {
        // Используем IdentifyAsync для быстрого получения метаданных
        using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        var imageInfo = await SixLabors.ImageSharp.Image.IdentifyAsync(fileStream);
        return imageInfo != null ? (imageInfo.Width, imageInfo.Height) : (0, 0);
      }
      catch
      {
        return (0, 0);
      }
    }

    public static Bitmap CreatePlaceholderWithGraphics(int width, int height, Color color)
    {
      var renderTarget = new RenderTargetBitmap(new PixelSize(width, height));

      using (var context = renderTarget.CreateDrawingContext())
      {
        // Фон
        var backgroundBrush = new SolidColorBrush(color);
        context.FillRectangle(backgroundBrush, new Rect(0, 0, width, height));

        // Простая иконка вместо текста (крестик)
        var iconBrush = new SolidColorBrush(Colors.Gray);
        var pen = new Pen(iconBrush, 2);

        var margin = Math.Min(width, height) * 0.2;
        var x1 = margin;
        var y1 = margin;
        var x2 = width - margin;
        var y2 = height - margin;

        // Рисуем крестик
        context.DrawLine(pen, new Point(x1, y1), new Point(x2, y2));
        context.DrawLine(pen, new Point(x1, y2), new Point(x2, y1));
      }

      return renderTarget;
    }
  }
}
