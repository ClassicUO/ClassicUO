// SPDX-License-Identifier: BSD-2-Clause

using System;
using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.IO;
using ClassicUO.Resources;
using ClassicUO.Utility.Logging;
using Microsoft.Xna.Framework;

namespace ClassicUO.Network
{
    internal sealed partial class PacketHandlers
    {
        internal static void RegisterItemsHandlers(PacketHandlers h)
        {
            h.Add(0x1A, UpdateItem);
            h.Add(0x1D, DeleteObject);
            h.Add(0x23, DragAnimation);
            h.Add(0x24, OpenContainer);
            h.Add(0x25, UpdateContainedItem);
            h.Add(0x27, DenyMoveItem);
            h.Add(0x28, EndDraggingItem);
            h.Add(0x29, DropItemAccepted);
            h.Add(0x2E, EquipItem);
            h.Add(0x3C, UpdateContainedItems);
            h.Add(0x95, DyeData);
            h.Add(0xF3, UpdateItemSA);
            h.Add(0xF7, PacketList);
        }

        private static void UpdateItem(World world, ref StackDataReader p)
        {
            if (world.Player == null)
            {
                return;
            }

            uint serial = p.ReadUInt32BE();
            ushort count = 0;
            byte graphicInc = 0;
            byte direction = 0;
            ushort hue = 0;
            byte flags = 0;
            byte type = 0;

            if ((serial & 0x80000000) != 0)
            {
                serial &= 0x7FFFFFFF;
                count = 1;
            }

            ushort graphic = p.ReadUInt16BE();

            if ((graphic & 0x8000) != 0)
            {
                graphic &= 0x7FFF;
                graphicInc = p.ReadUInt8();
            }

            if (count > 0)
            {
                count = p.ReadUInt16BE();
            }
            else
            {
                count++;
            }

            ushort x = p.ReadUInt16BE();

            if ((x & 0x8000) != 0)
            {
                x &= 0x7FFF;
                direction = 1;
            }

            ushort y = p.ReadUInt16BE();

            if ((y & 0x8000) != 0)
            {
                y &= 0x7FFF;
                hue = 1;
            }

            if ((y & 0x4000) != 0)
            {
                y &= 0x3FFF;
                flags = 1;
            }

            if (direction != 0)
            {
                direction = p.ReadUInt8();
            }

            sbyte z = p.ReadInt8();

            if (hue != 0)
            {
                hue = p.ReadUInt16BE();
            }

            if (flags != 0)
            {
                flags = p.ReadUInt8();
            }

            //if (graphic != 0x2006)
            //    graphic += graphicInc;

            if (graphic >= 0x4000)
            {
                //graphic -= 0x4000;
                type = 2;
            }

            UpdateGameObject(
                world,
                serial,
                graphic,
                graphicInc,
                count,
                x,
                y,
                z,
                (Direction)direction,
                hue,
                (Flags)flags,
                count,
                type,
                1
            );
        }

