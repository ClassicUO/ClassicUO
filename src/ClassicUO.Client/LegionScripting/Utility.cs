using System.Collections.Generic;
using System.ComponentModel;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using LScript;

namespace ClassicUO.LegionScripting
{
    internal static class Utility
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="gfx">Graphic to match</param>
        /// <param name="parentContainer">Matches *only* the parent container, not root **Don't use different continer params together**</param>
        /// <param name="rootContainer"></param>
        /// <param name="parOrRootContainer"></param>
        /// <param name="hue">Hue to match</param>
        /// <param name="groundRange">Distance from player</param>
        /// <returns></returns>
        public static List<Item> FindItems(
            uint gfx = uint.MaxValue,
            uint parentContainer = uint.MaxValue,
            uint rootContainer = uint.MaxValue,
            uint parOrRootContainer = uint.MaxValue,
            ushort hue = ushort.MaxValue,
            int groundRange = int.MaxValue,
            bool skipIgnoreCheck = false
            )
        {
            List<Item> list = new List<Item>();

            foreach (Item item in World.Items.Values)
            {
                if (gfx != uint.MaxValue && item.Graphic != gfx)
                    continue;

                if (parentContainer != uint.MaxValue && item.Container != parentContainer)
                    continue;

                if (rootContainer != uint.MaxValue && item.RootContainer != rootContainer)
                    continue;

                if (parOrRootContainer != uint.MaxValue && (item.Container != parOrRootContainer && item.RootContainer != parOrRootContainer))
                    continue;

                if (hue != ushort.MaxValue && item.Hue != hue)
                    continue;

                if (groundRange != int.MaxValue && item.Distance > groundRange)
                    continue;

                if (!skipIgnoreCheck && Interpreter.InIgnoreList(item))
                    continue;

                list.Add(item);
            }

            return list;
        }

        public static uint ContentsCount(Item container)
        {
            if (container == null) return 0;

            uint c = 0;
            for (LinkedObject i = container.Items; i != null; i = i.Next)
                c++;

            return c;
        }

        public static bool SearchItemNameAndProps(string search, Item item)
        {
            if (item == null)
                return false;

            if (World.OPL.TryGetNameAndData(item.Serial, out string name, out string data))
            {
                if (name != null && name.ToLower().Contains(search.ToLower()))
                    return true;
                if (data != null)
                    if (data.ToLower().Contains(search.ToLower()))
                        return true;
            }
            else
            {
                if (item.Name != null && item.Name.ToLower().Contains(search.ToLower()))
                    return true;

                if (item.ItemData.Name.ToLower().Contains(search.ToLower()))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Only includes layers that are items, not things like shop layers
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static Layer GetItemLayer(string name)
        {
            Layer finalLayer = Layer.Invalid;

            switch (name)
            {
                case "onehanded":
                    finalLayer = Layer.OneHanded;
                    break;
                case "twohanded":
                    finalLayer = Layer.TwoHanded;
                    break;
                case "shoes":
                    finalLayer = Layer.Shoes;
                    break;
                case "pants":
                    finalLayer = Layer.Pants;
                    break;
                case "shirt":
                    finalLayer = Layer.Shirt;
                    break;
                case "helmet":
                    finalLayer = Layer.Helmet;
                    break;
                case "gloves":
                    finalLayer = Layer.Gloves;
                    break;
                case "ring":
                    finalLayer = Layer.Ring;
                    break;
                case "talisman":
                    finalLayer = Layer.Talisman;
                    break;
                case "necklace":
                    finalLayer = Layer.Necklace;
                    break;
                case "hair":
                    finalLayer = Layer.Hair;
                    break;
                case "waist":
                    finalLayer = Layer.Waist;
                    break;
                case "torso":
                    finalLayer = Layer.Torso;
                    break;
                case "bracelet":
                    finalLayer = Layer.Bracelet;
                    break;
                case "face":
                    finalLayer = Layer.Face;
                    break;
                case "beard":
                    finalLayer = Layer.Beard;
                    break;
                case "tunic":
                    finalLayer = Layer.Tunic;
                    break;
                case "earrings":
                    finalLayer = Layer.Earrings;
                    break;
                case "arms":
                    finalLayer = Layer.Arms;
                    break;
                case "cloak":
                    finalLayer = Layer.Cloak;
                    break;
                case "backpack":
                    finalLayer = Layer.Backpack;
                    break;
                case "robe":
                    finalLayer = Layer.Robe;
                    break;
                case "skirt":
                    finalLayer = Layer.Skirt;
                    break;
                case "legs":
                    finalLayer = Layer.Legs;
                    break;
                case "mount":
                    finalLayer = Layer.Mount;
                    break;
                case "bank":
                    finalLayer = Layer.Bank;
                    break;
                default:
                    break;
            }

            return finalLayer;
        }

        /// <summary>
        /// Get Direction from a string
        /// </summary>
        /// <param name="direction"></param>
        /// <returns></returns>
        public static Direction GetDirection(string direction)
        {
            switch (direction.ToLower())
            {
                case "north": return Direction.North;

                case "northeast":
                case "right": return Direction.Right;

                case "east": return Direction.East;

                case "southeast":
                case "down": return Direction.Down;

                case "south": return Direction.South;

                case "southwest":
                case "left": return Direction.Left;

                case "west": return Direction.West;

                case "northwest":
                case "up": return Direction.Up;
                default: return Direction.NONE;
            }
        }

    }
}
