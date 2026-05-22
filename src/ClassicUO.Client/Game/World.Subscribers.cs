// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.Events;

namespace ClassicUO.Game
{
    internal sealed partial class World
    {
        private readonly System.Collections.Generic.List<IEventListener> _listeners = new();

        /// <summary>
        /// Register an <see cref="IEventListener"/> so its
        /// <c>Subscribe()</c> / <c>Unsubscribe()</c> are driven centrally
        /// from <see cref="SubscribeEvents"/> / <see cref="UnsubscribeEvents"/>.
        /// Returns the listener for fluent assignment.
        /// </summary>
        internal T RegisterListener<T>(T listener) where T : IEventListener
        {
            _listeners.Add(listener);
            return listener;
        }

        private void SubscribeEvents()
        {
            foreach (var listener in _listeners) listener.Subscribe();
        }

        public void UnsubscribeEvents()
        {
            foreach (var listener in _listeners) listener.Unsubscribe();
        }
    }
}
