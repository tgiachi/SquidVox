using System.Collections.Concurrent;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Xna.Framework;
using Serilog;
using SquidVox.Core.Data.Internal.Timers;
using SquidVox.Core.Interfaces.GameObjects;
using SquidVox.Core.Interfaces.Services;

namespace SquidVox.World3d.Services;

/// <summary>
/// Implements the timer service for scheduling and managing timed operations.
/// </summary>
public class TimerService : ITimerService, ISVoxUpdateable
{
    private readonly ILogger _logger = Log.ForContext<TimerService>();

    private readonly ObjectPool<TimerDataObject> _timerDataPool = ObjectPool.Create(
        new DefaultPooledObjectPolicy<TimerDataObject>()
    );

    private readonly SemaphoreSlim _timerSemaphore = new(1, 1);

    private readonly BlockingCollection<TimerDataObject> _timers = new();


    private void TimerExecutorGuard(TimerDataObject timerDataObject)
    {
        try
        {
            if (timerDataObject.IsAsync)
            {
                // Execute async callback synchronously within the event loop
                timerDataObject.AsyncCallback?.Invoke().GetAwaiter().GetResult();
            }
            else
            {
                timerDataObject.Callback();
            }
        }
        catch (Exception ex)
        {
            if (timerDataObject.DieOnException)
            {
                _logger.Error(ex, "Timer {TimerId} encountered an error and will be unregistered", timerDataObject.Id);
                UnregisterTimer(timerDataObject.Id);
            }
            else
            {
                _logger.Warning(ex, "Timer {TimerId} encountered an error", timerDataObject.Id);
            }
        }
    }


    public string RegisterTimer(string name, double intervalInMs, Action callback, double delayInMs = 0, bool repeat = false)
    {
        var existingTimer = _timers.FirstOrDefault(t => t.Name == name);

        if (existingTimer != null)
        {
            _logger.Warning("Timer with name {Name} already exists. Unregistering it.", name);
            UnregisterTimer(existingTimer.Id);
        }

        _timerSemaphore.Wait();

        var timerId = Guid.NewGuid().ToString();
        var timer = _timerDataPool.Get();

        timer.Name = name;
        timer.Id = timerId;
        timer.IntervalInMs = intervalInMs;
        timer.Callback = callback;
        timer.Repeat = repeat;
        timer.RemainingTimeInMs = intervalInMs;
        timer.DelayInMs = delayInMs;
        timer.IsAsync = false;


        _timers.Add(timer);

        _timerSemaphore.Release();

        _logger.Debug(
            "Registering timer: {TimerId}, Interval: {IntervalInSeconds} ms, Repeat: {Repeat}",
            timerId,
            intervalInMs,
            repeat
        );

        return timerId;
    }

    public string RegisterTimer(
        string name, TimeSpan interval, Action callback, TimeSpan delay = default, bool repeat = false
    )
    {
        return RegisterTimer(name, interval.TotalMilliseconds, callback, delay.TotalMilliseconds, repeat);
    }

    public string RegisterTimerAsync(
        string name, double intervalInMs, Func<Task> callback, double delayInMs = 0, bool repeat = false
    )
    {
        var existingTimer = _timers.FirstOrDefault(t => t.Name == name);

        if (existingTimer != null)
        {
            _logger.Warning("Timer with name {Name} already exists. Unregistering it.", name);
            UnregisterTimer(existingTimer.Id);
        }

        _timerSemaphore.Wait();

        var timerId = Guid.NewGuid().ToString();
        var timer = _timerDataPool.Get();

        timer.Name = name;
        timer.Id = timerId;
        timer.IntervalInMs = intervalInMs;
        timer.AsyncCallback = callback;
        timer.Repeat = repeat;
        timer.RemainingTimeInMs = intervalInMs;
        timer.DelayInMs = delayInMs;
        timer.IsAsync = true;


        _timers.Add(timer);

        _timerSemaphore.Release();

        _logger.Debug(
            "Registering async timer: {TimerId}, Interval: {IntervalInSeconds} ms, Repeat: {Repeat}",
            timerId,
            intervalInMs,
            repeat
        );

        return timerId;
    }

    public string RegisterTimerAsync(
        string name, TimeSpan interval, Func<Task> callback, TimeSpan delay = default, bool repeat = false
    )
    {
        return RegisterTimerAsync(name, interval.TotalMilliseconds, callback, delay.TotalMilliseconds, repeat);
    }

    public void UnregisterTimer(string timerId)
    {
        _timerSemaphore.Wait();

        var timer = _timers.FirstOrDefault(t => t.Id == timerId);

        if (timer != null)
        {
            _timers.TryTake(out timer);
            _logger.Information("Unregistering timer: {TimerId}", timer.Id);
            _timerDataPool.Return(timer);
        }
        else
        {
            _logger.Warning("Timer with ID {TimerId} not found", timerId);
        }

        _timerSemaphore.Release();
    }

    public void UnregisterAllTimers()
    {
        _timerSemaphore.Wait();

        while (_timers.TryTake(out var timer))
        {
            _logger.Information("Unregistering timer: {TimerId}", timer.Id);
        }

        _timerSemaphore.Release();
    }

    public void Dispose()
    {
        _timerSemaphore.Dispose();
        _timers.Dispose();

        GC.SuppressFinalize(this);
    }

    public void Update(GameTime gameTime)
    {
        _timerSemaphore.Wait();

        foreach (var timer in _timers)
        {
            timer.DecrementRemainingTime(gameTime.ElapsedGameTime.TotalMilliseconds);

            if (timer.RemainingTimeInMs <= 0)
            {
                try
                {
                    TimerExecutorGuard(timer);
                    //  _eventLoopService.EnqueueAction($"timer-{timer.Id}", () => TimerExecutorGuard(timer));
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Error executing timer callback for {TimerId}", timer.Id);
                }

                if (timer.Repeat)
                {
                    timer.ResetRemainingTime();
                }
                else
                {
                    _timers.TryTake(out var _);
                    _logger.Information("Unregistering timer: {TimerId}", timer.Id);
                }
            }
        }

        _timerSemaphore.Release();
    }
}
