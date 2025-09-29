using Avalonia;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;

namespace Dedupligator.App.Views;

public partial class ImagePreviewView : UserControl
{
  public static readonly StyledProperty<IRelayCommand?> OpenContainingFolderCommandProperty =
   AvaloniaProperty.Register<ImagePreviewView, IRelayCommand?>(nameof(OpenContainingFolderCommand));

  public IRelayCommand? OpenContainingFolderCommand
  {
    get => GetValue(OpenContainingFolderCommandProperty);
    set => SetValue(OpenContainingFolderCommandProperty, value);
  }

  public ImagePreviewView()
    {
        InitializeComponent();
    }
}