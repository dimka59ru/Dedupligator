using Avalonia.Media.Imaging;

namespace Dedupligator.App.Models
{
  public class ImageLoadResult
  {
    public Bitmap? Bitmap { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public bool IsSuccess { get; set; }
  }
}
