using Dedupligator.Services.Hashes;

namespace Dedupligator.Services.DuplicateFinders
{
  /// <summary>
  /// Стратегия для поиска точных дубликатов по хэшу содержимого.
  /// </summary>
  public class ExactMatchStrategy(IHashService hashService) : IDuplicateMatchStrategy
  {
    private readonly IHashService _hashService = hashService ?? throw new ArgumentNullException(nameof(hashService));

    public bool RequiresPreGrouping => true;

    public Func<FileInfo, object> GroupingKeySelector => file => file.Length;

    public bool AreDuplicates(FileInfo file1, FileInfo file2)
    {

#pragma warning disable S1135 // Track uses of "TODO" tags
                             // TODO: не плохо бы иметь кэш, чтобы не вычислять постоянно хэши.
      return _hashService.CompareHashes(file1, file2);
#pragma warning restore S1135 // Track uses of "TODO" tags
    }
  }
}
