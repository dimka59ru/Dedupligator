using Dedupligator.Services.DuplicateFinders;

namespace Dedupligator.Services.Factories
{
  public interface IDuplicateMatchStrategyFactory
  {
    IDuplicateMatchStrategy CreateExactMatchStrategy();
    IDuplicateMatchStrategy CreateSimilarImageStrategy();
    IDuplicateMatchStrategy CreateNeuralSimilarityStrategy(float threshold);
  }
}
