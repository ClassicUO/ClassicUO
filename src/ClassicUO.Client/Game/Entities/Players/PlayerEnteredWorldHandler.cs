// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.Events;

namespace ClassicUO.Game.Entities.Players
{
    internal sealed class PlayerEnteredWorldHandler : IEventListener
    {
        private readonly World _world;

        public PlayerEnteredWorldHandler(World world) => _world = world;

        public void Subscribe()
        {
            EventSink.PlayerEnteredWorld += OnPlayerEnteredWorld;
        }

        public void Unsubscribe()
        {
            EventSink.PlayerEnteredWorld -= OnPlayerEnteredWorld;
        }

        private void OnPlayerEnteredWorld(PlayerEnteredWorldArgs e)
        {
            _world.CreatePlayer(e.Serial);

            if (_world.Player == null) return;

            _world.Player.Graphic = e.Graphic;
            _world.Player.CheckGraphicChange();

            if (_world.Map == null)
            {
                _world.MapIndex = 0;
            }

            _world.Player.SetInWorldTile(e.X, e.Y, e.Z);
            _world.Player.Direction = e.Direction;

            _world.RangeSize.X = e.X;
            _world.RangeSize.Y = e.Y;
        }
    }
}
