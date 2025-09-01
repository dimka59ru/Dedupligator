using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Dedupligator.Services.Hash
{
  /// <summary>
  /// Вычисляет перцептивный хэш (pHash) для изображений
  /// </summary>
  public static class PHashCalculator
  {
    private const int TARGET_SIZE = 32;
    private const int DCT_SIZE = 32;
    private const int LOW_FREQUENCY_SIZE = 8;
    private const int HASH_BIT_LENGTH = 64;
    private const int SIMILARITY_THRESHOLD = 10;
    private const double SQRT_2 = 1.4142135623730950; // Math.Sqrt(2.0)

    /// <summary>
    /// Вычисляет перцептивный хэш для указанного изображения
    /// </summary>
    /// <param name="imagePath">Путь к файлу изображения</param>
    /// <returns>64-битный перцептивный хэш</returns>
    public static ulong CalculatePHash(string imagePath)
    {
      using var image = Image.Load<L8>(imagePath);
      using var processedImage = PreprocessImage(image);
      var dctCoefficients = CalculateDCT(processedImage);
      double mean = CalculateMean(dctCoefficients);
      return GenerateHash(dctCoefficients, mean);
    }

    /// <summary>
    /// Предварительная обработка изображения: изменение размера и преобразование в grayscale
    /// </summary>
    private static Image<L8> PreprocessImage(Image<L8> image)
    {
      var processed = image.Clone();
      processed.Mutate(x => x.Resize(TARGET_SIZE, TARGET_SIZE).Grayscale());
      return processed;
    }

    /// <summary>
    /// Вычисляет матрицу коэффициентов ДКП для изображения
    /// </summary>
    private static double[,] CalculateDCT(Image<L8> image)
    {
      var pixels = new double[DCT_SIZE, DCT_SIZE];

      for (int y = 0; y < DCT_SIZE; y++)
        for (int x = 0; x < DCT_SIZE; x++)
          pixels[x, y] = image[x, y].PackedValue / 255.0;

      var dct = new double[DCT_SIZE, DCT_SIZE];
      double sqrt2onN = Math.Sqrt(2.0 / DCT_SIZE);

      for (int u = 0; u < DCT_SIZE; u++)
      {
        double cu = u == 0 ? 1.0 / SQRT_2 : 1.0;

        for (int v = 0; v < DCT_SIZE; v++)
        {
          double cv = v == 0 ? 1.0 / SQRT_2 : 1.0;
          double sum = 0.0;

          for (int x = 0; x < DCT_SIZE; x++)
          {
            double cosU = Math.Cos(((2 * x + 1) * u * Math.PI) / (2 * DCT_SIZE));

            for (int y = 0; y < DCT_SIZE; y++)
            {
              double cosV = Math.Cos(((2 * y + 1) * v * Math.PI) / (2 * DCT_SIZE));
              sum += pixels[x, y] * cosU * cosV;
            }
          }

          dct[u, v] = cu * cv * sqrt2onN * sum;
        }
      }

      return dct;
    }

    /// <summary>
    /// Вычисляет среднее значение низкочастотных коэффициентов ДКП (исключая DC-компоненту)
    /// </summary>
    private static double CalculateMean(double[,] dct)
    {
      double sum = 0;
      int count = 0;

      for (int i = 0; i < LOW_FREQUENCY_SIZE; i++)
      {
        for (int j = 0; j < LOW_FREQUENCY_SIZE; j++)
        {
          if (i == 0 && j == 0) continue; // Пропускаем DC-компоненту
          sum += dct[i, j];
          count++;
        }
      }

      return sum / count;
    }

    /// <summary>
    /// Генерирует 64-битный хэш на основе сравнения коэффициентов со средним значением
    /// </summary>
    private static ulong GenerateHash(double[,] dct, double mean)
    {
      ulong hash = 0;
      int bitPosition = 0;

      for (int i = 0; i < LOW_FREQUENCY_SIZE; i++)
      {
        for (int j = 0; j < LOW_FREQUENCY_SIZE; j++)
        {
          if (i == 0 && j == 0) continue; // Пропускаем DC-компоненту

          if (dct[i, j] >= mean)
          {
            hash |= (1UL << bitPosition);
          }
          bitPosition++;
        }
      }

      return hash;
    }

    /// <summary>
    /// Конвертирует 64-битный хэш в бинарную строку
    /// </summary>
    /// <param name="hash">64-битный перцептивный хэш</param>
    /// <returns>Бинарная строка из 64 символов</returns>
    public static string HashToString(ulong hash)
    {
      return Convert.ToString((long)hash, 2).PadLeft(HASH_BIT_LENGTH, '0');
    }

    /// <summary>
    /// Вычисляет расстояние Хэмминга между двумя перцептивными хэшами
    /// </summary>
    /// <param name="hash1">Первый хэш</param>
    /// <param name="hash2">Второй хэш</param>
    /// <returns>Расстояние Хэмминга (0-64)</returns>
    public static int HammingDistance(ulong hash1, ulong hash2)
    {
      return BitCount(hash1 ^ hash2);
    }

    /// <summary>
    /// Проверяет, являются ли два изображения визуально похожими на основе их хэшей
    /// </summary>
    /// <param name="hash1">Первый хэш</param>
    /// <param name="hash2">Второй хэш</param>
    /// <returns>True если изображения похожи (расстояние Хэмминга ≤ порогу)</returns>
    public static bool AreImagesSimilar(ulong hash1, ulong hash2)
    {
      return HammingDistance(hash1, hash2) <= SIMILARITY_THRESHOLD;
    }

    /// <summary>
    /// Подсчитывает количество установленных битов в 64-битном числе
    /// </summary>
    private static int BitCount(ulong value)
    {
      int count = 0;
      while (value != 0)
      {
        count++;
        value &= value - 1;
      }
      return count;
    }
  }
}
