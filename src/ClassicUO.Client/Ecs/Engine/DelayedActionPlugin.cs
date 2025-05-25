using System;
using System.Collections.Generic;
using TinyEcs;

namespace ClassicUO.Ecs;

internal readonly struct DelayedActionPlugin : IPlugin
{
    public void Build(Scheduler scheduler)
    {
        scheduler.AddResource(new DelayedAction());

        scheduler.OnBeforeUpdate
        (
            (Res<DelayedAction> delayedAction, Time time) =>
            {
                delayedAction.Value.Run(time.Total);
            }, ThreadingMode.Single
        );
    }
}


public sealed class DelayedAction
{
    private readonly List<(Action Fn, float Time)> _actions = new();

    public void Add(Action fn, float delay)
    {
        ArgumentNullException.ThrowIfNull(fn);

        if (delay < 0)
            throw new ArgumentOutOfRangeException(nameof(delay), "Delay must be non-negative.");

        _actions.Add((fn, delay));
    }

    public void Run(float totalTime)
    {
        for (var i = _actions.Count - 1; i >= 0; i--)
        {
            (var fn, var time) = _actions[i];

            if (totalTime > time)
            {
                fn();
                _actions.RemoveAt(i);
                break;
            }
        }
    }

    public void Clear() => _actions.Clear();
}
