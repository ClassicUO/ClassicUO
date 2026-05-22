// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.Events;

namespace ClassicUO.Game.Map
{
    internal sealed class ClientViewRangeHandler : IEventListener
    {
        private readonly World _world;

        public ClientViewRangeHandler(World world) => _world = world;

        public void Subscribe()
        {
            EventSink.ClientViewRangeChanged += OnClientViewRangeChanged;
        }

        public void Unsubscribe()
        {
            EventSink.ClientViewRangeChanged -= OnClientViewRangeChanged;
        }

        private void OnClientViewRangeChanged(ClientViewRangeChangedArgs e)
        {
            _world.ClientViewRange = e.Range;
        }
    }
}
