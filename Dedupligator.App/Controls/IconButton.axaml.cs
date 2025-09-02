using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Dedupligator.App.Controls;

public class IconButton : Button
{
  // Dependency property для иконки
  public static readonly StyledProperty<Geometry> IconProperty =
      AvaloniaProperty.Register<IconButton, Geometry>(nameof(Icon));

  public Geometry Icon
  {
    get => GetValue(IconProperty);
    set => SetValue(IconProperty, value);
  }

  // Dependency property для размера иконки
  public static readonly StyledProperty<double> IconSizeProperty =
      AvaloniaProperty.Register<IconButton, double>(nameof(IconSize), 16.0);

  public double IconSize
  {
    get => GetValue(IconSizeProperty);
    set => SetValue(IconSizeProperty, value);
  }

  // Dependency property для отступа между иконкой и текстом
  public static readonly StyledProperty<double> IconSpacingProperty =
      AvaloniaProperty.Register<IconButton, double>(nameof(IconSpacing), 8.0);

  public double IconSpacing
  {
    get => GetValue(IconSpacingProperty);
    set => SetValue(IconSpacingProperty, value);
  }
}