        private static void DeleteObject(World world, ref StackDataReader p)
        {
            if (world.Player == null)
            {
                return;
            }

            uint serial = p.ReadUInt32BE();

            if (world.Player == serial)
            {
                return;
            }

            Entity entity = world.Get(serial);

            if (entity == null)
            {
                return;
            }

            bool updateAbilities = false;

            if (entity is Item it)
            {
                uint cont = it.Container & 0x7FFFFFFF;

                if (SerialHelper.IsValid(it.Container))
                {
                    Entity top = world.Get(it.RootContainer);

                    if (top != null)
                    {
                        if (top == world.Player)
                        {
                            updateAbilities =
                                it.Layer == Layer.OneHanded || it.Layer == Layer.TwoHanded;
                            Item tradeBoxItem = world.Player.GetSecureTradeBox();

                            if (tradeBoxItem != null)
                            {
                                UIManager.GetTradingGump(tradeBoxItem)?.RequestUpdateContents();
                            }
                        }
                    }

                    if (cont == world.Player && it.Layer == Layer.Invalid)
                    {
                        Client.Game.UO.GameCursor.ItemHold.Enabled = false;
                    }

                    if (it.Layer != Layer.Invalid)
                    {
                        UIManager.GetGump<PaperDollGump>(cont)?.RequestUpdateContents();
                    }

                    UIManager.GetGump<ContainerGump>(cont)?.RequestUpdateContents();

                    if (
                        top != null
                        && top.Graphic == 0x2006
                        && (
                            ProfileManager.CurrentProfile.GridLootType == 1
                            || ProfileManager.CurrentProfile.GridLootType == 2
                        )
                    )
                    {
                        UIManager.GetGump<GridLootGump>(cont)?.RequestUpdateContents();
                    }

                    if (it.Graphic == 0x0EB0)
                    {
                        UIManager.GetGump<BulletinBoardItem>(serial)?.Dispose();

                        BulletinBoardGump bbgump = UIManager.GetGump<BulletinBoardGump>();

                        if (bbgump != null)
                        {
                            bbgump.RemoveBulletinObject(serial);
                        }
                    }
                }
            }

            if (world.CorpseManager.Exists(0, serial))
            {
                return;
            }

            if (entity is Mobile m)
            {
                if (world.Party.Contains(serial))
                {
                    // m.RemoveFromTile();
                }

                // else
                {
                    //BaseHealthBarGump bar = UIManager.GetGump<BaseHealthBarGump>(serial);

                    //if (bar == null)
                    //{
                    //    NetClient.Socket.Send(new PCloseStatusBarGump(serial));
                    //}

                    world.RemoveMobile(serial, true);
                }
            }
            else
            {
                Item item = (Item)entity;

                if (item.IsMulti)
                {
                    world.HouseManager.Remove(serial);
                }

                Entity cont = world.Get(item.Container);

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

                world.RemoveItem(serial, true);

                if (updateAbilities)
                {
                    world.Player.UpdateAbilities();
                }
            }
        }

        private static void DragAnimation(World world, ref StackDataReader p)
        {
            ushort graphic = p.ReadUInt16BE();
            graphic += p.ReadUInt8();
            ushort hue = p.ReadUInt16BE();
            ushort count = p.ReadUInt16BE();
            uint source = p.ReadUInt32BE();
            ushort sourceX = p.ReadUInt16BE();
            ushort sourceY = p.ReadUInt16BE();
            sbyte sourceZ = p.ReadInt8();
            uint dest = p.ReadUInt32BE();
            ushort destX = p.ReadUInt16BE();
            ushort destY = p.ReadUInt16BE();
            sbyte destZ = p.ReadInt8();

            if (graphic == 0x0EED)
            {
                graphic = 0x0EEF;
            }
            else if (graphic == 0x0EEA)
            {
                graphic = 0x0EEC;
            }
            else if (graphic == 0x0EF0)
            {
                graphic = 0x0EF2;
            }

            Mobile entity = world.Mobiles.Get(source);

            if (entity == null)
            {
                source = 0;
            }
            else
            {
                sourceX = entity.X;
                sourceY = entity.Y;
                sourceZ = entity.Z;
            }

            Mobile destEntity = world.Mobiles.Get(dest);

            if (destEntity == null)
            {
                dest = 0;
            }
            else
            {
                destX = destEntity.X;
                destY = destEntity.Y;
                destZ = destEntity.Z;
            }

            world.SpawnEffect(
                !SerialHelper.IsValid(source) || !SerialHelper.IsValid(dest)
                    ? GraphicEffectType.Moving
                    : GraphicEffectType.DragEffect,
                source,
                dest,
                graphic,
                hue,
                sourceX,
                sourceY,
                sourceZ,
                destX,
                destY,
                destZ,
                5,
                5000,
                true,
                false,
                false,
                GraphicEffectBlendMode.Normal
            );

            //if (effect.AnimDataFrame.FrameCount != 0)
            //{
            //    effect.IntervalInMs = (uint) (effect.AnimDataFrame.FrameInterval * 45);
            //}
            //else
            //{
            //    effect.IntervalInMs = 13;
            //}
        }

