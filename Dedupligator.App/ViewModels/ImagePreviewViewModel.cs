using Avalonia.Media;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using Dedupligator.App.Helpers;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Dedupligator.App.ViewModels
{
  public partial class ImagePreviewViewModel : ViewModelBase
  {
    private readonly ILogger<ImagePreviewViewModel> _logger;

    [ObservableProperty]
    private Bitmap? _imagePreview;

    [ObservableProperty]
    private string? _resolution;

    [ObservableProperty]
    private bool _markedForDeletion;

    public string FileName { get; }
    public string FilePath { get; }
    public string FileSize { get; }

    public async Task LoadImageAsync(int maxWidth = 150)
    {
      try
      {
        _logger.LogDebug("Загрузка изображения: {FileName}", FileName);

        var dimensions = await ImageHelper.GetImageDimensionsAsync(FilePath);
        Resolution = dimensions != (0, 0) ? $"{dimensions.Width}×{dimensions.Height}" : "?×?";

        var imageInfo = await ImageHelper.LoadImageAsync(FilePath, maxWidth);

        if (imageInfo.Bitmap != null)
        {
          _logger.LogDebug("Изображение загружено: {FileName}, размер: {Width}x{Height}",
              FileName, imageInfo.Bitmap.Size.Width, imageInfo.Bitmap.Size.Height);
          ImagePreview = imageInfo.Bitmap;
        }
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Ошибка загрузки изображения: {FileName}", FileName);
        Resolution = "Ошибка загрузки";
      }
    }

    public ImagePreviewViewModel(string fileName, string filePath, string fileSize, ILogger<ImagePreviewViewModel> logger)
    {
      System.ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
      System.ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
      System.ArgumentException.ThrowIfNullOrWhiteSpace(fileSize);

      _logger = logger ?? throw new ArgumentNullException(nameof(logger));

      ImagePreview = ImageHelper.CreatePlaceholderWithGraphics(100, 100, Colors.LightGray);
      Resolution = "Loading...";
      FileName = fileName;
      FilePath = filePath;
      FileSize = fileSize;

      _logger.LogDebug("Создан ImagePreviewViewModel для файла: {FileName}", fileName);
    }
  }
}
