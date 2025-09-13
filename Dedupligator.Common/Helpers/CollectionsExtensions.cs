using System.Collections.Concurrent;

namespace Dedupligator.Common.Helpers
{
  public static class CollectionsExtensions
  {
    public static void AddRange<T>(this ConcurrentBag<T> bag, IEnumerable<T> toAdd)
    {
      foreach (var element in toAdd)
      {
        bag.Add(element);
      }
    }
  }
}
