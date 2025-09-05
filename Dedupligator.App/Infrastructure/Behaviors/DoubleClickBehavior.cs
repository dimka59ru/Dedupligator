using Avalonia;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using System.Windows.Input;

namespace Dedupligator.App.Infrastructure.Behaviors;
public class DoubleClickBehavior
{
  public static readonly AttachedProperty<ICommand?> CommandProperty =
      AvaloniaProperty.RegisterAttached<DoubleClickBehavior, Interactive, ICommand?>(
          "Command", default(ICommand?), false, BindingMode.OneTime);

  public static readonly AttachedProperty<object?> CommandParameterProperty =
      AvaloniaProperty.RegisterAttached<DoubleClickBehavior, Interactive, object?>(
          "CommandParameter", default(object?), false, BindingMode.OneWay);

  public static ICommand? GetCommand(Interactive obj) => obj.GetValue(CommandProperty);
  public static void SetCommand(Interactive obj, ICommand? value) => obj.SetValue(CommandProperty, value);

  public static object? GetCommandParameter(Interactive obj) => obj.GetValue(CommandParameterProperty);
  public static void SetCommandParameter(Interactive obj, object? value) => obj.SetValue(CommandParameterProperty, value);

  static DoubleClickBehavior()
  {
    CommandProperty.Changed.AddClassHandler<Interactive>((interactive, e) =>
    {
      if (e.NewValue is ICommand)
      {
        interactive.AddHandler(InputElement.DoubleTappedEvent, OnDoubleTapped);
      }
      else
      {
        interactive.RemoveHandler(InputElement.DoubleTappedEvent, OnDoubleTapped);
      }
    });
  }

  private static void OnDoubleTapped(object? sender, TappedEventArgs e)
  {
    if (sender is Interactive interactive)
    {
      var command = GetCommand(interactive);
      var parameter = GetCommandParameter(interactive);

      if (command?.CanExecute(parameter) == true)
      {
        command.Execute(parameter);
      }
    }
    e.Handled = true;
  }
}
