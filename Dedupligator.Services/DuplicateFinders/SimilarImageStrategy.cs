using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Numerics;

namespace Dedupligator.Services.DuplicateFinders
{
  public class SimilarImageStrategy : IDuplicateMatchStrategy
  {
    private readonly int _threshold = 5; // Максимальное расстояние Хэмминга

    public Func<FileInfo, object> GroupingKeySelector => file =>
    {
      try
      {
        var roughHash = ComputeRoughPhash(file); // 16-битный хеш
        return roughHash ?? (ushort)0;
      }
      catch
      {
        return (ushort)0;
      }
    };

    public bool RequiresPreGrouping => true;

    public bool AreDuplicates(FileInfo file1, FileInfo file2)
    {
      var hash1 = ComputeHash(file1);
      var hash2 = ComputeHash(file2);

      if (hash1 != null && hash2 != null)
      {
        int distance = HammingDistance(hash1.Value, hash2.Value);
        if (distance <= _threshold)
          return true;
      }
      
      return false;
    }

    private const int HashSize = 8; // 8x8 = 64 бита

    private static ulong? ComputeHash(FileInfo file)
    {
      try
      {
        using var stream = file.OpenRead();
        using var image = Image.Load<Rgba32>(stream);
        image.Mutate(x => x.Resize(HashSize, HashSize, KnownResamplers.Bicubic));

        var luminance = new byte[HashSize * HashSize];

        image.ProcessPixelRows(accessor =>
        {
          int index = 0;
          for (int y = 0; y < accessor.Height; y++)
          {
            Span<Rgba32> pixelRow = accessor.GetRowSpan(y);
            for (int x = 0; x < pixelRow.Length; x++)
            {
              Rgba32 pixel = pixelRow[x]; // копируем пиксель
              luminance[index++] = (byte)(0.299f * pixel.R + 0.587f * pixel.G + 0.114f * pixel.B);
            }
          }
        });

        // Находим медиану яркости
        var sorted = (byte[])luminance.Clone();
        Array.Sort(sorted);
        byte median = sorted[32]; // 64 элемента → медиана между 32 и 33

        // Формируем хеш: 1, если ярче медианы
        ulong hash = 0;
        for (int i = 0; i < 64; i++)
        {
          if (luminance[i] > median)
            hash |= (1UL << i);
        }

        return hash;
      }
      catch (Exception)
      {
        // Ошибки загрузки (битые файлы и т.п.)
        return 0;
      }
    }

    private static int HammingDistance(ulong hash1, ulong hash2)
    {
      return BitOperations.PopCount(hash1 ^ hash2);
    }


    private const int ResizeSize = 16;  // Уменьшаем до 16x16
    private const int GridSize = 4;     // Делим на сетку 4x4 → 16 блоков
    private const int BlockSize = ResizeSize / GridSize; // 4 пикселя на блок


    /// <summary>
    /// Вычисляет грубый 16-битный хеш для группировки.
    /// </summary>
    /// <param name="file">Файл изображения.</param>
    /// <returns>16-битный хеш или null при ошибке.</returns>
    private static ushort? ComputeRoughPhash(FileInfo file)
    {
      try
      {
        using var stream = file.OpenRead();
        using var image = Image.Load<Rgba32>(stream);
        image.Mutate(x => x.Resize(ResizeSize, ResizeSize, KnownResamplers.Bicubic));

        // Шаг 1: Получаем яркость всех 256 пикселей
        var luminance = new byte[ResizeSize * ResizeSize];
        int idx = 0;

        image.ProcessPixelRows(accessor =>
        {
          for (int y = 0; y < ResizeSize; y++)
          {
            var row = accessor.GetRowSpan(y);
            for (int x = 0; x < ResizeSize; x++)
            {
              var p = row[x];
              luminance[idx++] = (byte)(0.299f * p.R + 0.587f * p.G + 0.114f * p.B);
            }
          }
        });

        // Шаг 2: Делим на сетку 4x4, считаем среднюю яркость каждого блока
        var blockAverages = new float[GridSize * GridSize];
        for (int by = 0; by < GridSize; by++)
        {
          for (int bx = 0; bx < GridSize; bx++)
          {
            float sum = 0;
            int count = 0;
            for (int dy = 0; dy < BlockSize; dy++)
            {
              for (int dx = 0; dx < BlockSize; dx++)
              {
                int x = bx * BlockSize + dx;
                int y = by * BlockSize + dy;
                sum += luminance[y * ResizeSize + x];
                count++;
              }
            }
            blockAverages[by * GridSize + bx] = sum / count;
          }
        }

        // Шаг 3: Медиана яркости блоков
        var sorted = (float[])blockAverages.Clone();
        Array.Sort(sorted);
        float median = sorted[8]; // 16 элементов → медиана между 8 и 9

        // Шаг 4: Формируем 16-битный хеш
        ushort hash = 0;
        for (int i = 0; i < 16; i++)
        {
          if (blockAverages[i] > median)
            hash |= (ushort)(1 << i);
        }

        return hash;
      }
      catch
      {
        return null;
      }
    }
  }
}
