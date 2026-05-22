// SPDX-License-Identifier: BSD-2-Clause

namespace ClassicUO.Game.Events
{
    /// <summary>
    /// Anything that subscribes to <see cref="EventSink"/> events implements
    /// this. World iterates the registered listeners and drives Subscribe /
    /// Unsubscribe centrally instead of each subscriber auto-wiring inside
    /// its constructor.
    /// <para/>
    /// Subscribe / Unsubscribe use plain <c>EventSink.X += handler</c> /
    /// <c>EventSink.X -= handler</c>; the TypedEvent operator overloads keep
    /// that syntax compiling against the underlying typed event slot.
    /// <code>
    /// internal sealed class FooManager : IEventListener
    /// {
    ///     public void Subscribe()
    ///     {
    ///         EventSink.X += OnX;
    ///         EventSink.Y += OnY;
    ///     }
    ///
    ///     public void Unsubscribe()
    ///     {
    ///         EventSink.X -= OnX;
    ///         EventSink.Y -= OnY;
    ///     }
    /// }
    /// </code>
    /// </summary>
    internal interface IEventListener
    {
        void Subscribe();
        void Unsubscribe();
    }
}
