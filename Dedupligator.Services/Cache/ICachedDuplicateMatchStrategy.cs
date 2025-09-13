using Dedupligator.Services.DuplicateFinders;

namespace Dedupligator.Services.Cache
{
  public interface ICachedDuplicateMatchStrategy : IDuplicateMatchStrategy
  {
    void ClearCache();
  }
}
