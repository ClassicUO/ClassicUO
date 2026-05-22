// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.Events;

namespace ClassicUO.Game.Combat
{
    internal sealed class WarModeHandler : IEventListener
    {
        private readonly World _world;

        public WarModeHandler(World world) => _world = world;

        public void Subscribe()
        {
            EventSink.WarModeChanged += OnWarModeChanged;
        }

        public void Unsubscribe()
        {
            EventSink.WarModeChanged -= OnWarModeChanged;
        }

        private void OnWarModeChanged(WarModeChangedArgs e)
        {
            if (_world.Player != null && _world.Player.Serial == e.Serial)
            {
                _world.Player.InWarMode = e.InWarMode;
            }
        }
    }
}
