using System.Timers;

namespace Dedupligator.Services
{
  public sealed class DebounceTimer : IDisposable
  {
    private readonly System.Timers.Timer _timer;
    private Action? _action;

    public DebounceTimer(int milliseconds = 1000)
    {
      _timer = new System.Timers.Timer(milliseconds);
      _timer.Elapsed += OnTimerElapsed;
      _timer.AutoReset = false; // Таймер сработает только один раз
    }

    public void Start(Action action)
    {
      _action = action;
      _timer.Stop();
      _timer.Start();
    }

    private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
    {
      _action?.Invoke();
    }

    public void Dispose()
    {
      _timer?.Dispose();
    }
  }
}
