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
        result.Bitmap = CreateLightGrayPlaceholderWithGraphics();
        return result;
      }

      try
      {
        var info = await GetImageInfoAsync(filePath);
        if (!info.IsSuccess)
        {
          result.Bitmap = CreateLightGrayPlaceholderWithGraphics();
          return result;
        }

        result.Width = info.Width;
        result.Height = info.Height;
        result.IsSuccess = true;

        // 2. Загружаем только превью для отображения
        await using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);

        result.Bitmap = await Task.Run(() =>
                Bitmap.DecodeToWidth(fileStream, maxWidth)
                ?? CreateLightGrayPlaceholderWithGraphics()
            );
      }
      catch
      {
        result.Bitmap = CreateLightGrayPlaceholderWithGraphics();
      }

      return result;
    }

    public static async Task<(double Width, double Height)> GetImageDimensionsAsync(string filePath)
    {
      var result = await GetImageInfoAsync(filePath);
      return (result.Width, result.Height);
    }

    public static Bitmap CreateLightGrayPlaceholderWithGraphics()
    {
      return CreatePlaceholderWithGraphics(100, 100, Colors.LightGray);
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

    public static async Task<ImageInfoResult> GetImageInfoAsync(string filePath)
    {
      if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
        return ImageInfoResult.Fail;

      try
      {
        await using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);

        var imageInfo = await SixLabors.ImageSharp.Image.IdentifyAsync(stream);
        if (imageInfo is null)
          return ImageInfoResult.Fail;

        return new ImageInfoResult(true, imageInfo.Width, imageInfo.Height);
      }
      catch (Exception ex) when (
          ex is SixLabors.ImageSharp.InvalidImageContentException ||
          ex is NotSupportedException ||
          ex is IOException)
      {
        return ImageInfoResult.Fail;
      }
    }
  }  
}
