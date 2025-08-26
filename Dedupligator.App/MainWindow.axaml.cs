using Avalonia.Controls;
using Dedupligator.App.ViewModels;

namespace Dedupligator.App
{
  public partial class MainWindow : Window
  {
    public MainWindow()
    {
      InitializeComponent();
      DataContext = new MainWindowViewModel();
    }
  }
}