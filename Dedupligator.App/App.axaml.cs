using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Dedupligator.App.ViewModels;
using Dedupligator.Services.Factories;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Dedupligator.App
{
  public partial class App : Application
  {
    private IServiceProvider? _serviceProvider;
    private IServiceScope? _scope;

    public override void Initialize()
    {
      AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
      // Register all the services needed for the application to run
      var collection = new ServiceCollection();
      collection.AddCommonServices();

      _serviceProvider = collection.BuildServiceProvider();

      // Создаем область видимости для главного окна
      _scope = _serviceProvider.CreateScope();
      var vm = _scope.ServiceProvider.GetRequiredService<MainWindowViewModel>();

      if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
      {
        desktop.MainWindow = new MainWindow
        {
          DataContext = vm
        };

        desktop.MainWindow.Closed += MainWindow_Closed;
      }

      base.OnFrameworkInitializationCompleted();
    }

    private void MainWindow_Closed(object? sender, EventArgs e)
    {
      _scope?.Dispose();
    }

    private void OnExit(object sender, ControlledApplicationLifetimeExitEventArgs e)
    {
      _scope?.Dispose();
      (_serviceProvider as IDisposable)?.Dispose();
    }
  }

  public static class ServiceCollectionExtensions
  {
    public static void AddCommonServices(this IServiceCollection collection)
    {
      collection.AddSingleton<IDuplicateMatchStrategyFactory, DuplicateMatchStrategyFactory>();
      collection.AddScoped<MainWindowViewModel>();
    }
  }
}