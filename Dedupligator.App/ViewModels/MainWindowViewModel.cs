using Avalonia.Platform.Storage;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace Dedupligator.App.ViewModels
{
  public partial class MainWindowViewModel : ViewModelBase
  {
    [Reactive]
    private IStorageFolder? _fileFolder;
    private readonly IObservable<bool> _canExecuteScanFolder;

    public static string AppVersion { get; } = $"v{GetAppVersion()}";
    

    [ReactiveCommand(CanExecute = nameof(_canExecuteScanFolder))]
    private async Task ScanFolder()
    {
      // Логика сканирования
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
      _canExecuteScanFolder = this.WhenAnyValue(
          x => x.FileFolder,
          (IStorageFolder? folder) => folder is not null
      );
    }
  }
}