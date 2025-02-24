// SPDX-License-Identifier: BSD-2-Clause

namespace ClassicUO.Game.Data
{
    internal static class LayerOrder
    {
        public static Layer[,] UsedLayers { get; } = new Layer[8, Constants.USED_LAYER_COUNT]
        {
            {
                // 0
                Layer.Shirt, Layer.Pants, Layer.Shoes, Layer.Legs, Layer.Torso, Layer.Ring, Layer.Talisman,
                Layer.Bracelet, Layer.Face, Layer.Arms, Layer.Gloves, Layer.Skirt, Layer.Tunic, Layer.Robe,
                Layer.Necklace, Layer.Hair, Layer.Waist, Layer.Beard, Layer.Earrings, Layer.OneHanded, Layer.Helmet,
                Layer.TwoHanded, Layer.Cloak
            },
            {
                // 1
                Layer.Shirt, Layer.Pants, Layer.Shoes, Layer.Legs, Layer.Torso, Layer.Ring, Layer.Talisman,
                Layer.Bracelet, Layer.Face, Layer.Arms, Layer.Gloves, Layer.Skirt, Layer.Tunic, Layer.Robe,
                Layer.Necklace, Layer.Hair, Layer.Waist, Layer.Beard, Layer.Earrings, Layer.OneHanded, Layer.Cloak,
                Layer.Helmet, Layer.TwoHanded
            },
            {
                // 2
                Layer.Shirt, Layer.Pants, Layer.Shoes, Layer.Legs, Layer.Torso, Layer.Ring, Layer.Talisman,
                Layer.Bracelet, Layer.Face, Layer.Arms, Layer.Gloves, Layer.Skirt, Layer.Tunic, Layer.Robe,
                Layer.Necklace, Layer.Hair, Layer.Waist, Layer.Beard, Layer.Earrings, Layer.OneHanded, Layer.Cloak,
                Layer.Helmet, Layer.TwoHanded
            },
            {
                // 3
                Layer.Cloak, Layer.Shirt, Layer.Pants, Layer.Shoes, Layer.Legs, Layer.Torso, Layer.Ring, Layer.Talisman,
                Layer.Bracelet, Layer.Face, Layer.Arms, Layer.Gloves, Layer.Skirt, Layer.Tunic, Layer.Robe, Layer.Waist,
                Layer.Necklace, Layer.Hair, Layer.Beard, Layer.Earrings, Layer.Helmet, Layer.OneHanded, Layer.TwoHanded
            },
            {
                // 4
                Layer.Shirt, Layer.Pants, Layer.Shoes, Layer.Legs, Layer.Torso, Layer.Ring, Layer.Talisman,
                Layer.Bracelet, Layer.Face, Layer.Arms, Layer.Gloves, Layer.Skirt, Layer.Tunic, Layer.Robe,
                Layer.Necklace, Layer.Hair, Layer.Waist, Layer.Beard, Layer.Earrings, Layer.OneHanded, Layer.Cloak,
                Layer.Helmet, Layer.TwoHanded
            },
            {
                // 5
                Layer.Shirt, Layer.Pants, Layer.Shoes, Layer.Legs, Layer.Torso, Layer.Ring, Layer.Talisman,
                Layer.Bracelet, Layer.Face, Layer.Arms, Layer.Gloves, Layer.Skirt, Layer.Tunic, Layer.Robe,
                Layer.Necklace, Layer.Hair, Layer.Waist, Layer.Beard, Layer.Earrings, Layer.OneHanded, Layer.Cloak,
                Layer.Helmet, Layer.TwoHanded
            },
            {
                // 6
                Layer.Shirt, Layer.Pants, Layer.Shoes, Layer.Legs, Layer.Torso, Layer.Ring, Layer.Talisman,
                Layer.Bracelet, Layer.Face, Layer.Arms, Layer.Gloves, Layer.Skirt, Layer.Tunic, Layer.Robe,
                Layer.Necklace, Layer.Hair, Layer.Waist, Layer.Beard, Layer.Earrings, Layer.OneHanded, Layer.Cloak,
                Layer.Helmet, Layer.TwoHanded
            },
            {
                // 7
                Layer.Shirt, Layer.Pants, Layer.Shoes, Layer.Legs, Layer.Torso, Layer.Ring, Layer.Talisman,
                Layer.Bracelet, Layer.Face, Layer.Arms, Layer.Gloves, Layer.Skirt, Layer.Tunic, Layer.Robe,
                Layer.Necklace, Layer.Hair, Layer.Waist, Layer.Beard, Layer.Earrings, Layer.OneHanded, Layer.Cloak,
                Layer.Helmet, Layer.TwoHanded
            }
        };
    }
}