// SPDX-License-Identifier: BSD-2-Clause

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
        private void SubscribeMobileUpdates()
        {
            EventSink.PlayerUpdated += OnPlayerUpdated;
            EventSink.MobileUpdated += OnMobileUpdated;
            EventSink.ItemUpdated += OnItemUpdated;
        }

        private void UnsubscribeMobileUpdates()
        {
            EventSink.PlayerUpdated -= OnPlayerUpdated;
            EventSink.MobileUpdated -= OnMobileUpdated;
            EventSink.ItemUpdated -= OnItemUpdated;
        }

        private void OnPlayerUpdated(PlayerUpdatedArgs e)
        {
            if (Player == null) return;

            UpdatePlayer(
                e.Serial,
                e.Graphic,
                e.GraphicIncrement,
                e.Hue,
                e.Flags,
                e.X,
                e.Y,
                e.Z,
                e.ServerId,
                e.Direction
            );
        }

        private void OnMobileUpdated(MobileUpdatedArgs e)
        {
            if (Player == null) return;

            if (e.IsFullObject)
            {
                OnUpdateObject(e);
            }
            else
            {
                OnUpdateCharacter(e);
            }
        }

        private void OnUpdateCharacter(MobileUpdatedArgs e)
        {
            Mobile mobile = Mobiles.Get(e.Serial);
            if (mobile == null) return;

            mobile.NotorietyFlag = e.Notoriety;

            if (e.Serial == Player)
            {
                mobile.Flags = e.Flags;
                mobile.Graphic = e.Graphic;
                mobile.CheckGraphicChange();
                mobile.FixHue(e.Hue);
                // TODO: x,y,z, direction cause elastic effect, ignore 'em for the moment
            }
            else
            {
                UpdateGameObject(e.Serial, e.Graphic, 0, 0, e.X, e.Y, e.Z, e.Direction, e.Hue, e.Flags, 0, 1, 1);
            }
        }

        private void OnUpdateObject(MobileUpdatedArgs e)
        {
            bool oldDead = false;

            if (e.Serial == Player)
            {
                oldDead = Player.IsDead;
                Player.Graphic = e.Graphic;
                Player.CheckGraphicChange();
                Player.FixHue(e.Hue);
                Player.Flags = e.Flags;
            }
            else
            {
                UpdateGameObject(e.Serial, e.Graphic, 0, 0, e.X, e.Y, e.Z, e.Direction, e.Hue, e.Flags, 0, 0, 1);
            }

            Entity obj = Get(e.Serial);

            if (obj == null)
            {
                return;
            }

            if (!obj.IsEmpty)
            {
                LinkedObject o = obj.Items;

                while (o != null)
                {
                    LinkedObject next = o.Next;
                    Item it = (Item)o;

                    if (!it.Opened && it.Layer != Layer.Backpack)
                    {
                        RemoveItem(it.Serial, true);
                    }

                    o = next;
                }
            }

            if (SerialHelper.IsMobile(e.Serial) && obj is Mobile mob)
            {
                mob.NotorietyFlag = e.Notoriety;

                UIManager.GetGump<PaperDollGump>(e.Serial)?.RequestUpdateContents();
            }

            if (e.Equipment != null)
            {
                for (int i = 0; i < e.Equipment.Count; i++)
                {
                    var entry = e.Equipment[i];

                    Item item = GetOrCreateItem(entry.Serial);
                    item.Graphic = entry.Graphic;
                    item.FixHue(entry.Hue);
                    item.Amount = 1;
                    RemoveItemFromContainer(item);
                    item.Container = e.Serial;
                    item.Layer = entry.Layer;

                    item.CheckGraphicChange();

                    obj.PushToBack(item);
                }
            }

            if (e.Serial == Player)
            {
                if (oldDead != Player.IsDead)
                {
                    if (Player.IsDead)
                    {
                        // NOTE: This packet causes some weird issue on sphere servers.
                        //       When the character dies, this packet trigger a "reset" and
                        //       somehow it messes up the packet reading server side
                        //NetClient.Socket.Send_DeathScreen();
                        ChangeSeason(Seasons.Season.Desolation, 42);
                    }
                    else
                    {
                        ChangeSeason(OldSeason, OldMusicIndex);
                    }
                }

                UIManager.GetGump<PaperDollGump>(e.Serial)?.RequestUpdateContents();

                Player.UpdateAbilities();
            }
        }

        private void OnItemUpdated(ItemUpdatedArgs e)
        {
            if (Player == null) return;

            if (e.IsItemSA)
            {
                if (e.Serial != Player)
                {
                    UpdateGameObject(
                        e.Serial,
                        e.Graphic,
                        e.GraphicInc,
                        e.Amount,
                        e.X,
                        e.Y,
                        e.Z,
                        e.Direction,
                        e.Hue,
                        e.Flags,
                        e.Unk,
                        e.Type,
                        e.Unk2
                    );

                    if (e.Graphic == 0x2006 && ProfileManager.CurrentProfile != null && ProfileManager.CurrentProfile.AutoOpenCorpses)
                    {
                        Player.TryOpenCorpses();
                    }
                }
                else if (e.IsFromPacketList)
                {
                    UpdatePlayer(
                        e.Serial,
                        e.Graphic,
                        e.GraphicInc,
                        e.Hue,
                        e.Flags,
                        e.X,
                        e.Y,
                        e.Z,
                        0,
                        e.Direction
                    );
                }
            }
            else
            {
                UpdateGameObject(
                    e.Serial,
                    e.Graphic,
                    e.GraphicInc,
                    e.Amount,
                    e.X,
                    e.Y,
                    e.Z,
                    e.Direction,
                    e.Hue,
                    e.Flags,
                    e.Unk,
                    e.Type,
                    e.Unk2
                );
            }
        }
    }
}
