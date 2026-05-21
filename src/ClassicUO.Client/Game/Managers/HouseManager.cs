// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Buffers;
using System.Collections.Generic;
using ClassicUO.Game.Events;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.IO;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Managers
{
    internal sealed class HouseManager
    {
        private readonly Dictionary<uint, House> _houses = new Dictionary<uint, House>();
        private readonly World _world;

        public HouseManager(World world)
        {
            _world = world;
            EventSink.CustomHouseReceived += OnCustomHouseReceived;
        }

        public void Unsubscribe()
        {
            EventSink.CustomHouseReceived -= OnCustomHouseReceived;
        }

        public IReadOnlyCollection<House> Houses => _houses.Values;

        public void Add(uint serial, House revision)
        {
            _houses[serial] = revision;
        }

        public bool TryGetHouse(uint serial, out House house)
        {
            return _houses.TryGetValue(serial, out house);
        }

        public bool TryToRemove(uint serial, int distance)
        {
            if (!IsHouseInRange(serial, distance))
            {
                if (_houses.TryGetValue(serial, out House house))
                {
                    house.ClearComponents();
                    _houses.Remove(serial);
                }


                return true;
            }

            return false;
        }

        public bool IsHouseInRange(uint serial, int distance)
        {
            if (TryGetHouse(serial, out _))
            {
                int currX = _world.RangeSize.X;
                int currY = _world.RangeSize.Y;

                //if (World.Player.IsMoving)
                //{
                //    Mobile.Step step = World.Player.Steps.Back();

                //    currX = step.X;
                //    currY = step.Y;
                //}
                //else
                //{
                //    currX = World.Player.X;
                //    currY = World.Player.Y;
                //}

                Item found = _world.Items.Get(serial);

                if (found == null)
                {
                    return true;
                }

                distance += found.MultiDistanceBonus;

                return Math.Abs(found.X - currX) <= distance && Math.Abs(found.Y - currY) <= distance;
            }

            return false;
        }

        public bool EntityIntoHouse(uint house, GameObject obj)
        {
            if (obj != null && TryGetHouse(house, out _))
            {
                Item found = _world.Items.Get(house);

                if (found == null || !found.MultiInfo.HasValue)
                {
                    return true;
                }

                int minX = found.X + found.MultiInfo.Value.X;
                int maxX = found.X + found.MultiInfo.Value.Width;
                int minY = found.Y + found.MultiInfo.Value.Y;
                int maxY = found.Y + found.MultiInfo.Value.Height;

                return obj.X >= minX && obj.X <= maxX && obj.Y >= minY && obj.Y <= maxY;
            }

            return false;
        }

        public void Remove(uint serial)
        {
            if (TryGetHouse(serial, out House house))
            {
                house.ClearComponents();
                _houses.Remove(serial);
            }
        }

        public void RemoveMultiTargetHouse()
        {
            if (_houses.TryGetValue(0, out House house))
            {
                house.ClearComponents();
                _houses.Remove(0);
            }
        }

        public bool Exists(uint serial)
        {
            return _houses.ContainsKey(serial);
        }

        public void Clear()
        {
            foreach (KeyValuePair<uint, House> house in _houses)
            {
                house.Value.ClearComponents();
            }

            _houses.Clear();
        }

        private void OnCustomHouseReceived(CustomHouseReceivedArgs e)
        {
            uint serial = e.Serial;
            uint revision = e.Revision;
            byte[] data = e.Data;

            if (data == null)
            {
                return;
            }

            Item foundation = _world.Items.Get(serial);

            if (foundation == null)
            {
                return;
            }

            Rectangle? multi = foundation.MultiInfo;

            if (!foundation.IsMulti || multi == null)
            {
                return;
            }

            var reader = new StackDataReader(data.AsSpan(e.Offset));

            // Original packet handler did p.Skip(4) here.
            reader.Skip(4);

            if (!TryGetHouse(foundation, out House house))
            {
                house = new House(_world, foundation, revision, true);
                Add(foundation, house);
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

                reader.Release();
                return;
            }

            byte planes = reader.ReadUInt8();

            house.ClearCustomHouseComponents(0);

            for (int plane = 0; plane < planes; plane++)
            {
                uint header = reader.ReadUInt32BE();
                int dlen = (int)(((header & 0xFF0000) >> 16) | ((header & 0xF0) << 4));
                int clen = (int)(((header & 0xFF00) >> 8) | ((header & 0x0F) << 8));
                int planeZ = (int)((header & 0x0F000000) >> 24);
                int planeMode = (int)((header & 0xF0000000) >> 28);

                if (clen <= 0)
                {
                    continue;
                }

                ReadUnsafeCustomHouseData(
                    reader.Buffer,
                    reader.Position,
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

                reader.Skip(clen);
            }

            reader.Release();

            if (_world.CustomHouseManager != null)
            {
                _world.CustomHouseManager.GenerateFloorPlace();

                UIManager.GetGump<HouseCustomizationGump>(house.Serial)?.Update();
            }

            UIManager.GetGump<MiniMapGump>()?.RequestUpdateContents();

            if (EntityIntoHouse(serial, _world.Player))
            {
                Client.Game.GetScene<GameScene>()?.UpdateMaxDrawZ(true);
            }

            _world.BoatMovingManager.ClearSteps(serial);
        }

        private static unsafe void ReadUnsafeCustomHouseData(
            ReadOnlySpan<byte> source,
            int sourcePosition,
            int dlen,
            int clen,
            int planeZ,
            int planeMode,
            short minX,
            short minY,
            short maxY,
            Item item,
            House house
        )
        {
            bool ismovable = item.ItemData.IsMultiMovable;

            byte[] buffer = null;
            Span<byte> span =
                dlen <= 1024
                    ? stackalloc byte[dlen]
                    : (buffer = ArrayPool<byte>.Shared.Rent(dlen));

            try
            {
                var result = ZLib.Decompress(source.Slice(sourcePosition, clen), span.Slice(0, dlen));
                var reader = new StackDataReader(span.Slice(0, dlen));

                ushort id = 0;
                sbyte x = 0,
                    y = 0,
                    z = 0;

                switch (planeMode)
                {
                    case 0:
                        int c = dlen / 5;

                        for (uint i = 0; i < c; i++)
                        {
                            id = reader.ReadUInt16BE();
                            x = reader.ReadInt8();
                            y = reader.ReadInt8();
                            z = reader.ReadInt8();

                            if (id != 0)
                            {
                                house.Add(
                                    id,
                                    0,
                                    (ushort)(item.X + x),
                                    (ushort)(item.Y + y),
                                    (sbyte)(item.Z + z),
                                    true,
                                    ismovable
                                );
                            }
                        }

                        break;

                    case 1:

                        if (planeZ > 0)
                        {
                            z = (sbyte)((planeZ - 1) % 4 * 20 + 7);
                        }
                        else
                        {
                            z = 0;
                        }

                        c = dlen >> 2;

                        for (uint i = 0; i < c; i++)
                        {
                            id = reader.ReadUInt16BE();
                            x = reader.ReadInt8();
                            y = reader.ReadInt8();

                            if (id != 0)
                            {
                                house.Add(
                                    id,
                                    0,
                                    (ushort)(item.X + x),
                                    (ushort)(item.Y + y),
                                    (sbyte)(item.Z + z),
                                    true,
                                    ismovable
                                );
                            }
                        }

                        break;

                    case 2:
                        short offX = 0,
                            offY = 0;
                        short multiHeight = 0;

                        if (planeZ > 0)
                        {
                            z = (sbyte)((planeZ - 1) % 4 * 20 + 7);
                        }
                        else
                        {
                            z = 0;
                        }

                        if (planeZ <= 0)
                        {
                            offX = minX;
                            offY = minY;
                            multiHeight = (short)(maxY - minY + 2);
                        }
                        else if (planeZ <= 4)
                        {
                            offX = (short)(minX + 1);
                            offY = (short)(minY + 1);
                            multiHeight = (short)(maxY - minY);
                        }
                        else
                        {
                            offX = minX;
                            offY = minY;
                            multiHeight = (short)(maxY - minY + 1);
                        }

                        c = dlen >> 1;

                        for (uint i = 0; i < c; i++)
                        {
                            id = reader.ReadUInt16BE();
                            x = (sbyte)(i / multiHeight + offX);
                            y = (sbyte)(i % multiHeight + offY);

                            if (id != 0)
                            {
                                house.Add(
                                    id,
                                    0,
                                    (ushort)(item.X + x),
                                    (ushort)(item.Y + y),
                                    (sbyte)(item.Z + z),
                                    true,
                                    ismovable
                                );
                            }
                        }

                        break;
                }

                reader.Release();
            }
            finally
            {
                if (buffer != null)
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }
            }
        }
    }
}
