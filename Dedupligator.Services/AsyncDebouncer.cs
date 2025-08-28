using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dedupligator.Services
{
  public class AsyncDebouncer
  {
    private CancellationTokenSource? _cancellationTokenSource;
    private readonly int _delayMilliseconds;

    public AsyncDebouncer(int delayMilliseconds = 1000)
    {
      _delayMilliseconds = delayMilliseconds;
    }

    public async Task DebounceAsync(Func<Task> asyncAction)
    {
      // Отменяем предыдущую задачу
      if (_cancellationTokenSource != null)
      {
        await _cancellationTokenSource.CancelAsync();
        _cancellationTokenSource.Dispose();
      }
        
      _cancellationTokenSource = new CancellationTokenSource();

      try
      {
        // Ждем указанное время
        await Task.Delay(_delayMilliseconds, _cancellationTokenSource.Token);

        // Если не было отмены - выполняем действие
        await asyncAction();
      }
      catch (TaskCanceledException)
      {
        // Игнорируем отмену - это нормальное поведение
      }
    }

    public void Cancel()
    {
      _cancellationTokenSource?.Cancel();
    }
  }
}
