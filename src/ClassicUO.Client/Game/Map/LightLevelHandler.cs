// SPDX-License-Identifier: BSD-2-Clause

using System;
using ClassicUO.Configuration;
using ClassicUO.Game.Events;

namespace ClassicUO.Game.Map
{
    internal sealed class LightLevelHandler : IEventListener
    {
        private readonly World _world;

        public LightLevelHandler(World world) => _world = world;

        public void Subscribe()
        {
            EventSink.LightLevelChanged += OnLightLevelChanged;
        }

        public void Unsubscribe()
        {
            EventSink.LightLevelChanged -= OnLightLevelChanged;
        }

        private void OnLightLevelChanged(LightLevelChangedArgs e)
        {
            if (e.IsPersonal)
            {
                if (_world.Player == null || _world.Player.Serial != e.Serial) return;

                _world.Light.RealPersonal = e.Level;

                if (ProfileManager.CurrentProfile == null || !ProfileManager.CurrentProfile.UseCustomLightLevel)
                {
                    _world.Light.Personal = e.Level;
                }
            }
            else
            {
                _world.Light.RealOverall = e.Level;

                var profile = ProfileManager.CurrentProfile;
                if (profile == null || !profile.UseCustomLightLevel || profile.LightLevelType == 1)
                {
                    _world.Light.Overall = profile != null && profile.LightLevelType == 1
                        ? Math.Min(e.Level, profile.LightLevel)
                        : e.Level;
                }
            }
        }
    }
}
