using Avalonia.Media;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using Dedupligator.App.Helpers;
using System.Threading.Tasks;

namespace Dedupligator.App.ViewModels
{
  public partial class ImagePreviewViewModel : ViewModelBase
  {
    [ObservableProperty]
    private Bitmap? _imagePreview;

    [ObservableProperty]
    private string? _resolution;

    public string FileName { get; }
    public string FilePath { get; }
    public string FileSize { get; }

    public async Task LoadImageAsync(int maxWidth = 150)
    {
      try
      {
        var dimensions = await ImageHelper.GetImageDimensionsAsync(FilePath);
        Resolution = dimensions != (0, 0) ? $"{dimensions.Width}×{dimensions.Height}" : "?×?";

        var imageInfo = await ImageHelper.LoadImageAsync(FilePath, maxWidth);

        if (imageInfo.Bitmap != null)
        {
          ImagePreview = imageInfo.Bitmap;
        }
      }
      catch
      {
        Resolution = "Loading error";
      }
    }

    public ImagePreviewViewModel(string fileName, string filePath, string fileSize)
    {
      System.ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
      System.ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
      System.ArgumentException.ThrowIfNullOrWhiteSpace(fileSize);

      ImagePreview = ImageHelper.CreatePlaceholderWithGraphics(100, 100, Colors.LightGray);
      Resolution = "Loading...";
      FileName = fileName;
      FilePath = filePath;
      FileSize = fileSize;
    }
  }
}
