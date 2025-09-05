using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using System;
using System.Windows.Input;

namespace Dedupligator.App.Controls
{
  public partial class SimpleIconButton : UserControl
  {
    private Button? _button;

    public static readonly StyledProperty<string> TextProperty =
        AvaloniaProperty.Register<SimpleIconButton, string>(nameof(Text), "Button");

    public static readonly StyledProperty<object> IconProperty =
        AvaloniaProperty.Register<SimpleIconButton, object>(nameof(Icon));

    public static readonly StyledProperty<ICommand> CommandProperty =
        AvaloniaProperty.Register<SimpleIconButton, ICommand>(nameof(Command));

    public static readonly StyledProperty<object> CommandParameterProperty =
        AvaloniaProperty.Register<SimpleIconButton, object>(nameof(CommandParameter));

    public static readonly StyledProperty<string> ButtonClassProperty =
      AvaloniaProperty.Register<SimpleIconButton, string>(nameof(ButtonClass));

    public static readonly RoutedEvent<RoutedEventArgs> ClickEvent = 
      RoutedEvent.Register<SimpleIconButton, RoutedEventArgs>(nameof(Click), RoutingStrategies.Bubble);

    public event EventHandler<RoutedEventArgs> Click
    {
      add => AddHandler(ClickEvent, value);
      remove => RemoveHandler(ClickEvent, value);
    }

    public string ButtonClass
    {
      get => GetValue(ButtonClassProperty);
      set => SetValue(ButtonClassProperty, value);

    }

    public SimpleIconButton()
    {
      InitializeComponent();

      // Привязка изменения свойства к классам кнопки
      ButtonClassProperty.Changed.AddClassHandler<SimpleIconButton>((x, e) =>
      {
        if (x.PART_Button != null && e.NewValue is string newClass)
        {
          x.PART_Button.Classes.Clear();
          if (!string.IsNullOrEmpty(newClass))
          {
            x.PART_Button.Classes.Add(newClass);
          }
        }
      });
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
      base.OnApplyTemplate(e);

      _button = this.FindControl<Button>("PART_Button");
      if (_button != null)
      {
        _button.Click += OnButtonClick;
        UpdateButtonState();
      }
    }

    public string Text
    {
      get => GetValue(TextProperty);
      set => SetValue(TextProperty, value);
    }

    public object Icon
    {
      get => GetValue(IconProperty);
      set => SetValue(IconProperty, value);
    }

    public ICommand Command
    {
      get => GetValue(CommandProperty);
      set => SetValue(CommandProperty, value);
    }

    public object CommandParameter
    {
      get => GetValue(CommandParameterProperty);
      set => SetValue(CommandParameterProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
      base.OnPropertyChanged(change);

      if (change.Property == TextProperty)
      {
        TextBlock.Text = change.NewValue as string;
      }
      else if (change.Property == IconProperty)
      {
        IconControl.Content = change.NewValue;
      }
      else if (change.Property == CommandProperty)
      {
        // Отписываемся от старой команды
        if (change.OldValue is ICommand oldCommand)
        {
          oldCommand.CanExecuteChanged -= OnCanExecuteChanged;
        }

        // Подписываемся на новую команду
        if (change.NewValue is ICommand newCommand)
        {
          newCommand.CanExecuteChanged += OnCanExecuteChanged;
        }

        UpdateButtonState();
      }
      else if (change.Property == CommandParameterProperty)
      {
        UpdateButtonState();
      }
    }

    private void OnCanExecuteChanged(object? sender, EventArgs e)
    {
      UpdateButtonState();
    }

    private void UpdateButtonState()
    {
      if (_button != null)
      {
        _button.IsEnabled = Command?.CanExecute(CommandParameter) ?? true;
      }
    }

    private void OnButtonClick(object? sender, RoutedEventArgs e)
    {
      var newArgs = new RoutedEventArgs(ClickEvent);
      RaiseEvent(newArgs);

      if (Command?.CanExecute(CommandParameter) == true)
      {
        Command.Execute(CommandParameter);
      }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
      base.OnDetachedFromVisualTree(e);

      // Отписываемся от команды
      if (Command != null)
      {
        Command.CanExecuteChanged -= OnCanExecuteChanged;
      }

      // Отписываемся от кнопки
      if (_button != null)
      {
        _button.Click -= OnButtonClick;
      }
    }
  }
}