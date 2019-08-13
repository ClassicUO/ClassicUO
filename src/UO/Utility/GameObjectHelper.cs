using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

using ClassicUO.Game;
using ClassicUO.Game.GameObjects;
using ClassicUO.IO;
using ClassicUO.IO.Resources;

using Multi = ClassicUO.Game.GameObjects.Multi;

namespace ClassicUO.Utility
{
    internal static class GameObjectHelper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetStaticData(GameObject obj, out StaticTiles itemdata)
        {           
            switch (obj)
            {
                case Static st:

                    if (st.OriginalGraphic != st.Graphic)
                        itemdata = FileManager.TileData.StaticData[st.OriginalGraphic];
                    else
                        itemdata = st.ItemData;
                    return true;
                case Item item:
                    itemdata = item.ItemData;
                    return true;
                case Multi multi:
                    itemdata = multi.ItemData;
                    return true;
                default:
                    itemdata = default;
                    return false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsStaticItem(GameObject obj) => obj is Static || obj is Item;

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

                ref StaticTiles data = ref FileManager.TileData.StaticData[g];

                if (!data.IsNoDiagonal || data.IsAnimated && World.Player != null && World.Player.Race == RaceType.GARGOYLE) return false;
            }

            return true;
        }
    }
}
