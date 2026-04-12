using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using System.Threading.Tasks;

namespace Dedupligator.App.Services
{
  public sealed class ConfirmationDialogService : IConfirmationDialogService
  {
    public async Task<bool> ConfirmMoveToTrashAsync(int fileCount, string trashFolderPath)
    {
      var dialog = new Views.ConfirmationDialog(
        title: "Подтверждение переноса",
        message: $"Переместить {fileCount} файл(ов) в служебную папку удалённых?\n\n{trashFolderPath}",
        confirmText: "Переместить",
        cancelText: "Отмена");

      var owner = GetOwnerWindow();
      if (owner == null)
        return false;

      return await dialog.ShowDialog<bool>(owner);
    }

    private static Window? GetOwnerWindow()
    {
      if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
      {
        return desktop.MainWindow;
      }

      return null;
    }
  }
}
