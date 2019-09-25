using System.Runtime.CompilerServices;

using ClassicUO.Game;
using ClassicUO.Game.GameObjects;
using ClassicUO.IO;
using ClassicUO.IO.Resources;

namespace ClassicUO.Utility
{
    internal static class GameObjectHelper
    {
        [MethodImpl(256)]
        public static bool TryGetStaticData(GameObject obj, out StaticTiles itemdata)
        {
            switch (obj)
            {
                case Static st:
                    itemdata = /*st.OriginalGraphic != st.Graphic ? FileManager.TileData.StaticData[st.OriginalGraphic] :*/ st.ItemData;

                    return true;

                case Item item:
                    itemdata = item.ItemData;

                    return true;

                case Multi multi:
                    itemdata = multi.ItemData;

                    return true;

                case AnimatedItemEffect ef when ef.Source is Static s:
                    itemdata = s.ItemData;

                    return true;

                default:
                    itemdata = default;

                    return false;
            }
        }

        [MethodImpl(256)]
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

                ref readonly StaticTiles data = ref FileManager.TileData.StaticData[g];

                if (!data.IsNoDiagonal || data.IsAnimated && World.Player != null && World.Player.Race == RaceType.GARGOYLE) return false;
            }

            return true;
        }
    }
}