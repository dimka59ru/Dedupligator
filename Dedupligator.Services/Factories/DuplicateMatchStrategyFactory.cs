using Dedupligator.Services.DuplicateFinders;
using System.Collections.Concurrent;

namespace Dedupligator.Services.Factories
{
  public sealed class DuplicateMatchStrategyFactory : IDuplicateMatchStrategyFactory, IDisposable
  {
    private readonly ConcurrentDictionary<float, NeuralSimilarityStrategy> _neuralCache = new();
    private readonly Lazy<ExactMatchStrategy> _exactMatchStrategy = new(() => new ExactMatchStrategy());
    private readonly Lazy<SimilarImageStrategy> _similarImageStrategy = new(() => new SimilarImageStrategy());
    private bool _disposed;

    public IDuplicateMatchStrategy CreateExactMatchStrategy() => _exactMatchStrategy.Value;

    public IDuplicateMatchStrategy CreateSimilarImageStrategy() => _similarImageStrategy.Value;

    public IDuplicateMatchStrategy CreateNeuralSimilarityStrategy(float threshold)
    {
      ObjectDisposedException.ThrowIf(_disposed, this);
      return _neuralCache.GetOrAdd(threshold, t => new NeuralSimilarityStrategy(t));
    }

    public void Dispose()
    {
      if (_disposed)
        return;

      foreach (var strategy in _neuralCache.Values)
      {
        strategy.Dispose();
      }

      _neuralCache.Clear();
      _disposed = true;
    }
  }
}
