using Dedupligator.Common.Helpers;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;

namespace Dedupligator.Common.Models
{
  public record ImageInfo(
       string FileName,
       string FilePath,
       string FileSize,
       string Dimensions,
       string CameraModel = "",
       string DateTaken = "",
       string Exposure = ""
   )
  {
    public bool HasExif => !string.IsNullOrEmpty(CameraModel) ||
                          !string.IsNullOrEmpty(DateTaken) ||
                          !string.IsNullOrEmpty(Exposure);

    public static ImageInfo CreateBasic(string filePath, long fileSize, double width, double height)
    {
      return new ImageInfo(
          FileName: Path.GetFileName(filePath),
          FilePath: filePath,
          FileSize: fileSize.ToFileSizeString(),
          Dimensions: $"{width} × {height}"
      );
    }

    public static ImageInfo CreateWithExif(ImageInfo baseInfo, ExifProfile exifProfile)
    {
      var make = exifProfile.Values.FirstOrDefault(x => x.Tag == ExifTag.Make)?.GetValue()?.ToString();
      var model = exifProfile.Values.FirstOrDefault(x => x.Tag == ExifTag.Model)?.GetValue()?.ToString();
      var dateTaken = exifProfile.Values.FirstOrDefault(x => x.Tag == ExifTag.DateTime)?.GetValue()?.ToString();
      var exposure = GetExposureString(exifProfile);

      return baseInfo with
      {
        CameraModel = $"{make} {model}".Trim(),
        DateTaken = dateTaken ?? string.Empty,
        Exposure = exposure
      };
    }

    public static string GetExposureString(ExifProfile exifProfile)
    {
      if (exifProfile == null)
        return "f/--, --, ISO --";

      var parts = new List<string>();

      // F-Number
      if (exifProfile.TryGetValue(ExifTag.FNumber, out var fNumberEntry) &&
          fNumberEntry.GetValue() is Rational fNumber &&
          fNumber.Denominator != 0)
      {
        var aperture = (double)fNumber.Numerator / fNumber.Denominator;
        parts.Add($"f/{aperture:F1}");
      }
      else
      {
        parts.Add("f/--");
      }

      // Exposure Time
      if (exifProfile.TryGetValue(ExifTag.ExposureTime, out var exposureEntry) &&
          exposureEntry.GetValue() is Rational exposure &&
          exposure.Denominator != 0)
      {
        var exposureTime = (double)exposure.Numerator / exposure.Denominator;
        var exposureString = exposureTime >= 1 ? $"{exposureTime:F0}s" : $"1/{(int)(1 / exposureTime)}s";
        parts.Add(exposureString);
      }
      else
      {
        parts.Add("--");
      }

      // ISO
      parts.Add(GetIsoValue(exifProfile));

      return string.Join(", ", parts);
    }

    private static string GetIsoValue(ExifProfile exifProfile)
    {
      if (exifProfile.TryGetValue(ExifTag.ISOSpeedRatings, out var isoEntry))
      {
        if (isoEntry.GetValue() is ushort[] isoArray && isoArray.Length > 0)
        {
          return $"ISO {isoArray[0]}";
        }
        else
        {
          return $"ISO {isoEntry.GetValue()}";
        }
      }

      return "ISO --";
    }
  }
}
