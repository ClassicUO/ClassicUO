using System.Collections.Generic;
using ClassicUO.AssetsLoader;
using ClassicUO.Game.GameObjects;

namespace ClassicUO.Game.Map
{
    public static class TileSorter
    {
        public static void Sort(in List<GameObject> objects)
        {
            for (int i = 0; i < objects.Count - 1; i++)
            {
                int j = i + 1;
                while (j > 0)
                {
                    int result = Compare(objects[j - 1], objects[j]);
                    if (result > 0)
                    {
                        GameObject temp = objects[j - 1];
                        objects[j - 1] = objects[j];
                        objects[j] = temp;
                    }

                    j--;
                }
            }
        }

        private static int Compare(in GameObject x, in GameObject y)
        {
            (int xZ, int xType, int xThreshold, int xTierbreaker) = GetSortValues(x);
            (int yZ, int yType, int yThreshold, int yTierbreaker) = GetSortValues(y);

            xZ += xThreshold;
            yZ += yThreshold;

            int comparison = xZ - yZ;
            if (comparison == 0)
                comparison = xType - yType;
            if (comparison == 0)
                comparison = xThreshold - yThreshold;
            if (comparison == 0)
                comparison = xTierbreaker - yTierbreaker;

            return comparison;
        }

        private static (int, int, int, int) GetSortValues(in GameObject e)
        {
            switch (e)
            {
                case Tile tile:
                    return (tile.View.SortZ, 0, 0, 0);
                case Static staticitem:
                    return (staticitem.Position.Z, 1, (staticitem.ItemData.Height > 0 ? 1 : 0) + (TileData.IsBackground((long) staticitem.ItemData.Flags) ? 0 : 1), staticitem.Index);
                case Item item:
                    return (item.Position.Z, item.IsCorpse ? 4 : 2, (item.ItemData.Height > 0 ? 1 : 0) + (TileData.IsBackground((long) item.ItemData.Flags) ? 0 : 1), (int) item.Serial.Value);
                case Mobile mobile:
                    return (mobile.Position.Z, 3 /* is sitting */, 2, mobile == World.Player ? 0x40000000 : (int) mobile.Serial.Value);
                case DeferredEntity def:
                    return (def.Position.Z, 2, 1, 0);
                case GameEffect effect:
                    return (effect.Position.Z, 4, 2, 0);
                default:
                    return (0, 0, 0, 0);
            }
        }
    }
}