using System;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using Avalonia.Threading;

namespace ModernX2Launcher.Utilities;

/// <summary>
/// Exactly same as <see cref="AvaloniaScheduler"/> but will always schedule on the dispatcher, even
/// if the calling code is on the UI thread already
/// </summary>
public class NotInlineAvaloniaScheduler : LocalScheduler
{
    public static readonly NotInlineAvaloniaScheduler Instance = new();

    private NotInlineAvaloniaScheduler()
    {
    }

    public override IDisposable Schedule<TState>(
        TState state, TimeSpan dueTime, Func<IScheduler, TState, IDisposable> action
    )
    {
        IDisposable PostOnDispatcher()
        {
            var composite = new CompositeDisposable(2);

            var cancellation = new CancellationDisposable();

            Dispatcher.UIThread.Post(() =>
            {
                if (!cancellation.Token.IsCancellationRequested)
                {
                    composite.Add(action(this, state));
                }
            }, DispatcherPriority.DataBind);

            composite.Add(cancellation);

            return composite;
        }

        if (dueTime == TimeSpan.Zero)
        {
            return PostOnDispatcher();
            
            // if (!Dispatcher.UIThread.CheckAccess())
            // {
            //     return PostOnDispatcher();
            // }
            // else
            // {
            //     if (_reentrancyGuard >= MaxReentrantSchedules)
            //     {
            //         return PostOnDispatcher();
            //     }
            //
            //     try
            //     {
            //         _reentrancyGuard++;
            //
            //         return action(this, state);
            //     }
            //     finally
            //     {
            //         _reentrancyGuard--;
            //     }
            // }
        }
        else
        {
            var composite = new CompositeDisposable(2);

            composite.Add(DispatcherTimer.RunOnce(() => composite.Add(action(this, state)), dueTime));

            return composite;
        }
    }
}