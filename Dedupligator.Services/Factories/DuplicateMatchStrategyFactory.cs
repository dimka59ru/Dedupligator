using Dedupligator.Services.DuplicateFinders;
using System.Collections.Concurrent;

namespace Dedupligator.Services.Factories
{
  public class DuplicateMatchStrategyFactory : IDuplicateMatchStrategyFactory
  {
    private readonly ConcurrentDictionary<float, NeuralSimilarityStrategy> _neuralCache = new();
    private readonly Lazy<ExactMatchStrategy> _exactMatchStrategy = new(() => new ExactMatchStrategy());
    private readonly Lazy<SimilarImageStrategy> _similarImageStrategy = new(() => new SimilarImageStrategy());

    public IDuplicateMatchStrategy CreateExactMatchStrategy() => _exactMatchStrategy.Value;

    public IDuplicateMatchStrategy CreateSimilarImageStrategy() => _similarImageStrategy.Value;

    public IDuplicateMatchStrategy CreateNeuralSimilarityStrategy(float threshold)
    {
      return _neuralCache.GetOrAdd(threshold, t => new NeuralSimilarityStrategy(t));
    }
  }
}
