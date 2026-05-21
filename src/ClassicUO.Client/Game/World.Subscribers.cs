// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.Events;
using ClassicUO.Game.GameObjects;

namespace ClassicUO.Game
{
    internal sealed partial class World
    {
        private void SubscribeEvents()
        {
            EventSink.HitpointsUpdated += OnHitpointsUpdated;
            EventSink.ManaUpdated += OnManaUpdated;
            EventSink.StaminaUpdated += OnStaminaUpdated;
            EventSink.MobileAttributesUpdated += OnMobileAttributesUpdated;
            EventSink.WarModeChanged += OnWarModeChanged;
            EventSink.ClientViewRangeChanged += OnClientViewRangeChanged;
        }

        public void UnsubscribeEvents()
        {
            EventSink.HitpointsUpdated -= OnHitpointsUpdated;
            EventSink.ManaUpdated -= OnManaUpdated;
            EventSink.StaminaUpdated -= OnStaminaUpdated;
            EventSink.MobileAttributesUpdated -= OnMobileAttributesUpdated;
            EventSink.WarModeChanged -= OnWarModeChanged;
            EventSink.ClientViewRangeChanged -= OnClientViewRangeChanged;
        }

        private void OnWarModeChanged(WarModeChangedArgs e)
        {
            if (Player != null && Player.Serial == e.Serial)
            {
                Player.InWarMode = e.InWarMode;
            }
        }

        private void OnClientViewRangeChanged(ClientViewRangeChangedArgs e)
        {
            ClientViewRange = e.Range;
        }

        private void OnHitpointsUpdated(HitpointsUpdatedArgs e)
        {
            var entity = Get(e.Serial);
            if (entity == null) return;

            entity.HitsMax = e.HitsMax;
            entity.Hits = e.Hits;

            if (entity.HitsRequest == HitsRequestStatus.Pending)
            {
                entity.HitsRequest = HitsRequestStatus.Received;
            }

            if (entity == Player)
            {
                UoAssist.SignalHits();
            }
        }

        private void OnManaUpdated(ManaUpdatedArgs e)
        {
            var mobile = Mobiles.Get(e.Serial);
            if (mobile == null) return;

            mobile.ManaMax = e.ManaMax;
            mobile.Mana = e.Mana;

            if (mobile == Player)
            {
                UoAssist.SignalMana();
            }
        }

        private void OnStaminaUpdated(StaminaUpdatedArgs e)
        {
            var mobile = Mobiles.Get(e.Serial);
            if (mobile == null) return;

            mobile.StaminaMax = e.StaminaMax;
            mobile.Stamina = e.Stamina;

            if (mobile == Player)
            {
                UoAssist.SignalStamina();
            }
        }

        private void OnMobileAttributesUpdated(MobileAttributesUpdatedArgs e)
        {
            var entity = Get(e.Serial);
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

                if (mobile == Player)
                {
                    UoAssist.SignalHits();
                    UoAssist.SignalStamina();
                    UoAssist.SignalMana();
                }
            }
        }
    }
}
