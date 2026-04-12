using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Dedupligator.App.Views
{
  public partial class ConfirmationDialog : Window
  {
    public ConfirmationDialog(string title, string message, string confirmText, string cancelText)
    {
      InitializeComponent();

      Title = title;
      TitleTextBlock.Text = title;
      MessageTextBlock.Text = message;
      ConfirmButton.Content = confirmText;
      CancelButton.Content = cancelText;
    }

    private void ConfirmButton_Click(object? sender, RoutedEventArgs e)
    {
      Close(true);
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
      Close(false);
    }
  }
}
