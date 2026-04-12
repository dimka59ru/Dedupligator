using System.Threading.Tasks;

namespace Dedupligator.App.Services
{
  public interface IConfirmationDialogService
  {
    Task<bool> ConfirmMoveToTrashAsync(int fileCount, string trashFolderPath);
  }
}