        private static void OpenContainer(World world, ref StackDataReader p)
        {
            if (world.Player == null)
            {
                return;
            }

            uint serial = p.ReadUInt32BE();
            ushort graphic = p.ReadUInt16BE();

            if (graphic == 0xFFFF)
            {
                Item spellBookItem = world.Items.Get(serial);

                if (spellBookItem == null)
                {
                    return;
                }

                UIManager.GetGump<SpellbookGump>(serial)?.Dispose();

                SpellbookGump spellbookGump = new SpellbookGump(world, spellBookItem);

                if (!UIManager.GetGumpCachePosition(spellBookItem, out Point location))
                {
                    location = new Point(64, 64);
                }

                spellbookGump.Location = location;
                UIManager.Add(spellbookGump);

                Client.Game.Audio.PlaySound(0x0055);
            }
            else if (graphic == 0x0030)
            {
                Mobile vendor = world.Mobiles.Get(serial);

                if (vendor == null)
                {
                    return;
                }

                UIManager.GetGump<ShopGump>(serial)?.Dispose();

                ShopGump gump = new ShopGump(world, serial, true, 150, 5);
                UIManager.Add(gump);

                for (Layer layer = Layer.ShopBuyRestock; layer < Layer.ShopBuy + 1; layer++)
                {
                    Item item = vendor.FindItemByLayer(layer);

                    LinkedObject first = item.Items;

                    if (first == null)
                    {
                        //Log.Warn("buy item not found");
                        continue;
                    }

                    bool reverse = item.Graphic != 0x2AF8; //hardcoded logic in original client that we must match

                    if (reverse)
                    {
                        while (first?.Next != null)
                        {
                            first = first.Next;
                        }
                    }

                    while (first != null)
                    {
                        Item it = (Item)first;

                        gump.AddItem(
                            it.Serial,
                            it.Graphic,
                            it.Hue,
                            it.Amount,
                            it.Price,
                            it.Name,
                            false
                        );

                        if (reverse)
                        {
                            first = first.Previous;
                        }
                        else
                        {
                            first = first.Next;
                        }
                    }
                }
            }
            else
            {
                Item item = world.Items.Get(serial);

                if (item != null)
                {
                    if (
                        item.IsCorpse
                        && (
                            ProfileManager.CurrentProfile.GridLootType == 1
                            || ProfileManager.CurrentProfile.GridLootType == 2
                        )
                    )
                    {
                        //UIManager.GetGump<GridLootGump>(serial)?.Dispose();
                        //UIManager.Add(new GridLootGump(serial));
                        _requestedGridLoot = serial;

                        if (ProfileManager.CurrentProfile.GridLootType == 1)
                        {
                            return;
                        }
                    }

                    ContainerGump container = UIManager.GetGump<ContainerGump>(serial);
                    bool playsound = false;
                    int x,
                        y;

                    // TODO: check client version ?
                    if (
                        Client.Game.UO.Version >= Utility.ClientVersion.CV_706000
                        && ProfileManager.CurrentProfile != null
                        && ProfileManager.CurrentProfile.UseLargeContainerGumps
                    )
                    {
                        var gumps = Client.Game.UO.Gumps;

                        switch (graphic)
                        {
                            case 0x0048:
                                if (gumps.GetGump(0x06E8).Texture != null)
                                {
                                    graphic = 0x06E8;
                                }

                                break;

                            case 0x0049:
                                if (gumps.GetGump(0x9CDF).Texture != null)
                                {
                                    graphic = 0x9CDF;
                                }

                                break;

                            case 0x0051:
                                if (gumps.GetGump(0x06E7).Texture != null)
                                {
                                    graphic = 0x06E7;
                                }

                                break;

                            case 0x003E:
                                if (gumps.GetGump(0x06E9).Texture != null)
                                {
                                    graphic = 0x06E9;
                                }

                                break;

                            case 0x004D:
                                if (gumps.GetGump(0x06EA).Texture != null)
                                {
                                    graphic = 0x06EA;
                                }

                                break;

                            case 0x004E:
                                if (gumps.GetGump(0x06E6).Texture != null)
                                {
                                    graphic = 0x06E6;
                                }

                                break;

                            case 0x004F:
                                if (gumps.GetGump(0x06E5).Texture != null)
                                {
                                    graphic = 0x06E5;
                                }

                                break;

                            case 0x004A:
                                if (gumps.GetGump(0x9CDD).Texture != null)
                                {
                                    graphic = 0x9CDD;
                                }

                                break;

                            case 0x0044:
                                if (gumps.GetGump(0x9CE3).Texture != null)
                                {
                                    graphic = 0x9CE3;
                                }

                                break;
                        }
                    }

                    if (container != null)
                    {
                        x = container.ScreenCoordinateX;
                        y = container.ScreenCoordinateY;
                        container.Dispose();
                    }
                    else
                    {
                        world.ContainerManager.CalculateContainerPosition(serial, graphic);
                        x = world.ContainerManager.X;
                        y = world.ContainerManager.Y;
                        playsound = true;
                    }

                    UIManager.Add(
                        new ContainerGump(world, item, graphic, playsound)
                        {
                            X = x,
                            Y = y,
                            InvalidateContents = true
                        }
                    );

                    UIManager.RemovePosition(serial);
                }
                else
                {
                    Log.Error("[OpenContainer]: item not found");
                }
            }

            if (graphic != 0x0030)
            {
                Item it = world.Items.Get(serial);

                if (it != null)
                {
                    it.Opened = true;

                    if (!it.IsCorpse && graphic != 0xFFFF)
                    {
                        ClearContainerAndRemoveItems(world, it);
                    }
                }
            }
        }

