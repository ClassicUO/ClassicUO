// SPDX-License-Identifier: BSD-2-Clause

using System.Collections.Generic;
using System.Runtime.InteropServices;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;

namespace ClassicUO.Game.Boats
{
    /// <summary>
    /// Concrete <see cref="IBoatEntityTracker"/>. Stores each boat's rider
    /// list as a flat <see cref="List{T}"/> of <see cref="ItemInside"/>
    /// records, walked via <see cref="CollectionsMarshal.AsSpan{T}(List{T})"/>
    /// for in-place mutation without re-hash overhead.
    /// </summary>
    internal sealed class BoatEntityTracker : IBoatEntityTracker
    {
        private readonly Dictionary<uint, List<ItemInside>> _items = new Dictionary<uint, List<ItemInside>>();

        public void PushItemToList(uint serial, uint objSerial, int x, int y, int z)
        {
            if (!_items.TryGetValue(serial, out var list))
            {
                list = new List<ItemInside>();
                _items[serial] = list;
            }

            var span = CollectionsMarshal.AsSpan(list);
            for (int i = 0; i < span.Length; i++)
            {
                ref var item = ref span[i];

                if (!SerialHelper.IsValid(item.Serial))
                {
                    break;
                }

                if (item.Serial == objSerial)
                {
                    item.X = x;
                    item.Y = y;
                    item.Z = z;

                    return;
                }
            }

            list.Add
            (
                new ItemInside
                {
                    Serial = objSerial,
                    X = x,
                    Y = y,
                    Z = z
                }
            );
        }

        public void ClearEntities(uint serial)
        {
            _items.Remove(serial);
        }

        public void ResetEntityOffsets(World world, uint serial)
        {
            if (!_items.TryGetValue(serial, out var list))
            {
                return;
            }

            var span = CollectionsMarshal.AsSpan(list);
            for (int i = 0; i < span.Length; i++)
            {
                ref var it = ref span[i];

                Entity ent = world.Get(it.Serial);

                if (ent == null)
                {
                    continue;
                }

                ent.Offset.X = 0;
                ent.Offset.Y = 0;
                ent.Offset.Z = 0;
            }

            list.Clear();
        }

        public void UpdateEntitiesInside
        (
            World world,
            uint serial,
            bool removeStep,
            int x,
            int y,
            int z,
            Direction direction
        )
        {
            if (!_items.TryGetValue(serial, out var list))
            {
                return;
            }

            Item item = world.Items.Get(serial);

            var span = CollectionsMarshal.AsSpan(list);
            for (int i = 0; i < span.Length; i++)
            {
                ref var it = ref span[i];

                Entity entity = world.Get(it.Serial);

                if (entity == null || entity.IsDestroyed)
                {
                    continue;
                }

                if (removeStep)
                {
                    entity.X = (ushort)(x - it.X);
                    entity.Y = (ushort)(y - it.Y);
                    entity.Z = (sbyte)(z - it.Z);
                    entity.UpdateScreenPosition();

                    entity.Offset.X = 0;
                    entity.Offset.Y = 0;
                    entity.Offset.Z = 0;

                    if (entity.TPrevious != null || entity.TNext != null)
                    {
                        entity.AddToTile();
                    }
                }
                else
                {
                    if (item != null)
                    {
                        entity.Offset = item.Offset;
                    }
                }
            }
        }

        public void Remove(uint serial)
        {
            _items.Remove(serial);
        }

        private struct ItemInside
        {
            public uint Serial;
            public int X, Y, Z;
        }
    }
}
