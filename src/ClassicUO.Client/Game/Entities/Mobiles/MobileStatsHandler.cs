// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.Data;
using ClassicUO.Game.Events;
using ClassicUO.Game.GameObjects;

namespace ClassicUO.Game.Entities.Mobiles
{
    /// <summary>
    /// Listens for hits / mana / stamina / aggregate attribute updates and
    /// applies them to the matching <see cref="Entity"/> or <see cref="Mobile"/>
    /// on <see cref="World"/>. Also relays the Player-side signals to UoAssist.
    /// </summary>
    internal sealed class MobileStatsHandler : IEventListener
    {
        private readonly World _world;

        public MobileStatsHandler(World world) => _world = world;

        public void Subscribe()
        {
            EventSink.HitpointsUpdated += OnHitpointsUpdated;
            EventSink.ManaUpdated += OnManaUpdated;
            EventSink.StaminaUpdated += OnStaminaUpdated;
            EventSink.MobileAttributesUpdated += OnMobileAttributesUpdated;
        }

        public void Unsubscribe()
        {
            EventSink.HitpointsUpdated -= OnHitpointsUpdated;
            EventSink.ManaUpdated -= OnManaUpdated;
            EventSink.StaminaUpdated -= OnStaminaUpdated;
            EventSink.MobileAttributesUpdated -= OnMobileAttributesUpdated;
        }

        private void OnHitpointsUpdated(HitpointsUpdatedArgs e)
        {
            var entity = _world.Get(e.Serial);
            if (entity == null) return;

            entity.HitsMax = e.HitsMax;
            entity.Hits = e.Hits;

            if (entity.HitsRequest == HitsRequestStatus.Pending)
            {
                entity.HitsRequest = HitsRequestStatus.Received;
            }

            if (entity == _world.Player)
            {
                _world.UoAssist.SignalHits();
            }
        }

        private void OnManaUpdated(ManaUpdatedArgs e)
        {
            var mobile = _world.Mobiles.Get(e.Serial);
            if (mobile == null) return;

            mobile.ManaMax = e.ManaMax;
            mobile.Mana = e.Mana;

            if (mobile == _world.Player)
            {
                _world.UoAssist.SignalMana();
            }
        }

        private void OnStaminaUpdated(StaminaUpdatedArgs e)
        {
            var mobile = _world.Mobiles.Get(e.Serial);
            if (mobile == null) return;

            mobile.StaminaMax = e.StaminaMax;
            mobile.Stamina = e.Stamina;

            if (mobile == _world.Player)
            {
                _world.UoAssist.SignalStamina();
            }
        }

        private void OnMobileAttributesUpdated(MobileAttributesUpdatedArgs e)
        {
            var entity = _world.Get(e.Serial);
            if (entity == null) return;

            entity.HitsMax = e.HitsMax;
            entity.Hits = e.Hits;

            if (entity.HitsRequest == HitsRequestStatus.Pending)
            {
                entity.HitsRequest = HitsRequestStatus.Received;
            }

            if (SerialHelper.IsMobile(e.Serial) && entity is Mobile mobile)
            {
                mobile.ManaMax = e.ManaMax;
                mobile.Mana = e.Mana;
                mobile.StaminaMax = e.StaminaMax;
                mobile.Stamina = e.Stamina;

                if (mobile == _world.Player)
                {
                    _world.UoAssist.SignalHits();
                    _world.UoAssist.SignalStamina();
                    _world.UoAssist.SignalMana();
                }
            }
        }
    }
}