        private static void UpdateContainedItem(World world, ref StackDataReader p)
        {
            if (!world.InGame)
            {
                return;
            }

            uint serial = p.ReadUInt32BE();
            ushort graphic = (ushort)(p.ReadUInt16BE() + p.ReadUInt8());
            ushort amount = Math.Max((ushort)1, p.ReadUInt16BE());
            ushort x = p.ReadUInt16BE();
            ushort y = p.ReadUInt16BE();

            if (Client.Game.UO.Version >= Utility.ClientVersion.CV_6017)
            {
                p.Skip(1);
            }

            uint containerSerial = p.ReadUInt32BE();
            ushort hue = p.ReadUInt16BE();

            AddItemToContainer(world, serial, graphic, amount, x, y, hue, containerSerial);
        }

        private static void DenyMoveItem(World world, ref StackDataReader p)
        {
            if (!world.InGame)
            {
                return;
            }

            Item firstItem = world.Items.Get(Client.Game.UO.GameCursor.ItemHold.Serial);

            if (
                Client.Game.UO.GameCursor.ItemHold.Enabled
                || Client.Game.UO.GameCursor.ItemHold.Dropped
                    && (firstItem == null || !firstItem.AllowedToDraw)
            )
            {
                if (world.ObjectToRemove == Client.Game.UO.GameCursor.ItemHold.Serial)
                {
                    world.ObjectToRemove = 0;
                }

                if (
                    SerialHelper.IsValid(Client.Game.UO.GameCursor.ItemHold.Serial)
                    && Client.Game.UO.GameCursor.ItemHold.Graphic != 0xFFFF
                )
                {
                    if (!Client.Game.UO.GameCursor.ItemHold.UpdatedInWorld)
                    {
                        if (
                            Client.Game.UO.GameCursor.ItemHold.Layer == Layer.Invalid
                            && SerialHelper.IsValid(Client.Game.UO.GameCursor.ItemHold.Container)
                        )
                        {
                            // Server should send an UpdateContainedItem after this packet.
                            Console.WriteLine("=== DENY === ADD TO CONTAINER");

                            AddItemToContainer(
                                world,
                                Client.Game.UO.GameCursor.ItemHold.Serial,
                                Client.Game.UO.GameCursor.ItemHold.Graphic,
                                Client.Game.UO.GameCursor.ItemHold.TotalAmount,
                                Client.Game.UO.GameCursor.ItemHold.X,
                                Client.Game.UO.GameCursor.ItemHold.Y,
                                Client.Game.UO.GameCursor.ItemHold.Hue,
                                Client.Game.UO.GameCursor.ItemHold.Container
                            );

                            UIManager
                                .GetGump<ContainerGump>(Client.Game.UO.GameCursor.ItemHold.Container)
                                ?.RequestUpdateContents();
                        }
                        else
                        {
                            Item item = world.GetOrCreateItem(
                                Client.Game.UO.GameCursor.ItemHold.Serial
                            );

                            item.Graphic = Client.Game.UO.GameCursor.ItemHold.Graphic;
                            item.Hue = Client.Game.UO.GameCursor.ItemHold.Hue;
                            item.Amount = Client.Game.UO.GameCursor.ItemHold.TotalAmount;
                            item.Flags = Client.Game.UO.GameCursor.ItemHold.Flags;
                            item.Layer = Client.Game.UO.GameCursor.ItemHold.Layer;
                            item.X = Client.Game.UO.GameCursor.ItemHold.X;
                            item.Y = Client.Game.UO.GameCursor.ItemHold.Y;
                            item.Z = Client.Game.UO.GameCursor.ItemHold.Z;
                            item.CheckGraphicChange();

                            Entity container = world.Get(Client.Game.UO.GameCursor.ItemHold.Container);

                            if (container != null)
                            {
                                if (SerialHelper.IsMobile(container.Serial))
                                {
                                    Console.WriteLine("=== DENY === ADD TO PAPERDOLL");

                                    world.RemoveItemFromContainer(item);
                                    container.PushToBack(item);
                                    item.Container = container.Serial;

                                    UIManager
                                        .GetGump<PaperDollGump>(item.Container)
                                        ?.RequestUpdateContents();
                                }
                                else
                                {
                                    Console.WriteLine("=== DENY === SOMETHING WRONG");

                                    world.RemoveItem(item, true);
                                }
                            }
                            else
                            {
                                Console.WriteLine("=== DENY === ADD TO TERRAIN");

                                world.RemoveItemFromContainer(item);

                                item.SetInWorldTile(item.X, item.Y, item.Z);
                            }
                        }
                    }
                }
                else
                {
                    Log.Error(
                        $"Wrong data: serial = {Client.Game.UO.GameCursor.ItemHold.Serial:X8}  -  graphic = {Client.Game.UO.GameCursor.ItemHold.Graphic:X4}"
                    );
                }

                UIManager.GetGump<SplitMenuGump>(Client.Game.UO.GameCursor.ItemHold.Serial)?.Dispose();

                Client.Game.UO.GameCursor.ItemHold.Clear();
            }
            else
            {
                Log.Warn("There was a problem with ItemHold object. It was cleared before :|");
            }

            //var result = World.Items.Get(ItemHold.Serial);

            //if (result != null && !result.IsDestroyed)
            //    result.AllowedToDraw = true;

            byte code = p.ReadUInt8();

            if (code < 5)
            {
                world.MessageManager.HandleMessage(
                    null,
                    ServerErrorMessages.GetError(p[0], code),
                    string.Empty,
                    0x03b2,
                    MessageType.System,
                    3,
                    TextType.SYSTEM
                );
            }
        }

