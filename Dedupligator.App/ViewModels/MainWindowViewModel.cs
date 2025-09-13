using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dedupligator.App.Collections;
using Dedupligator.App.Helpers;
using Dedupligator.App.Models;
using Dedupligator.Services;
using Dedupligator.Services.Cache;
using Dedupligator.Services.DuplicateFinders;
using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Dedupligator.App.ViewModels
{
  public partial class MainWindowViewModel : ViewModelBase
  {
    private const int PreviewImageMaxWidth = 250;

    private readonly AsyncDebouncer _debouncer = new(500);

    private CancellationTokenSource? _cancellationTokenSource;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ScanFolderCommand))]
    [NotifyPropertyChangedFor(nameof(SelectedFolderPath))]
    private IStorageFolder? _selectedFolder;

    [ObservableProperty]
    private ObservableRangeCollection<DuplicateGroup> _duplicateGroups = [];

    [ObservableProperty]
    private ObservableRangeCollection<ImagePreviewViewModel> _filePreviews = [];

    [ObservableProperty]
    private bool _isProcess;

    [ObservableProperty]
    private bool _useNeuro;

    [ObservableProperty]
    private double _progress;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RemoveFilesCommand))]
    private DuplicateGroup? _selectedFileGroup;

    [ObservableProperty]
    private bool _useExactMatch = true;

    [ObservableProperty]
    private Bitmap? _selectedImage;

    [ObservableProperty]
    private float _threshold;

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

      var strategy = CreateStrategy();
      var finder = new DuplicateFinder(strategy);

      IsProcess = true;
      DuplicateGroups.Clear();
      FilePreviews.Clear();
      Progress = 0;

      try
      {
        var progress = new Progress<double>(p =>
        {
          Progress = p;
        });

        _cancellationTokenSource = new CancellationTokenSource();
        var duplicateGroups = await Task.Run(() => finder.FindDuplicates(SelectedFolderPath, progress, _cancellationTokenSource.Token));

        var groupsForUi = duplicateGroups.Select(group => new DuplicateGroup(
            GroupName: group[0].Name,
            FileCount: group.Count,
            TotalSize: group.Sum(x => x.Length).ToFileSizeString(),
            Files: group
        )).ToList();

        DuplicateGroups.ReplaceWith(groupsForUi);

        SelectedFileGroup = DuplicateGroups.Count > 0 ? DuplicateGroups[0] : null;
      }
      finally
      {
        IsProcess = false;
        _cancellationTokenSource?.Dispose();

        if (strategy is ICachedDuplicateMatchStrategy cachedStrategy)
        {
          cachedStrategy.ClearCache();
        }
      }
    }

    [RelayCommand]
    private void StopScanFolder()
    {
      _cancellationTokenSource?.Cancel();
    }

    private bool CanExecuteRemoveFiles => FilePreviews.Any(x => x.MarkedForDeletion);

    [RelayCommand(CanExecute = nameof(CanExecuteRemoveFiles))]
    private void RemoveFiles()
    {
      var itemsToRemove = FilePreviews.Where(x => x.MarkedForDeletion).ToList();
      foreach (var item in itemsToRemove)
      {
        FilePreviews.Remove(item);
      }
    }

    [RelayCommand]
    private async Task OpenFullScreen(ImagePreviewViewModel? imageVm)
    {
      if (imageVm != null)
      {
        var (Width, _) = await ImageHelper.GetImageDimensionsAsync(imageVm.FilePath);
        var image = await ImageHelper.LoadImageAsync(imageVm.FilePath, (int)Width);
        SelectedImage = image.Bitmap;
      }
    }

    [RelayCommand]
    private void CloseFullScreen(ImagePreviewViewModel? imageVm)
    {
      CloseFullScreen();
    }

    private void CloseFullScreen()
    {
      SelectedImage = null;
    }

    private IDuplicateMatchStrategy CreateStrategy()
    {
      if (UseNeuro)
        return new NeuralSimilarityStrategy(Threshold);

      if (UseExactMatch)
        return new ExactMatchStrategy();

      return new SimilarImageStrategy();
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

      FilePreviews.ReplaceWith(previews);

      await _debouncer.DebounceAsync(async () =>
      {
        foreach (var preview in FilePreviews.ToList())
        {
          await preview.LoadImageAsync(PreviewImageMaxWidth);
        }
      });
    }

    private void FilePreviews_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
      switch (e.Action)
      {
        case NotifyCollectionChangedAction.Add:
        case NotifyCollectionChangedAction.Replace:
        case NotifyCollectionChangedAction.Move:
          if (e.NewItems != null)
          {
            foreach (ImagePreviewViewModel item in e.NewItems)
            {
              item.PropertyChanged += ImagePreview_PropertyChanged;
            }
          }
          break;

        case NotifyCollectionChangedAction.Remove:
          if (e.OldItems != null)
          {
            foreach (ImagePreviewViewModel item in e.OldItems)
            {
              item.PropertyChanged -= ImagePreview_PropertyChanged;
            }
          }
          break;

        case NotifyCollectionChangedAction.Reset:
          // Коллекция полностью изменилась — нужно обработать ВСЕ элементы
          if (FilePreviews.Count > 0)
          {
            // Отписываем старые (на всякий случай)
            foreach (var item in FilePreviews)
            {
              item.PropertyChanged -= ImagePreview_PropertyChanged;
            }
            // Подписываем снова
            foreach (var item in FilePreviews)
            {
              item.PropertyChanged += ImagePreview_PropertyChanged;
            }
          }

          CloseFullScreen();
          break;
      }


      RemoveFilesCommand.NotifyCanExecuteChanged();
    }

    private void ImagePreview_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
      if (e.PropertyName == nameof(ImagePreviewViewModel.MarkedForDeletion))
      {
        RemoveFilesCommand.NotifyCanExecuteChanged();
      }
    }

    private void DuplicateGroups_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
      OnPropertyChanged(nameof(TotalFiles));
      OnPropertyChanged(nameof(TotalGroup));
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

    public MainWindowViewModel()
    {
      DuplicateGroups.CollectionChanged += DuplicateGroups_CollectionChanged;
      FilePreviews.CollectionChanged+= FilePreviews_CollectionChanged;
    }
  }
}