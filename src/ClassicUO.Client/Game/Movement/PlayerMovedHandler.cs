// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.Data;
using ClassicUO.Game.Events;

namespace ClassicUO.Game.Movement
{
    internal sealed class PlayerMovedHandler : IEventListener
    {
        private readonly World _world;

        public PlayerMovedHandler(World world) => _world = world;

        public void Subscribe()
        {
            EventSink.PlayerMoved += OnPlayerMoved;
        }

        public void Unsubscribe()
        {
            EventSink.PlayerMoved -= OnPlayerMoved;
        }

        private void OnPlayerMoved(PlayerMovedArgs e)
        {
            if (_world.Player == null) return;
            _world.Player.Walk(e.Direction & Direction.Mask, e.IsRunning);
        }
    }
}