        private static void EndDraggingItem(World world, ref StackDataReader p)
        {
            if (!world.InGame)
            {
                return;
            }

            Client.Game.UO.GameCursor.ItemHold.Enabled = false;
            Client.Game.UO.GameCursor.ItemHold.Dropped = false;
        }

        private static void DropItemAccepted(World world, ref StackDataReader p)
        {
            if (!world.InGame)
            {
                return;
            }

            Client.Game.UO.GameCursor.ItemHold.Enabled = false;
            Client.Game.UO.GameCursor.ItemHold.Dropped = false;

            Console.WriteLine("PACKET - ITEM DROP OK!");
        }

        private static void EquipItem(World world, ref StackDataReader p)
        {
            if (!world.InGame)
            {
                return;
            }

            uint serial = p.ReadUInt32BE();

            Item item = world.GetOrCreateItem(serial);

            if (item.Graphic != 0 && item.Layer != Layer.Backpack)
            {
                //ClearContainerAndRemoveItems(item);
                world.RemoveItemFromContainer(item);
            }

            if (SerialHelper.IsValid(item.Container))
            {
                UIManager.GetGump<ContainerGump>(item.Container)?.RequestUpdateContents();

                UIManager.GetGump<PaperDollGump>(item.Container)?.RequestUpdateContents();
            }

            item.Graphic = (ushort)(p.ReadUInt16BE() + p.ReadInt8());
            item.Layer = (Layer)p.ReadUInt8();
            item.Container = p.ReadUInt32BE();
            item.FixHue(p.ReadUInt16BE());
            item.Amount = 1;

            Entity entity = world.Get(item.Container);

            entity?.PushToBack(item);

            if (item.Layer >= Layer.ShopBuyRestock && item.Layer <= Layer.ShopSell)
            {
                //item.Clear();
            }
            else if (SerialHelper.IsValid(item.Container) && item.Layer < Layer.Mount)
            {
                UIManager.GetGump<PaperDollGump>(item.Container)?.RequestUpdateContents();
            }

            if (
                entity == world.Player
                && (item.Layer == Layer.OneHanded || item.Layer == Layer.TwoHanded)
            )
            {
                world.Player?.UpdateAbilities();
            }

            //if (ItemHold.Serial == item.Serial)
            //{
            //    Console.WriteLine("PACKET - ITEM EQUIP");
            //    ItemHold.Enabled = false;
            //    ItemHold.Dropped = true;
            //}
        }

