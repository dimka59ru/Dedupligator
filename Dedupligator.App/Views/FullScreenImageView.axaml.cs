using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.Input;
using Dedupligator.Common.Models;

namespace Dedupligator.App.Views;

public partial class FullScreenImageView : UserControl
{
  public static readonly StyledProperty<Bitmap?> ImageSourceProperty =
          AvaloniaProperty.Register<FullScreenImageView, Bitmap?>(nameof(ImageSource));

  public static readonly StyledProperty<IRelayCommand?> PreviousCommandProperty =
      AvaloniaProperty.Register<FullScreenImageView, IRelayCommand?>(nameof(PreviousCommand));

  public static readonly StyledProperty<IRelayCommand?> NextCommandProperty =
      AvaloniaProperty.Register<FullScreenImageView, IRelayCommand?>(nameof(NextCommand));

  public static readonly StyledProperty<IRelayCommand?> CloseCommandProperty =
      AvaloniaProperty.Register<FullScreenImageView, IRelayCommand?>(nameof(CloseCommand));

  public static readonly StyledProperty<ImageInfo?> ImageInfoProperty =
            AvaloniaProperty.Register<FullScreenImageView, ImageInfo?>(nameof(ImageInfo));

  public static readonly StyledProperty<IRelayCommand?> OpenContainingFolderCommandProperty =
     AvaloniaProperty.Register<FullScreenImageView, IRelayCommand?>(nameof(OpenContainingFolderCommand));

  public Bitmap? ImageSource
  {
    get => GetValue(ImageSourceProperty);
    set => SetValue(ImageSourceProperty, value);
  }

  public IRelayCommand? PreviousCommand
  {
    get => GetValue(PreviousCommandProperty);
    set => SetValue(PreviousCommandProperty, value);
  }

  public IRelayCommand? NextCommand
  {
    get => GetValue(NextCommandProperty);
    set => SetValue(NextCommandProperty, value);
  }

  public IRelayCommand? CloseCommand
  {
    get => GetValue(CloseCommandProperty);
    set => SetValue(CloseCommandProperty, value);
  }

  public IRelayCommand? OpenContainingFolderCommand
  {
    get => GetValue(OpenContainingFolderCommandProperty);
    set => SetValue(OpenContainingFolderCommandProperty, value);
  }

  public ImageInfo? ImageInfo
  {
    get => GetValue(ImageInfoProperty);
    set => SetValue(ImageInfoProperty, value);
  }

  public FullScreenImageView()
  {
    InitializeComponent();
  }
}