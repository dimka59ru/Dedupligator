using Dedupligator.Services.DuplicateFinders;
using Dedupligator.Services.Hashes;
using System.IO;

var hashService = new Sha256HashService();
var strategy = new HashMatchStrategy(hashService);
var finder = new DuplicateFinder(strategy);
var duplicateGroups = finder.FindDuplicates("E:\\С Диска 250 Гб");
Console.WriteLine($"Найдено {duplicateGroups.Count} дубликатов.");
