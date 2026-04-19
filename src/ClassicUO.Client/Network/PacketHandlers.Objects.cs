// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.IO;
using Microsoft.Xna.Framework;

namespace ClassicUO.Network
{
    internal sealed partial class PacketHandlers
    {
        internal static void RegisterObjectsHandlers(PacketHandlers h)
        {
            h.Add(0x6E, CharacterAnimation);
            h.Add(0xE2, NewCharacterAnimation);
            h.Add(0x77, UpdateCharacter);
            h.Add(0xD2, UpdateCharacter);
            h.Add(0x78, UpdateObject);
            h.Add(0xD3, UpdateObject);
            h.Add(0x89, CorpseEquipment);
            h.Add(0x88, OpenPaperdoll);
            h.Add(0x98, UpdateName);
            h.Add(0xB8, CharacterProfile);
        }

        private static void CharacterAnimation(World world, ref StackDataReader p)
        {
            Mobile mobile = world.Mobiles.Get(p.ReadUInt32BE());

            if (mobile == null)
            {
                return;
            }

            ushort action = p.ReadUInt16BE();
            ushort frame_count = p.ReadUInt16BE();
            ushort repeat_count = p.ReadUInt16BE();
            bool forward = !p.ReadBool();
            bool repeat = p.ReadBool();
            byte delay = p.ReadUInt8();

            mobile.SetAnimation(
                Mobile.GetReplacedObjectAnimation(mobile.Graphic, action),
                delay,
                (byte)frame_count,
                (byte)repeat_count,
                repeat,
                forward,
                true
            );
        }

        private static void NewCharacterAnimation(World world, ref StackDataReader p)
        {
            if (world.Player == null)
            {
                return;
            }

            Mobile mobile = world.Mobiles.Get(p.ReadUInt32BE());

            if (mobile == null)
            {
                return;
            }

            ushort type = p.ReadUInt16BE();
            ushort action = p.ReadUInt16BE();
            byte mode = p.ReadUInt8();
            byte group = Mobile.GetObjectNewAnimation(mobile, type, action, mode);

            mobile.SetAnimation(
                group,
                repeatCount: 1,
                repeat: (type == 1 || type == 2) && mobile.Graphic == 0x0015,
                forward: true,
                fromServer: true
            );
        }

        private static void UpdateCharacter(World world, ref StackDataReader p)
        {
            if (world.Player == null)
            {
                return;
            }

            uint serial = p.ReadUInt32BE();
            Mobile mobile = world.Mobiles.Get(serial);

            if (mobile == null)
            {
                return;
            }

            ushort graphic = p.ReadUInt16BE();
            ushort x = p.ReadUInt16BE();
            ushort y = p.ReadUInt16BE();
            sbyte z = p.ReadInt8();
            Direction direction = (Direction)p.ReadUInt8();
            ushort hue = p.ReadUInt16BE();
            Flags flags = (Flags)p.ReadUInt8();
            NotorietyFlag notoriety = (NotorietyFlag)p.ReadUInt8();

            mobile.NotorietyFlag = notoriety;

            if (serial == world.Player)
            {
                mobile.Flags = flags;
                mobile.Graphic = graphic;
                mobile.CheckGraphicChange();
                mobile.FixHue(hue);
                // TODO: x,y,z, direction cause elastic effect, ignore 'em for the moment
            }
            else
            {
                UpdateGameObject(world, serial, graphic, 0, 0, x, y, z, direction, hue, flags, 0, 1, 1);
            }
        }

        private static void UpdateObject(World world, ref StackDataReader p)
        {
            if (world.Player == null)
            {
                return;
            }

            uint serial = p.ReadUInt32BE();
            ushort graphic = p.ReadUInt16BE();
            ushort x = p.ReadUInt16BE();
            ushort y = p.ReadUInt16BE();
            sbyte z = p.ReadInt8();
            Direction direction = (Direction)p.ReadUInt8();
            ushort hue = p.ReadUInt16BE();
            Flags flags = (Flags)p.ReadUInt8();
            NotorietyFlag notoriety = (NotorietyFlag)p.ReadUInt8();
            bool oldDead = false;
            //bool alreadyExists =world.Get(serial) != null;

            if (serial == world.Player)
            {
                oldDead = world.Player.IsDead;
                world.Player.Graphic = graphic;
                world.Player.CheckGraphicChange();
                world.Player.FixHue(hue);
                world.Player.Flags = flags;
            }
            else
            {
                UpdateGameObject(world, serial, graphic, 0, 0, x, y, z, direction, hue, flags, 0, 0, 1);
            }

            Entity obj = world.Get(serial);

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
                        world.RemoveItem(it.Serial, true);
                    }

