using System.Runtime.CompilerServices;
using ClassicUO.Data;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.IO.Resources;

namespace ClassicUO.Utility
{
    internal static class GameObjectHelper
    {
        [MethodImpl(256)]
        public static bool IsNoDrawable(ushort g)
        {
            switch (g)
            {
                case 0x0001:
                case 0x21BC:
                    //case 0x5690:
                    return true;

                case 0x9E4C:
                case 0x9E64:
                case 0x9E65:
                case 0x9E7D:
                    ref StaticTiles data = ref TileDataLoader.Instance.StaticData[g];

                    return data.IsBackground || data.IsSurface;
            }

            if (g != 0x63D3)
            {
                if (g >= 0x2198 && g <= 0x21A4)
                {
                    return true;
                }

                // Easel fix.
                // In older clients the tiledata flag for this 
                // item contains NoDiagonal for some reason.
                // So the next check will make the item invisible.
                if (g == 0x0F65 && Client.Version < ClientVersion.CV_60144)
                {
                    return false;
                }

                if (g < TileDataLoader.Instance.StaticData.Length)
                {
                    ref StaticTiles data = ref TileDataLoader.Instance.StaticData[g];

                    if (!data.IsNoDiagonal || data.IsAnimated && World.Player != null &&
                        World.Player.Race == RaceType.GARGOYLE)
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}