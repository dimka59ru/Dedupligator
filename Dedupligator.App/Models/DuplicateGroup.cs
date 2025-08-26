using System.Collections.Generic;
using System.IO;

namespace Dedupligator.App.Models
{
  /// <summary>
  /// Описывает группу с дубликатами файлов.
  /// </summary>
  /// <param name="GroupName">Наименование группы.</param>
  /// <param name="FileCount">Количество файлов в группе.</param>
  /// <param name="TotalSizeMb">Суммарный размер файлов в группе в МБ</param>
  /// <param name="Files">Список файлов.</param>
  public record class DuplicateGroup(
    string GroupName,
    int FileCount,
    double TotalSizeMb,
    List<FileInfo> Files
    );
}
