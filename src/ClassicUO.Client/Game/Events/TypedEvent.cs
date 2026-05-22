// SPDX-License-Identifier: BSD-2-Clause

using System;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Game.Events
{
    /// <summary>
    /// Reified event slot. Replaces <c>event Action&lt;T&gt;</c> fields on
    /// <see cref="EventSink"/> / <see cref="Outgoing.OutgoingEventSink"/> so
    /// subscriptions can be tracked through a helper (<see cref="EventSubscriptions"/>)
    /// instead of manual <c>+= / -=</c> pairs.
    /// <para/>
    /// Operator overloads keep the existing <c>EventSink.X += handler</c> and
    /// <c>EventSink.X -= handler</c> syntax compiling. The compiler rewrites
    /// these as <c>field = field + handler</c>, the <c>+</c> operator
    /// subscribes as a side effect, and the same instance is returned so the
    /// field assignment is a no-op.
    /// </summary>
    internal sealed class TypedEvent<T>
    {
        private Action<T> _handlers;

        public void Subscribe(Action<T> handler)   => _handlers += handler;
        public void Unsubscribe(Action<T> handler) => _handlers -= handler;
        public void Clear()                        => _handlers = null;

        public void Raise(in T args)
        {
            var snapshot = _handlers;
            if (snapshot is null) return;

            foreach (var d in snapshot.GetInvocationList())
            {
                try
                {
                    ((Action<T>)d)(args);
                }
                catch (Exception ex)
                {
                    Log.Error($"EventSink handler failed for {typeof(T).Name}: {ex}");
                }
            }
        }

        /// <summary>Backward-compat for callers using the C# <c>+=</c> syntax.</summary>
        public static TypedEvent<T> operator +(TypedEvent<T> evt, Action<T> handler)
        {
            evt.Subscribe(handler);
            return evt;
        }

        /// <summary>Backward-compat for callers using the C# <c>-=</c> syntax.</summary>
        public static TypedEvent<T> operator -(TypedEvent<T> evt, Action<T> handler)
        {
            evt.Unsubscribe(handler);
            return evt;
        }
    }
}
