// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.Events;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Game.UI.WorldMap;

namespace ClassicUO.Game.Entities.Mobiles
{
    /// <summary>
    /// Listens for mobile name changes and propagates them to the world-map
    /// entity, the <see cref="Entity"/> itself, the OS window title (when the
    /// rename is for the local player), and the existing name-overhead gump.
    /// </summary>
    internal sealed class MobileNameHandler : IEventListener
    {
        private readonly World _world;

        public MobileNameHandler(World world) => _world = world;

        public void Subscribe()
        {
            EventSink.MobileNameChanged += OnMobileNameChanged;
        }

        public void Unsubscribe()
        {
            EventSink.MobileNameChanged -= OnMobileNameChanged;
        }

        private void OnMobileNameChanged(MobileNameChangedArgs e)
        {
            WMapEntity wme = _world.WMapManager.GetEntity(e.Serial);
            if (wme != null && !string.IsNullOrEmpty(e.Name))
            {
                wme.Name = e.Name;
            }

            Entity entity = _world.Get(e.Serial);
            if (entity == null) return;

            entity.Name = e.Name;

            if (
                _world.Player != null
                && e.Serial == _world.Player.Serial
                && !string.IsNullOrEmpty(e.Name)
                && e.Name != _world.Player.Name
            )
            {
                Client.Game.SetWindowTitle(e.Name);
            }

            UIManager.GetGump<NameOverheadGump>(e.Serial)?.SetName();
        }
    }
}
