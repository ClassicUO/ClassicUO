// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Collections.Generic;
using System.Threading;

namespace ClassicUO;

/**
 * A #SynchronizationContext implementation that is must be invoked
 * manually.  It is meant to act as a #SynchronizationContext for the
 * main/game/UI thread which runs at a fixed frame rate and invokes
 * this class's Tick() method in each iteration.
 */
sealed class ManualSynchronizationContext : SynchronizationContext
{
    private readonly Thread targetThread = Thread.CurrentThread;
    private readonly List<Action> newWork = new List<Action>();
    private readonly List<Action> currentWork = new List<Action>();

    public override void Post(SendOrPostCallback d, object state)
    {
        lock (newWork)
        {
            newWork.Add(() => d(state));
        }
    }

    public override void Send(SendOrPostCallback d, object state)
    {
        if (Thread.CurrentThread == targetThread)
        {
            /* we're already in the target thread; we can invoke the
               callback directly */
            d(state);
        }
        else
        {
            using var completion = new ManualResetEvent(false);

            var work = () => {
                try
                {
                    d(state);
                }
                finally
                {
                    completion.Set();
                }
            };

            lock (newWork)
            {
                newWork.Add(work);
            }

            completion.WaitOne();
        }
    }

    public void Tick()
    {
        lock (newWork)
        {
            currentWork.AddRange(newWork);
            newWork.Clear();
        }

        try
        {
            foreach (var work in currentWork)
            {
                work();
            }
        }
        finally
        {
            currentWork.Clear();
        }
    }
}
