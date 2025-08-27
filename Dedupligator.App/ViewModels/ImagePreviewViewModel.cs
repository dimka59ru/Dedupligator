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

    public string? FileName { get; set; }
    public string? FilePath { get; set; }
    public string? FileSize { get; set; }

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

    public ImagePreviewViewModel()
    {
      ImagePreview = ImageHelper.CreatePlaceholderWithGraphics(100, 100, Colors.LightGray);
      Resolution = "Loading...";
    }
  }
}
