using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Dedupligator.App.Services;
using Dedupligator.App.ViewModels;
using Dedupligator.Services.DuplicateFinders;
using Dedupligator.Services.Factories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.IO;
using System.Threading.Tasks;

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
      ConfigureLogger();

      var collection = new ServiceCollection();
      collection.AddCommonServices();

      _serviceProvider = collection.BuildServiceProvider();

      // Создаем область видимости для главного окна
      _scope = _serviceProvider.CreateScope();
      var vm = _scope.ServiceProvider.GetRequiredService<MainWindowViewModel>();

      Log.Information("Application starting...");
      if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
      {
        desktop.MainWindow = new MainWindow
        {
          DataContext = vm
        };

        desktop.MainWindow.Closed += MainWindow_Closed;
        desktop.Startup += OnStartup;
        desktop.Exit += OnExit;
      }

      base.OnFrameworkInitializationCompleted();
    }

    private void MainWindow_Closed(object? sender, EventArgs e)
    {
      _scope?.Dispose();
    }

    private void OnStartup(object? sender, ControlledApplicationLifetimeStartupEventArgs e)
    {
      Log.Information("Application started successfully");
    }

    private void OnExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
      _scope?.Dispose();
      (_serviceProvider as IDisposable)?.Dispose();

      Log.Information("Application shutting down");
      Log.CloseAndFlush();
    }

    private static void ConfigureLogger()
    {
      Log.Logger = new LoggerConfiguration()
          .MinimumLevel.Debug()
          .WriteTo.Console()
          .WriteTo.File(
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Dedupligator",
                "logs",
                "app-.log"
            ),
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 7,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level}] {Message}{NewLine}{Exception}"
          )
          .CreateLogger();

      ConfigureGlobalExceptionHandling();
    }

    private static void ConfigureGlobalExceptionHandling()
    {
      AppDomain.CurrentDomain.UnhandledException += (s, e) =>
      {
        var ex = e.ExceptionObject as Exception;
        Log.Fatal(ex, "Unhandled exception occurred");
      };

      TaskScheduler.UnobservedTaskException += (s, e) =>
      {
        Log.Error(e.Exception, "Unobserved task exception");
        e.SetObserved();
      };
    }
  }

  public static class ServiceCollectionExtensions
  {
    public static void AddCommonServices(this IServiceCollection services)
    {
      services.AddLogging(builder =>
      {
        builder.AddSerilog();
        builder.SetMinimumLevel(LogLevel.Debug);
      });
      services.AddTransient<DuplicateFinder>();
      services.AddSingleton<IConfirmationDialogService, ConfirmationDialogService>();
      services.AddSingleton<DuplicateMatchStrategyFactory>();
      services.AddSingleton<IDuplicateMatchStrategyFactory>(sp => sp.GetRequiredService<DuplicateMatchStrategyFactory>());
      services.AddScoped<MainWindowViewModel>();
    }
  }
}
