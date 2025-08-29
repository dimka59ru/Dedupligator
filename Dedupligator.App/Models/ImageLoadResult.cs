using Avalonia.Media.Imaging;

namespace Dedupligator.App.Models
{
  public class ImageInfoResult(bool isSuccess, double width, double height)
  {
    public static readonly ImageInfoResult Fail = new(false, 0, 0);

    public double Width { get; set; } = width;
    public double Height { get; set; } = height;
    public bool IsSuccess { get; set; } = isSuccess;
  }

  public class ImageLoadResult(bool isSuccess, double width, double height) : ImageInfoResult(isSuccess, width, height)
  {
    public Bitmap? Bitmap { get; set; }

    public ImageLoadResult() : this(false, 0, 0)
    {      
    }
  }
}