        private static void UpdateContainedItems(World world, ref StackDataReader p)
        {
            if (!world.InGame)
            {
                return;
            }

            ushort count = p.ReadUInt16BE();

            for (int i = 0; i < count; i++)
            {
                uint serial = p.ReadUInt32BE();
                ushort graphic = (ushort)(p.ReadUInt16BE() + p.ReadUInt8());
                ushort amount = Math.Max(p.ReadUInt16BE(), (ushort)1);
                ushort x = p.ReadUInt16BE();
                ushort y = p.ReadUInt16BE();

                if (Client.Game.UO.Version >= Utility.ClientVersion.CV_6017)
                {
                    p.Skip(1);
                }

                uint containerSerial = p.ReadUInt32BE();
                ushort hue = p.ReadUInt16BE();

                if (i == 0)
                {
                    Entity container = world.Get(containerSerial);

                    if (container != null)
                    {
                        ClearContainerAndRemoveItems(world, container, container.Graphic == 0x2006);
                    }
                }

                AddItemToContainer(world, serial, graphic, amount, x, y, hue, containerSerial);
            }
        }

        private static void DyeData(World world, ref StackDataReader p)
        {
            uint serial = p.ReadUInt32BE();
            p.Skip(2);
            ushort graphic = p.ReadUInt16BE();

            ref readonly var gumpInfo = ref Client.Game.UO.Gumps.GetGump(0x0906);

            int x = (Client.Game.ClientBounds.Width >> 1) - (gumpInfo.UV.Width >> 1);
            int y = (Client.Game.ClientBounds.Height >> 1) - (gumpInfo.UV.Height >> 1);

            ColorPickerGump gump = UIManager.GetGump<ColorPickerGump>(serial);

            if (gump == null || gump.IsDisposed || gump.Graphic != graphic)
            {
                gump?.Dispose();

                gump = new ColorPickerGump(world, serial, graphic, x, y, null);

                UIManager.Add(gump);
            }
        }

        private static void UpdateItemSA(World world, ref StackDataReader p)
        {
            if (world.Player == null)
            {
                return;
            }

            p.Skip(2);
            byte type = p.ReadUInt8();
            uint serial = p.ReadUInt32BE();
            ushort graphic = p.ReadUInt16BE();
            byte graphicInc = p.ReadUInt8();
            ushort amount = p.ReadUInt16BE();
            ushort unk = p.ReadUInt16BE();
            ushort x = p.ReadUInt16BE();
            ushort y = p.ReadUInt16BE();
            sbyte z = p.ReadInt8();
            Direction dir = (Direction)p.ReadUInt8();
            ushort hue = p.ReadUInt16BE();
            Flags flags = (Flags)p.ReadUInt8();
            ushort unk2 = p.ReadUInt16BE();

            if (serial != world.Player)
            {
                UpdateGameObject(
                    world,
                    serial,
                    graphic,
                    graphicInc,
                    amount,
                    x,
                    y,
                    z,
                    dir,
                    hue,
                    flags,
                    unk,
                    type,
                    unk2
                );

                if (graphic == 0x2006 && ProfileManager.CurrentProfile.AutoOpenCorpses)
                {
                    world.Player.TryOpenCorpses();
                }
            }
            else if (p[0] == 0xF7)
            {
                UpdatePlayer(world, serial, graphic, graphicInc, hue, flags, x, y, z, 0, dir);
            }
        }

        private static void PacketList(World world, ref StackDataReader p)
        {
            if (world.Player == null)
            {
                return;
            }

            int count = p.ReadUInt16BE();

            for (int i = 0; i < count; i++)
            {
                byte id = p.ReadUInt8();

                if (id == 0xF3)
                {
                    UpdateItemSA(world, ref p);
                }
                else
                {
                    Log.Warn($"Unknown packet ID: [0x{id:X2}] in 0xF7");

                    break;
                }
            }
        }
    }
}
