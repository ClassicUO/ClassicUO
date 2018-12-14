using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

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

    }
}
