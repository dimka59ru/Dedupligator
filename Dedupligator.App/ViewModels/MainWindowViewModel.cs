using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dedupligator.App.Models;
using Dedupligator.Services.DuplicateFinders;
using Dedupligator.Services.Hashes;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Dedupligator.App.ViewModels
{
  public partial class MainWindowViewModel : ViewModelBase
  {
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ScanFolderCommand))]
    [NotifyPropertyChangedFor(nameof(SelectedFolderPath))]
    private IStorageFolder? _fileFolder;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalFiles))]
    [NotifyPropertyChangedFor(nameof(TotalGroup))]
    private ObservableCollection<DuplicateGroup> _duplicateGroups = [];

    [ObservableProperty]
    private bool _isProcess;

    [ObservableProperty]
    private double _progress;

    public int TotalFiles => DuplicateGroups.Sum(x => x.FileCount);
    public int TotalGroup => DuplicateGroups.Count;
    public string? SelectedFolderPath => FileFolder != null ? Helpers.StorageExtensions.GetSafeLocalPath(FileFolder) : null;
    public static string AppVersion { get; } = $"v{GetAppVersion()}";

    private bool CanExecuteScanFolder => FileFolder is not null && Helpers.StorageExtensions.GetSafeLocalPath(FileFolder) is not null;

    [RelayCommand(CanExecute = nameof(CanExecuteScanFolder))]
    private async Task ScanFolder()
    {
      if (SelectedFolderPath is null || !Directory.Exists(SelectedFolderPath))
        return;

      var hashService = new Sha256HashService();
      var strategy = new HashMatchStrategy(hashService);
      var finder = new DuplicateFinder(strategy);

      IsProcess = true;
      DuplicateGroups = [];
      Progress = 0;

      try
      {
        var duplicateGroups = await Task.Run(() => 
          finder.FindDuplicates(SelectedFolderPath, p => Progress = p));

        var groupsForUi = duplicateGroups.Select(group => new DuplicateGroup(
            GroupName: group[0].Name,
            FileCount: group.Count,
            TotalSizeMb: group.Sum(x => x.Length) / 1e6,
            Files: group
        )).ToList();

        DuplicateGroups = new ObservableCollection<DuplicateGroup>(groupsForUi);
        foreach (var group in duplicateGroups)
        {
          var duplicateGroup = new DuplicateGroup(
            GroupName: group[0].Name,
            FileCount: group.Count,
            TotalSizeMb: group.Sum(x => x.Length) / 1e6,
            Files: group
          );

          DuplicateGroups.Add(duplicateGroup);
        }
      }
      finally
      {
        IsProcess = false;
      }
    }

    private static string GetAppVersion()
    {
      try
      {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version;
        return version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : "0.1.0";
      }
      catch
      {
        return "0.1.0";
      }
    }
  }
}