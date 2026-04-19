// SPDX-License-Identifier: BSD-2-Clause

using System;
using ClassicUO.Game;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.IO;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;
using Microsoft.Xna.Framework;

namespace ClassicUO.Network
{
    internal sealed partial class PacketHandlers
    {
        internal static void RegisterHousingHandlers(PacketHandlers h)
        {
            h.Add(0x71, BulletinBoardData);
            h.Add(0x99, MultiPlacement);
            h.Add(0xD8, CustomHouse);
        }

        private static void BulletinBoardData(World world, ref StackDataReader p)
        {
            if (!world.InGame)
            {
                return;
            }

            switch (p.ReadUInt8())
            {
                case 0: // open

                    {
                        uint serial = p.ReadUInt32BE();
                        Item item = world.Items.Get(serial);

                        if (item != null)
                        {
                            BulletinBoardGump bulletinBoard = UIManager.GetGump<BulletinBoardGump>(
                                serial
                            );
                            bulletinBoard?.Dispose();

                            int x = (Client.Game.ClientBounds.Width >> 1) - 245;
                            int y = (Client.Game.ClientBounds.Height >> 1) - 205;

                            bulletinBoard = new BulletinBoardGump(world, item, x, y, p.ReadUTF8(22, true)); //p.ReadASCII(22));
                            UIManager.Add(bulletinBoard);

                            item.Opened = true;
                        }
                    }

                    break;

                case 1: // summary msg

                    {
                        uint boardSerial = p.ReadUInt32BE();
                        BulletinBoardGump bulletinBoard = UIManager.GetGump<BulletinBoardGump>(
                            boardSerial
                        );

                        if (bulletinBoard != null)
                        {
                            uint serial = p.ReadUInt32BE();
                            uint parendID = p.ReadUInt32BE();

                            // poster
                            int len = p.ReadUInt8();
                            string text = (len <= 0 ? string.Empty : p.ReadUTF8(len, true)) + " - ";

                            // subject
                            len = p.ReadUInt8();
                            text += (len <= 0 ? string.Empty : p.ReadUTF8(len, true)) + " - ";

                            // datetime
                            len = p.ReadUInt8();
                            text += (len <= 0 ? string.Empty : p.ReadUTF8(len, true));

                            bulletinBoard.AddBulletinObject(serial, text);
                        }
                    }

                    break;

                case 2: // message

                    {
                        uint boardSerial = p.ReadUInt32BE();
                        BulletinBoardGump bulletinBoard = UIManager.GetGump<BulletinBoardGump>(
                            boardSerial
                        );

                        if (bulletinBoard != null)
                        {
                            uint serial = p.ReadUInt32BE();

                            int len = p.ReadUInt8();
                            string poster = len > 0 ? p.ReadASCII(len) : string.Empty;

                            len = p.ReadUInt8();
                            string subject = len > 0 ? p.ReadUTF8(len, true) : string.Empty;

                            len = p.ReadUInt8();
                            string dataTime = len > 0 ? p.ReadASCII(len) : string.Empty;

                            p.Skip(4);

                            byte unk = p.ReadUInt8();

                            if (unk > 0)
                            {
                                p.Skip(unk * 4);
                            }

                            byte lines = p.ReadUInt8();

                            Span<char> span = stackalloc char[256];
                            ValueStringBuilder sb = new ValueStringBuilder(span);

                            for (int i = 0; i < lines; i++)
                            {
                                byte lineLen = p.ReadUInt8();

                                if (lineLen > 0)
                                {
                                    string putta = p.ReadUTF8(lineLen, true);
                                    sb.Append(putta);
                                    sb.Append('\n');
                                }
                            }

                            string msg = sb.ToString();
                            byte variant = (byte)(1 + (poster == world.Player.Name ? 1 : 0));

                            UIManager.Add(
                                new BulletinBoardItem(
                                    world,
                                    boardSerial,
                                    serial,
                                    poster,
                                    subject,
                                    dataTime,
                                    msg.TrimStart(),
                                    variant
                                )
                                {
                                    X = 40,
                                    Y = 40
                                }
                            );

                            sb.Dispose();
                        }
                    }

                    break;
            }
        }

        private static void MultiPlacement(World world, ref StackDataReader p)
        {
            if (world.Player == null)
            {
                return;
            }

            bool allowGround = p.ReadBool();
            uint targID = p.ReadUInt32BE();
            byte flags = p.ReadUInt8();
            p.Seek(18);
            ushort multiID = p.ReadUInt16BE();
            ushort xOff = p.ReadUInt16BE();
            ushort yOff = p.ReadUInt16BE();
            ushort zOff = p.ReadUInt16BE();
            ushort hue = p.ReadUInt16BE();

            world.TargetManager.SetTargetingMulti(targID, multiID, xOff, yOff, zOff, hue);
        }

        private static void CustomHouse(World world, ref StackDataReader p)
        {
            bool compressed = p.ReadUInt8() == 0x03;
            bool enableReponse = p.ReadBool();
            uint serial = p.ReadUInt32BE();
            Item foundation = world.Items.Get(serial);
            uint revision = p.ReadUInt32BE();

            if (foundation == null)
            {
                return;
            }

            Rectangle? multi = foundation.MultiInfo;

            if (!foundation.IsMulti || multi == null)
            {
                return;
            }

            p.Skip(4);

            if (!world.HouseManager.TryGetHouse(foundation, out House house))
            {
                house = new House(world, foundation, revision, true);
                world.HouseManager.Add(foundation, house);
            }
            else
            {
                house.ClearComponents(true);
                house.Revision = revision;
                house.IsCustom = true;
            }

            short minX = (short)multi.Value.X;
            short minY = (short)multi.Value.Y;
            short maxY = (short)multi.Value.Height;

            if (minX == 0 && minY == 0 && maxY == 0 && multi.Value.Width == 0)
            {
                Log.Warn(
                    "[CustomHouse (0xD8) - Invalid multi dimentions. Maybe missing some installation required files"
                );

                return;
            }

            byte planes = p.ReadUInt8();

            house.ClearCustomHouseComponents(0);

            for (int plane = 0; plane < planes; plane++)
            {
                uint header = p.ReadUInt32BE();
                int dlen = (int)(((header & 0xFF0000) >> 16) | ((header & 0xF0) << 4));
                int clen = (int)(((header & 0xFF00) >> 8) | ((header & 0x0F) << 8));
                int planeZ = (int)((header & 0x0F000000) >> 24);
                int planeMode = (int)((header & 0xF0000000) >> 28);

                if (clen <= 0)
                {
                    continue;
                }

                ReadUnsafeCustomHouseData(
                    p.Buffer,
                    p.Position,
                    dlen,
                    clen,
                    planeZ,
                    planeMode,
                    minX,
                    minY,
                    maxY,
                    foundation,
                    house
                );

                p.Skip(clen);
            }

            if (world.CustomHouseManager != null)
            {
                world.CustomHouseManager.GenerateFloorPlace();

                UIManager.GetGump<HouseCustomizationGump>(house.Serial)?.Update();
            }

            UIManager.GetGump<MiniMapGump>()?.RequestUpdateContents();

            if (world.HouseManager.EntityIntoHouse(serial, world.Player))
            {
                Client.Game.GetScene<GameScene>()?.UpdateMaxDrawZ(true);
            }

            world.BoatMovingManager.ClearSteps(serial);
        }
    }
}
