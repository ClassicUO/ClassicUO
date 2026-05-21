// SPDX-License-Identifier: BSD-2-Clause

using System;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
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
            EventSink.SeasonChanged += OnSeasonChanged;
            EventSink.LightLevelChanged += OnLightLevelChanged;
            EventSink.GraphicEffectSpawned += OnGraphicEffectSpawned;
            EventSink.BuffApplied += OnBuffApplied;
            EventSink.BuffRemoved += OnBuffRemoved;
            EventSink.ObjectDeleted += OnObjectDeleted;
            EventSink.PlayerMoved += OnPlayerMoved;
            EventSink.PathfindingReceived += OnPathfindingReceived;
            EventSink.HealthBarStateChanged += OnHealthBarStateChanged;
            EventSink.ItemDragEnded += OnItemDragEnded;
            EventSink.ItemDropAccepted += OnItemDropAccepted;
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
            EventSink.SeasonChanged -= OnSeasonChanged;
            EventSink.LightLevelChanged -= OnLightLevelChanged;
            EventSink.GraphicEffectSpawned -= OnGraphicEffectSpawned;
            EventSink.BuffApplied -= OnBuffApplied;
            EventSink.BuffRemoved -= OnBuffRemoved;
            EventSink.ObjectDeleted -= OnObjectDeleted;
            EventSink.PlayerMoved -= OnPlayerMoved;
            EventSink.PathfindingReceived -= OnPathfindingReceived;
            EventSink.HealthBarStateChanged -= OnHealthBarStateChanged;
            EventSink.ItemDragEnded -= OnItemDragEnded;
            EventSink.ItemDropAccepted -= OnItemDropAccepted;
        }

        private void OnItemDragEnded(ItemDragEndedArgs e)
        {
            Client.Game.UO.GameCursor.ItemHold.Enabled = false;
            Client.Game.UO.GameCursor.ItemHold.Dropped = false;
        }

        private void OnItemDropAccepted(ItemDropAcceptedArgs e)
        {
            Client.Game.UO.GameCursor.ItemHold.Enabled = false;
            Client.Game.UO.GameCursor.ItemHold.Dropped = false;
        }

        private void OnPlayerMoved(PlayerMovedArgs e)
        {
            if (Player == null) return;
            Player.Walk(e.Direction & Direction.Mask, e.IsRunning);
        }

        private void OnPathfindingReceived(PathfindingReceivedArgs e)
        {
            if (Player == null) return;
            Player.Pathfinder.WalkTo(e.X, e.Y, e.Z, 0);
        }

        private void OnHealthBarStateChanged(HealthBarStateChangedArgs e)
        {
            Mobile mobile = Mobiles.Get(e.Serial);
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

        private void OnObjectDeleted(ObjectDeletedArgs e)
        {
            if (Player == null || Player.Serial == e.Serial) return;

            uint serial = e.Serial;
            Entity entity = Get(serial);
            if (entity == null) return;

            bool updateAbilities = false;

            if (entity is Item it)
            {
                uint cont = it.Container & 0x7FFFFFFF;

                if (SerialHelper.IsValid(it.Container))
                {
                    Entity top = Get(it.RootContainer);

                    if (top != null && top == Player)
                    {
                        updateAbilities = it.Layer == Layer.OneHanded || it.Layer == Layer.TwoHanded;
                        Item tradeBoxItem = Player.GetSecureTradeBox();
                        if (tradeBoxItem != null)
                        {
                            UIManager.GetTradingGump(tradeBoxItem)?.RequestUpdateContents();
                        }
                    }

                    if (cont == Player.Serial && it.Layer == Layer.Invalid)
                    {
                        Client.Game.UO.GameCursor.ItemHold.Enabled = false;
                    }

                    if (it.Layer != Layer.Invalid)
                    {
                        UIManager.GetGump<PaperDollGump>(cont)?.RequestUpdateContents();
                    }

                    UIManager.GetGump<ContainerGump>(cont)?.RequestUpdateContents();

                    var profile = ProfileManager.CurrentProfile;
                    if (top != null && top.Graphic == 0x2006 && profile != null
                        && (profile.GridLootType == 1 || profile.GridLootType == 2))
                    {
                        UIManager.GetGump<GridLootGump>(cont)?.RequestUpdateContents();
                    }

                    if (it.Graphic == 0x0EB0)
                    {
                        UIManager.GetGump<BulletinBoardItem>(serial)?.Dispose();
                        UIManager.GetGump<BulletinBoardGump>()?.RemoveBulletinObject(serial);
                    }
                }
            }

            if (CorpseManager.Exists(0, serial)) return;

            if (entity is Mobile)
            {
                RemoveMobile(serial, true);
            }
            else
            {
                Item item = (Item)entity;

                if (item.IsMulti)
                {
                    HouseManager.Remove(serial);
                }

                Entity cont = Get(item.Container);

                if (cont != null)
                {
                    cont.Remove(item);

                    if (item.Layer != Layer.Invalid)
                    {
                        UIManager.GetGump<PaperDollGump>(cont)?.RequestUpdateContents();
                    }
                }
                else if (item.IsMulti)
                {
                    UIManager.GetGump<MiniMapGump>()?.RequestUpdateContents();
                }

                RemoveItem(serial, true);

                if (updateAbilities)
                {
                    Player.UpdateAbilities();
                }
            }
        }

        private void OnSeasonChanged(SeasonChangedArgs e)
        {
            if (Player == null) return;

            byte season = e.Season;
            if (season > 4) season = 0;

            if (Player.IsDead && season == 4) return;

            OldSeason = (Managers.Season)season;
            OldMusicIndex = e.MusicCue;

            if (Season == Managers.Season.Desolation)
            {
                OldMusicIndex = 42;
            }

            ChangeSeason((Managers.Season)season, e.MusicCue);
        }

        private void OnLightLevelChanged(LightLevelChangedArgs e)
        {
            if (e.IsPersonal)
            {
                if (Player == null || Player.Serial != e.Serial) return;

                Light.RealPersonal = e.Level;

                if (ProfileManager.CurrentProfile == null || !ProfileManager.CurrentProfile.UseCustomLightLevel)
                {
                    Light.Personal = e.Level;
                }
            }
            else
            {
                Light.RealOverall = e.Level;

                var profile = ProfileManager.CurrentProfile;
                if (profile == null || !profile.UseCustomLightLevel || profile.LightLevelType == 1)
                {
                    Light.Overall = profile != null && profile.LightLevelType == 1
                        ? Math.Min(e.Level, profile.LightLevel)
                        : e.Level;
                }
            }
        }

        private void OnGraphicEffectSpawned(GraphicEffectSpawnedArgs e)
        {
            SpawnEffect(
                (GraphicEffectType)e.EffectType,
                e.Source,
                e.Target,
                e.Graphic,
                (ushort)e.Hue,
                e.SourceX,
                e.SourceY,
                e.SourceZ,
                e.TargetX,
                e.TargetY,
                e.TargetZ,
                e.Speed,
                e.Duration,
                e.FixedDirection,
                e.DoesExplode,
                false,
                (GraphicEffectBlendMode)e.BlendMode
            );
        }

        private void OnBuffApplied(BuffAppliedArgs e)
        {
            if (Player == null) return;
            if (e.IconId >= BuffTable.Table.Length) return;

            bool alreadyExists = Player.IsBuffIconExists((BuffIconType)e.IconType);
            Player.AddBuff((BuffIconType)e.IconType, BuffTable.Table[e.IconId], e.Timer, e.Text);

            if (!alreadyExists)
            {
                UIManager.GetGump<BuffGump>()?.RequestUpdateContents();
            }
        }

        private void OnBuffRemoved(BuffRemovedArgs e)
        {
            if (Player == null) return;

            Player.RemoveBuff((BuffIconType)e.IconType);
            UIManager.GetGump<BuffGump>()?.RequestUpdateContents();
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
