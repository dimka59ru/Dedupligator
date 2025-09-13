using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Collections.Concurrent;

namespace Dedupligator.Services.DuplicateFinders
{
  public class NeuralSimilarityStrategy : IDuplicateMatchStrategy, IDisposable
  {
    private readonly InferenceSession _session;
    private readonly string _inputName;
    private readonly ConcurrentDictionary<string, float[]> _embeddingCache;
    private readonly float _threshold;
    private bool _disposed = false;

    public Func<FileInfo, object> GroupingKeySelector => file =>
    {
      return RoughGrouper.CreateGroupKey(file.FullName);
    };

    public bool RequiresPreGrouping => true;

    public bool AreDuplicates(FileInfo file1, FileInfo file2)
    {
      return AreSimilar(file1, file2, _threshold);
    }

    public bool AreSimilar(FileInfo file1, FileInfo file2, float similarityThreshold = 0.7f)
    {
      try
      {
        var embedding1 = GetCachedEmbedding(file1.FullName);
        var embedding2 = GetCachedEmbedding(file2.FullName);

        var similarity = CalculateCosineSimilarity(embedding1, embedding2);

        return similarity >= similarityThreshold;
      }
      catch (Exception)
      {
        return false;
      }
    }

    private float[] GetCachedEmbedding(string imagePath)
    {
      return _embeddingCache.GetOrAdd(imagePath, path =>
      {
        if (!File.Exists(path))
          throw new FileNotFoundException($"Image file not found: {path}");

        return GetImageEmbedding(path);
      });
    }

    private float[] GetImageEmbedding(string imagePath)
    {
      var imageTensor = LoadAndPreprocessImage(imagePath);

      var inputs = new List<NamedOnnxValue>
      {
        NamedOnnxValue.CreateFromTensor(_inputName, imageTensor)
      };

      using var results = _session.Run(inputs);
      var output = results[0].AsTensor<float>();
      var embedding = output.ToArray();
      return embedding;
    }

    private static DenseTensor<float> LoadAndPreprocessImage(string imagePath)
    {
      using var image = Image.Load<Rgb24>(imagePath);

      var options = new ResizeOptions
      {
        Size = new Size(224, 224),
        Mode = ResizeMode.Crop,
      };

      image.Mutate(x => x.Resize(options));

      var mean = new[] { 0.485f, 0.456f, 0.406f };
      var stddev = new[] { 0.229f, 0.224f, 0.225f };
      DenseTensor<float> processedImage = new([1, 3, 224, 224]);

      image.ProcessPixelRows(accessor =>
      {
        for (int y = 0; y < accessor.Height; y++)
        {
          Span<Rgb24> pixelSpan = accessor.GetRowSpan(y);
          for (int x = 0; x < accessor.Width; x++)
          {
#pragma warning disable S4143 // Collection elements should not be replaced unconditionally
            processedImage[0, 0, y, x] = ((pixelSpan[x].R / 255f) - mean[0]) / stddev[0];
            processedImage[0, 1, y, x] = ((pixelSpan[x].G / 255f) - mean[1]) / stddev[1];
            processedImage[0, 2, y, x] = ((pixelSpan[x].B / 255f) - mean[2]) / stddev[2];
#pragma warning restore S4143 // Collection elements should not be replaced unconditionally
          }
        }
      });

      return processedImage;
    }

    private static float CalculateCosineSimilarity(float[] vector1, float[] vector2)
    {
      float dot = 0, mag1 = 0, mag2 = 0;
      for (int i = 0; i < vector1.Length; i++)
      {
        dot += vector1[i] * vector2[i];
        mag1 += vector1[i] * vector1[i];
        mag2 += vector2[i] * vector2[i];
      }

      var denominator = MathF.Sqrt(mag1) * MathF.Sqrt(mag2);
      return denominator < float.Epsilon ? 0 : dot / denominator;
    }

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
      if (!_disposed)
      {
        _disposed = true;

        if (disposing)
        {
          _session?.Dispose();
        }
      }
    }

    public NeuralSimilarityStrategy(float Threshold)
    {
      var modelPath = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "mobilenetv2-7.onnx");
      if (!File.Exists(modelPath))
        throw new FileNotFoundException($"Model file not found: {modelPath}");

      _session = new InferenceSession(modelPath);
      _inputName = _session.InputMetadata.Keys.First();
      _embeddingCache = [];
      _threshold = Threshold;
    }
  }
}
