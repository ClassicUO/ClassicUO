// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.Events;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;

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
            EventSink.WalkDenied += OnWalkDenied;
            EventSink.WalkConfirmed += OnWalkConfirmed;
            EventSink.MobileNameChanged += OnMobileNameChanged;
            EventSink.PlayerEnteredWorld += OnPlayerEnteredWorld;
        }

        public void UnsubscribeEvents()
        {
            EventSink.HitpointsUpdated -= OnHitpointsUpdated;
            EventSink.ManaUpdated -= OnManaUpdated;
            EventSink.StaminaUpdated -= OnStaminaUpdated;
            EventSink.MobileAttributesUpdated -= OnMobileAttributesUpdated;
            EventSink.WarModeChanged -= OnWarModeChanged;
            EventSink.ClientViewRangeChanged -= OnClientViewRangeChanged;
            EventSink.WalkDenied -= OnWalkDenied;
            EventSink.WalkConfirmed -= OnWalkConfirmed;
            EventSink.MobileNameChanged -= OnMobileNameChanged;
            EventSink.PlayerEnteredWorld -= OnPlayerEnteredWorld;
        }

        private void OnWalkDenied(WalkDeniedArgs e)
        {
            if (Player == null) return;

            Player.Walker.DenyWalk(e.Sequence, e.X, e.Y, e.Z);
            Player.Direction = e.Direction;
        }

        private void OnWalkConfirmed(WalkConfirmedArgs e)
        {
            if (Player == null) return;

            byte noto = e.Notoriety;
            if (noto == 0 || noto >= 8)
            {
                noto = 0x01;
            }

            Player.NotorietyFlag = (Data.NotorietyFlag)noto;
            Player.Walker.ConfirmWalk(e.Sequence);
            Player.AddToTile();
        }

        private void OnMobileNameChanged(MobileNameChangedArgs e)
        {
            WMapEntity wme = WMapManager.GetEntity(e.Serial);
            if (wme != null && !string.IsNullOrEmpty(e.Name))
            {
                wme.Name = e.Name;
            }

            Entity entity = Get(e.Serial);
            if (entity == null) return;

            entity.Name = e.Name;

            if (
                Player != null
                && e.Serial == Player.Serial
                && !string.IsNullOrEmpty(e.Name)
                && e.Name != Player.Name
            )
            {
                Client.Game.SetWindowTitle(e.Name);
            }

            UIManager.GetGump<NameOverheadGump>(e.Serial)?.SetName();
        }

        private void OnPlayerEnteredWorld(PlayerEnteredWorldArgs e)
        {
            if (Player == null) return;

            Player.Graphic = e.Graphic;
            Player.CheckGraphicChange();

            if (Map == null)
            {
                MapIndex = 0;
            }

            Player.SetInWorldTile(e.X, e.Y, e.Z);
            Player.Direction = e.Direction;

            RangeSize.X = e.X;
            RangeSize.Y = e.Y;
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
