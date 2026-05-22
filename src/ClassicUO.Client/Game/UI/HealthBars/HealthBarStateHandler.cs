// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.Data;
using ClassicUO.Game.Events;
using ClassicUO.Game.GameObjects;

namespace ClassicUO.Game.UI.HealthBars
{
    /// <summary>
    /// Listens for health-bar state changes (poison / yellow-bar) and toggles
    /// the matching mobile flag, falling back to the SA poison helper on newer
    /// clients.
    /// </summary>
    internal sealed class HealthBarStateHandler : IEventListener
    {
        private readonly World _world;

        public HealthBarStateHandler(World world) => _world = world;

        public void Subscribe()
        {
            EventSink.HealthBarStateChanged += OnHealthBarStateChanged;
        }

        public void Unsubscribe()
        {
            EventSink.HealthBarStateChanged -= OnHealthBarStateChanged;
        }

        private void OnHealthBarStateChanged(HealthBarStateChangedArgs e)
        {
            Mobile mobile = _world.Mobiles.Get(e.Serial);
            if (mobile == null) return;

            switch (e.StateType)
            {
                case 1: // poison
                    if (Client.Game.UO.Version >= Utility.ClientVersion.CV_7000)
                    {
                        mobile.SetSAPoison(e.Enabled);
                    }
                    else if (e.Enabled)
                    {
                        mobile.Flags |= Flags.Poisoned;
                    }
                    else
                    {
                        mobile.Flags &= ~Flags.Poisoned;
                    }
                    break;

                case 2: // yellow bar
                    if (e.Enabled) mobile.Flags |= Flags.YellowBar;
                    else mobile.Flags &= ~Flags.YellowBar;
                    break;
            }
        }
    }
}
