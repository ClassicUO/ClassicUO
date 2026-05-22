// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Collections.Generic;

namespace ClassicUO.Game.Events
{
    /// <summary>
    /// Tracks a set of <see cref="TypedEvent{T}"/> subscriptions and
    /// undoes them as a batch. Replaces hand-written <c>Subscribe()</c> /
    /// <c>Unsubscribe()</c> pairs that have to mirror each other line-for-line.
    /// <para/>
    /// Usage:
    /// <code>
    /// _subs.Add(EventSink.MusicPlay, OnMusicPlay);
    /// _subs.Add(EventSink.MusicStop, OnMusicStop);
    /// ...
    /// _subs.DisposeAll();  // on teardown
    /// </code>
    /// Owner classes can no longer forget to remove a handler — the
    /// subscribe and unsubscribe lambdas are paired at the single <c>Add</c>
    /// call site.
    /// </summary>
    internal sealed class EventSubscriptions
    {
        private readonly List<Action> _unsubscribers = new();

        public void Add<T>(TypedEvent<T> evt, Action<T> handler)
        {
            evt.Subscribe(handler);
            _unsubscribers.Add(() => evt.Unsubscribe(handler));
        }

        public void DisposeAll()
        {
            foreach (var u in _unsubscribers) u();
            _unsubscribers.Clear();
        }
    }
}
