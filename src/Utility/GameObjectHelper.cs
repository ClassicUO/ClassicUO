using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

using ClassicUO.Game;
using ClassicUO.Game.GameObjects;
using ClassicUO.IO.Resources;

namespace ClassicUO.Utility
{
    internal static class GameObjectHelper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetItemData(GameObject obj, out StaticTiles itemdata)
        {
            if (obj is Static st)
            {
                itemdata = st.ItemData;
                return true;
            }

            if (obj is Item item)
            {
                itemdata = item.ItemData;
                return true;
            }

            itemdata = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsDynamicItem(GameObject obj) => obj is Static || obj is Item;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNoDrawable(ushort g)
        {
            switch (g)
            {
                case 0x0001:
                case 0x21BC:
                case 0x9E4C:
                case 0x9E64:
                case 0x9E65:
                case 0x9E7D:

                    return true;
            }

            if (g != 0x63D3)
            {
                if (g >= 0x2198 && g <= 0x21A4) return true;
                ulong flags = TileData.StaticData[g].Flags;

                if (!TileData.IsNoDiagonal(flags) || TileData.IsAnimated(flags) && World.Player != null && World.Player.Race == RaceType.GARGOYLE) return false;
            }

            return true;
        }
    }
}
