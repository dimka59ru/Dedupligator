using Avalonia.Controls.Shapes;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dedupligator.App.Helpers;
using Dedupligator.App.Models;
using Dedupligator.Services;
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
    private const int PreviewImageMaxWidth = 250;

    private AsyncDebouncer _debouncer = new(500);

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ScanFolderCommand))]
    [NotifyPropertyChangedFor(nameof(SelectedFolderPath))]
    private IStorageFolder? _selectedFolder;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalFiles))]
    [NotifyPropertyChangedFor(nameof(TotalGroup))]
    private ObservableCollection<DuplicateGroup> _duplicateGroups = [];

    [ObservableProperty]
    private bool _isProcess;

    [ObservableProperty]
    private double _progress;

    [ObservableProperty]
    private DuplicateGroup? _selectedFileGroup;

    [ObservableProperty]
    private ObservableCollection<ImagePreviewViewModel> _filePreviews = [];

    public int TotalFiles => DuplicateGroups.Sum(x => x.FileCount);
    public int TotalGroup => DuplicateGroups.Count;
    public string? SelectedFolderPath => SelectedFolder?.GetSafeLocalPath();
    public static string AppVersion { get; } = $"v{GetAppVersion()}";


    private bool CanExecuteScanFolder => SelectedFolderPath is not null;

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
            TotalSize: group.Sum(x => x.Length).ToFileSizeString(),
            Files: group
        )).ToList();

        DuplicateGroups = new ObservableCollection<DuplicateGroup>(groupsForUi);
      }
      finally
      {
        IsProcess = false;
      }
    }

    async partial void OnSelectedFileGroupChanged(DuplicateGroup? oldValue, DuplicateGroup? newValue)
    {
      if (newValue == null)
        return;

      var previews = newValue.Files.Select(file => new ImagePreviewViewModel
      (
        file.Name,
        file.FullName,
        file.Length.ToFileSizeString()
      )).ToList();

      FilePreviews = new ObservableCollection<ImagePreviewViewModel>(previews);

      await _debouncer.DebounceAsync(async () =>
      {
        foreach (var preview in previews)
        {
          await preview.LoadImageAsync(PreviewImageMaxWidth);
        }
      });
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