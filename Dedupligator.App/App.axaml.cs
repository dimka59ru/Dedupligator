using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Dedupligator.App.ViewModels;
using Dedupligator.Services.DuplicateFinders;
using Microsoft.Extensions.DependencyInjection;

namespace Dedupligator.App
{
  public partial class App : Application
  {
    public override void Initialize()
    {
      AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
      // Register all the services needed for the application to run
      var collection = new ServiceCollection();
      collection.AddCommonServices();

      // Creates a ServiceProvider containing services from the provided IServiceCollection
      //var services = collection.BuildServiceProvider();

      //var vm = services.GetRequiredService<MainWindowViewModel>();
      if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
      {
        desktop.MainWindow = new MainWindow();
      }

      base.OnFrameworkInitializationCompleted();
    }    
  }

  public static class ServiceCollectionExtensions
  {
    public static void AddCommonServices(this IServiceCollection collection)
    {
      //collection.AddScoped<DuplicateFinder>();
      //collection.AddTransient<MainWindowViewModel>();
    }
  }
}