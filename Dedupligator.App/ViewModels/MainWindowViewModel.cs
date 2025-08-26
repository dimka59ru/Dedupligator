using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Reflection;

namespace Dedupligator.App.ViewModels
{
  public class MainWindowViewModel : ViewModelBase
  {
    public string Description { get; } = $"Dedupligator {AppVersion}";

    public static string AppVersion { get; } = GetAppVersion();

    private static string GetAppVersion()
    {
      try
      {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version;
        return version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : "0.1.0";
      }
      catch
      {
        return "0.1.0";
      }
    }
  }
}
