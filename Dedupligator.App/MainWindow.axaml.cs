using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Dedupligator.App.ViewModels;

namespace Dedupligator.App
{
  public partial class MainWindow : Window
  {
    private readonly MainWindowViewModel _mainViewModel = new();

    private async void BrowseButton_Click(object sender, RoutedEventArgs args)
    {
      var topLevel = TopLevel.GetTopLevel(this);
      if (topLevel == null) 
        return;

      var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions());
      if (folders.Count > 0)
        _mainViewModel.SelectedFolder = folders[0];
    }

    public MainWindow()
    {
      InitializeComponent();
      DataContext = _mainViewModel;
    }
  }
}