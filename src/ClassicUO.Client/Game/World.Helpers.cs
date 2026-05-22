// SPDX-License-Identifier: BSD-2-Clause

using System;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Network;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Game
{
    internal sealed partial class World
    {
        public void AddItemToContainer(
            uint serial,
            ushort graphic,
            ushort amount,
            ushort x,
            ushort y,
            ushort hue,
            uint containerSerial
        )
        {
            if (Client.Game.UO.GameCursor.ItemHold.Serial == serial)
            {
                if (Client.Game.UO.GameCursor.ItemHold.Dropped)
                {
                    Console.WriteLine("ADD ITEM TO CONTAINER -- CLEAR HOLD");
                    Client.Game.UO.GameCursor.ItemHold.Clear();
                }

                //else if (ItemHold.Graphic == graphic && ItemHold.Amount == amount &&
                //         ItemHold.Container == containerSerial)
                //{
                //    ItemHold.Enabled = false;
                //    ItemHold.Dropped = false;
                //}
            }

            Entity container = Get(containerSerial);

            if (container == null)
            {
                Log.Warn($"No container ({containerSerial}) found");

                //container = GetOrCreateItem(containerSerial);
                return;
            }

            Item item = Items.Get(serial);

            if (SerialHelper.IsMobile(serial))
            {
                RemoveMobile(serial, true);
                Log.Warn("AddItemToContainer function adds mobile as Item");
            }

            if (item != null && (container.Graphic != 0x2006 || item.Layer == Layer.Invalid))
            {
                RemoveItem(item, true);
            }

            item = GetOrCreateItem(serial);
            item.Graphic = graphic;
            item.CheckGraphicChange();
            item.Amount = amount;
            item.FixHue(hue);
            item.X = x;
            item.Y = y;
            item.Z = 0;

            RemoveItemFromContainer(item);
            item.Container = containerSerial;
            container.PushToBack(item);

            if (SerialHelper.IsMobile(containerSerial))
            {
                Mobile m = Mobiles.Get(containerSerial);
                Item secureBox = m?.GetSecureTradeBox();

                if (secureBox != null)
                {
                    UIManager.GetTradingGump(secureBox)?.RequestUpdateContents();
                }
                else
                {
                    UIManager.GetGump<PaperDollGump>(containerSerial)?.RequestUpdateContents();
                }
            }
            else if (SerialHelper.IsItem(containerSerial))
            {
                Gump gump = UIManager.GetGump<BulletinBoardGump>(containerSerial);

                if (gump != null)
                {
                    NetClient.Socket.Send_BulletinBoardRequestMessageSummary(
                        containerSerial,
                        serial
                    );
                }
                else
                {
                    gump = UIManager.GetGump<SpellbookGump>(containerSerial);

                    if (gump == null)
                    {
                        gump = UIManager.GetGump<ContainerGump>(containerSerial);

                        if (gump != null)
                        {
                            ((ContainerGump)gump).CheckItemControlPosition(item);
                        }

                        if (ProfileManager.CurrentProfile.GridLootType > 0)
                        {
                            GridLootGump grid_gump = UIManager.GetGump<GridLootGump>(
                                containerSerial
                            );

                            if (
                                grid_gump == null
                                && SerialHelper.IsValid(ContainerManager.PendingGridLootSerial)
                                && ContainerManager.PendingGridLootSerial == containerSerial
                            )
                            {
                                grid_gump = new GridLootGump(this, ContainerManager.PendingGridLootSerial);
                                UIManager.Add(grid_gump);
                                ContainerManager.PendingGridLootSerial = 0;
                            }

                            grid_gump?.RequestUpdateContents();
                        }
                    }

                    if (gump != null)
                    {
                        if (SerialHelper.IsItem(containerSerial))
                        {
                            ((Item)container).Opened = true;
                        }

                        gump.RequestUpdateContents();
                    }
                }
            }

            UIManager.GetTradingGump(containerSerial)?.RequestUpdateContents();
        }

        public void UpdateGameObject(
            uint serial,
            ushort graphic,
            byte graphic_inc,
            ushort count,
            ushort x,
            ushort y,
            sbyte z,
            Direction direction,
            ushort hue,
            Flags flagss,
            int UNK,
            byte type,
            ushort UNK_2
        )
        {
            Mobile mobile = null;
            Item item = null;
            Entity obj = Get(serial);

            if (
                Client.Game.UO.GameCursor.ItemHold.Enabled
                && Client.Game.UO.GameCursor.ItemHold.Serial == serial
            )
            {
                if (SerialHelper.IsValid(Client.Game.UO.GameCursor.ItemHold.Container))
                {
                    if (Client.Game.UO.GameCursor.ItemHold.Layer == 0)
                    {
                        UIManager
                            .GetGump<ContainerGump>(Client.Game.UO.GameCursor.ItemHold.Container)
                            ?.RequestUpdateContents();
                    }
                    else
                    {
                        UIManager
                            .GetGump<PaperDollGump>(Client.Game.UO.GameCursor.ItemHold.Container)
                            ?.RequestUpdateContents();
                    }
                }

                Client.Game.UO.GameCursor.ItemHold.UpdatedInWorld = true;
            }

            bool created = false;

            if (obj == null || obj.IsDestroyed)
            {
                created = true;

                if (SerialHelper.IsMobile(serial) && type != 3)
                {
                    mobile = GetOrCreateMobile(serial);

                    if (mobile == null)
                    {
                        return;
                    }

                    obj = mobile;
                    mobile.Graphic = (ushort)(graphic + graphic_inc);
                    mobile.CheckGraphicChange();
                    mobile.Direction = direction & Direction.Up;
                    mobile.FixHue(hue);
                    mobile.X = x;
                    mobile.Y = y;
                    mobile.Z = z;
                    mobile.Flags = flagss;
                }
                else
                {
                    item = GetOrCreateItem(serial);

                    if (item == null)
                    {
                        return;
                    }

                    obj = item;
                }
            }
            else
            {
                if (obj is Item item1)
                {
                    item = item1;

                    if (SerialHelper.IsValid(item.Container))
                    {
                        RemoveItemFromContainer(item);
                    }
                }
                else
                {
                    mobile = (Mobile)obj;
                }
            }

            if (obj == null)
            {
                return;
            }

            if (item != null)
            {
                if (graphic != 0x2006)
                {
                    graphic += graphic_inc;
                }

                if (type == 2)
                {
                    item.IsMulti = true;
                    item.WantUpdateMulti =
                        (graphic & 0x3FFF) != item.Graphic
                        || item.X != x
                        || item.Y != y
                        || item.Z != z
                        || item.Hue != hue;
                    item.Graphic = (ushort)(graphic & 0x3FFF);
                }
                else
                {
                    item.IsDamageable = type == 3;
                    item.IsMulti = false;
                    item.Graphic = graphic;
                }

                item.X = x;
                item.Y = y;
                item.Z = z;
                item.LightID = (byte)direction;

                if (graphic == 0x2006)
                {
                    item.Layer = (Layer)direction;
                }

                item.FixHue(hue);

                if (count == 0)
                {
                    count = 1;
                }

                item.Amount = count;
                item.Flags = flagss;
                item.Direction = direction;
                item.CheckGraphicChange(item.AnimIndex);
            }
            else
            {
                graphic += graphic_inc;

                if (serial != Player)
                {
                    Direction cleaned_dir = direction & Direction.Up;
                    bool isrun = (direction & Direction.Running) != 0;

                    if (Get(mobile) == null || mobile.X == 0xFFFF && mobile.Y == 0xFFFF)
                    {
                        mobile.X = x;
                        mobile.Y = y;
                        mobile.Z = z;
                        mobile.Direction = cleaned_dir;
                        mobile.IsRunning = isrun;
                        mobile.ClearSteps();
                    }

                    if (!mobile.EnqueueStep(x, y, z, cleaned_dir, isrun))
                    {
                        mobile.X = x;
                        mobile.Y = y;
                        mobile.Z = z;
                        mobile.Direction = cleaned_dir;
                        mobile.IsRunning = isrun;
                        mobile.ClearSteps();
                    }
                }

                mobile.Graphic = (ushort)(graphic & 0x3FFF);
                mobile.FixHue(hue);
                mobile.Flags = flagss;
            }

            if (created && !obj.IsClicked)
            {
                if (mobile != null)
                {
                    if (ProfileManager.CurrentProfile.ShowNewMobileNameIncoming)
                    {
                        GameActions.SingleClick(this, serial);
                    }
                }
                else if (graphic == 0x2006)
                {
                    if (ProfileManager.CurrentProfile.ShowNewCorpseNameIncoming)
                    {
                        GameActions.SingleClick(this, serial);
                    }
                }
            }

            if (mobile != null)
            {
                mobile.SetInWorldTile(mobile.X, mobile.Y, mobile.Z);

                if (created)
                {
                    // This is actually a way to get all Hp from all new mobiles.
                    // Real UO client does it only when LastAttack == serial.
                    // We force to close suddenly.
                    GameActions.RequestMobileStatus(this, serial);

                    //if (TargetManager.LastAttack != serial)
                    //{
                    //    GameActions.SendCloseStatus(serial);
                    //}
                }
            }
            else
            {
                if (
                    Client.Game.UO.GameCursor.ItemHold.Serial == serial
                    && Client.Game.UO.GameCursor.ItemHold.Dropped
                )
                {
                    // we want maintain the item data due to the denymoveitem packet
                    //ItemHold.Clear();
                    Client.Game.UO.GameCursor.ItemHold.Enabled = false;
                    Client.Game.UO.GameCursor.ItemHold.Dropped = false;
                }

                if (item.OnGround)
                {
                    item.SetInWorldTile(item.X, item.Y, item.Z);

                    if (graphic == 0x2006 && ProfileManager.CurrentProfile.AutoOpenCorpses)
                    {
                        Player.TryOpenCorpses();
                    }
                }
            }
        }

        public void UpdatePlayer(
            uint serial,
            ushort graphic,
            byte graph_inc,
            ushort hue,
            Flags flags,
            ushort x,
            ushort y,
            sbyte z,
            ushort serverID,
            Direction direction
        )
        {
            if (serial == Player)
            {
                RangeSize.X = x;
                RangeSize.Y = y;

                bool olddead = Player.IsDead;
                ushort old_graphic = Player.Graphic;

                Player.CloseBank();
                Player.Walker.WalkingFailed = false;
                Player.Graphic = graphic;
                Player.Direction = direction & Direction.Mask;
                Player.FixHue(hue);
                Player.Flags = flags;
                Player.Walker.DenyWalk(0xFF, -1, -1, -1);

                GameScene gs = Client.Game.GetScene<GameScene>();

                if (gs != null)
                {
                    Weather.Reset();
                    gs.UpdateDrawPosition = true;
                }

                // std client keeps the target open!
                /*if (old_graphic != 0 && old_graphic != Player.Graphic)
                {
                    if (Player.IsDead)
                    {
                        TargetManager.Reset();
                    }
                }*/

                if (olddead != Player.IsDead)
                {
                    if (Player.IsDead)
                    {
                        ChangeSeason(Game.Managers.Season.Desolation, 42);
                    }
                    else
                    {
                        ChangeSeason(OldSeason, OldMusicIndex);
                    }
                }

                Player.Walker.ResendPacketResync = false;
                Player.CloseRangedGumps();
                Player.SetInWorldTile(x, y, z);
                Player.UpdateAbilities();
            }
        }

        public void ClearContainerAndRemoveItems(
            Entity container,
            bool remove_unequipped = false
        )
        {
            if (container == null || container.IsEmpty)
            {
                return;
            }

            LinkedObject first = container.Items;
            LinkedObject new_first = null;

            while (first != null)
            {
                LinkedObject next = first.Next;
                Item it = (Item)first;

                if (remove_unequipped && it.Layer != 0)
                {
                    if (new_first == null)
                    {
                        new_first = first;
                    }
                }
                else
                {
                    RemoveItem(it, true);
                }

                first = next;
            }

            container.Items = remove_unequipped ? new_first : null;
        }
    }
}