                    o = next;
                }
            }

            if (SerialHelper.IsMobile(serial) && obj is Mobile mob)
            {
                mob.NotorietyFlag = notoriety;

                UIManager.GetGump<PaperDollGump>(serial)?.RequestUpdateContents();
            }

            if (p[0] != 0x78)
            {
                p.Skip(6);
            }

            uint itemSerial = p.ReadUInt32BE();

            while (itemSerial != 0 && p.Position < p.Length)
            {
                //if (!SerialHelper.IsItem(itemSerial))
                //    break;

                ushort itemGraphic = p.ReadUInt16BE();
                byte layer = p.ReadUInt8();
                ushort item_hue = 0;

                if (Client.Game.UO.Version >= Utility.ClientVersion.CV_70331)
                {
                    item_hue = p.ReadUInt16BE();
                }
                else if ((itemGraphic & 0x8000) != 0)
                {
                    itemGraphic &= 0x7FFF;
                    item_hue = p.ReadUInt16BE();
                }

                Item item = world.GetOrCreateItem(itemSerial);
                item.Graphic = itemGraphic;
                item.FixHue(item_hue);
                item.Amount = 1;
                world.RemoveItemFromContainer(item);
                item.Container = serial;
                item.Layer = (Layer)layer;

                item.CheckGraphicChange();

                obj.PushToBack(item);

                itemSerial = p.ReadUInt32BE();
            }

            if (serial == world.Player)
            {
                if (oldDead != world.Player.IsDead)
                {
                    if (world.Player.IsDead)
                    {
                        // NOTE: This packet causes some weird issue on sphere servers.
                        //       When the character dies, this packet trigger a "reset" and
                        //       somehow it messes up the packet reading server side
                        //NetClient.Socket.Send_DeathScreen();
                        world.ChangeSeason(Game.Managers.Season.Desolation, 42);
                    }
                    else
                    {
                        world.ChangeSeason(world.OldSeason, world.OldMusicIndex);
                    }
                }

                UIManager.GetGump<PaperDollGump>(serial)?.RequestUpdateContents();

                world.Player.UpdateAbilities();
            }
        }

        private static void OpenPaperdoll(World world, ref StackDataReader p)
        {
            Mobile mobile = world.Mobiles.Get(p.ReadUInt32BE());

            if (mobile == null)
            {
                return;
            }

            string text = p.ReadASCII(60);
            byte flags = p.ReadUInt8();

            mobile.Title = text;

            PaperDollGump paperdoll = UIManager.GetGump<PaperDollGump>(mobile);

            if (paperdoll == null)
            {
                if (!UIManager.GetGumpCachePosition(mobile, out Point location))
                {
                    location = new Point(100, 100);
                }

                UIManager.Add(
                    new PaperDollGump(world, mobile, (flags & 0x02) != 0) { Location = location }
                );
            }
            else
            {
                bool old = paperdoll.CanLift;
                bool newLift = (flags & 0x02) != 0;

                paperdoll.CanLift = newLift;
                paperdoll.UpdateTitle(text);

                if (old != newLift)
                {
                    paperdoll.RequestUpdateContents();
                }

                paperdoll.SetInScreen();
                paperdoll.BringOnTop();
            }
        }

        private static void CorpseEquipment(World world, ref StackDataReader p)
        {
            if (!world.InGame)
            {
                return;
            }

            uint serial = p.ReadUInt32BE();
            Entity corpse = world.Get(serial);

            if (corpse == null)
            {
                return;
            }

            // if it's not a corpse we should skip this [?]
            if (corpse.Graphic != 0x2006)
            {
                return;
            }

            Layer layer = (Layer)p.ReadUInt8();

            while (layer != Layer.Invalid && p.Position < p.Length)
            {
                uint item_serial = p.ReadUInt32BE();

                if (layer - 1 != Layer.Backpack)
                {
                    Item item = world.GetOrCreateItem(item_serial);

                    world.RemoveItemFromContainer(item);
                    item.Container = serial;
                    item.Layer = layer - 1;
                    corpse.PushToBack(item);
                }

                layer = (Layer)p.ReadUInt8();
            }
        }

        private static void UpdateName(World world, ref StackDataReader p)
        {
            if (!world.InGame)
            {
                return;
            }

            uint serial = p.ReadUInt32BE();
            string name = p.ReadASCII();

            WMapEntity wme = world.WMapManager.GetEntity(serial);

            if (wme != null && !string.IsNullOrEmpty(name))
            {
                wme.Name = name;
            }

            Entity entity = world.Get(serial);

            if (entity != null)
            {
                entity.Name = name;

                if (
                    serial == world.Player.Serial
                    && !string.IsNullOrEmpty(name)
                    && name != world.Player.Name
                )
                {
                    Client.Game.SetWindowTitle(name);
                }

                UIManager.GetGump<NameOverheadGump>(serial)?.SetName();
            }
        }

        private static void CharacterProfile(World world, ref StackDataReader p)
        {
            if (!world.InGame)
            {
                return;
            }

            uint serial = p.ReadUInt32BE();
            string header = p.ReadASCII();
            string footer = p.ReadUnicodeBE();

            string body = p.ReadUnicodeBE();

            UIManager.GetGump<ProfileGump>(serial)?.Dispose();

            UIManager.Add(
                new ProfileGump(world, serial, header, footer, body, serial == world.Player.Serial)
            );
        }
    }
}
