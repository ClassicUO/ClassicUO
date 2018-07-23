using ClassicUO.Game.WorldObjects;

namespace ClassicUO.Game.Renderer.Views
{
    public static class LayerOrder
    {
        public const int USED_LAYER_COUNT = 25;

        public static Layer[,] UsedLayers { get; } = new Layer[8, USED_LAYER_COUNT]
        {
            {
                Layer.Mount, Layer.Invalid, Layer.Cloak, Layer.Shirt,
                Layer.Pants, Layer.Shoes, Layer.InnerLegs, Layer.InnerTorso,
                Layer.Ring, Layer.Talisman, Layer.Bracelet, Layer.Face,
                Layer.Arms, Layer.Gloves, Layer.OuterLegs, Layer.MiddleTorso,
                Layer.Neck, Layer.Hair, Layer.OuterTorso, Layer.Waist,
                Layer.FacialHair, Layer.Earrings, Layer.LeftHand, Layer.Helm,
                Layer.RightHand
            },
            {
                Layer.Mount, Layer.Invalid, Layer.Shirt, Layer.Pants,
                Layer.Shoes, Layer.InnerLegs, Layer.InnerTorso, Layer.Ring,
                Layer.Talisman, Layer.Bracelet, Layer.Face, Layer.Arms,
                Layer.Gloves, Layer.OuterLegs, Layer.MiddleTorso, Layer.Neck,
                Layer.Hair, Layer.OuterTorso, Layer.Waist, Layer.FacialHair,
                Layer.Earrings, Layer.LeftHand, Layer.Cloak, Layer.Helm,
                Layer.RightHand
            },
            {
                Layer.Mount, Layer.Invalid, Layer.Shirt, Layer.Pants,
                Layer.Shoes, Layer.InnerLegs, Layer.InnerTorso, Layer.Ring,
                Layer.Talisman, Layer.Bracelet, Layer.Face, Layer.Arms,
                Layer.Gloves, Layer.OuterLegs, Layer.MiddleTorso, Layer.Neck,
                Layer.Hair, Layer.OuterTorso, Layer.Waist, Layer.FacialHair,
                Layer.Earrings, Layer.LeftHand, Layer.Cloak, Layer.Helm,
                Layer.RightHand
            },
            {
                Layer.Mount, Layer.Invalid, Layer.Shirt, Layer.Pants,
                Layer.Shoes, Layer.InnerLegs, Layer.InnerTorso, Layer.Ring,
                Layer.Talisman, Layer.Bracelet, Layer.Face, Layer.Arms,
                Layer.Gloves, Layer.OuterLegs, Layer.MiddleTorso, Layer.Neck,
                Layer.Hair, Layer.OuterTorso, Layer.Waist, Layer.FacialHair,
                Layer.Earrings, Layer.LeftHand, Layer.Cloak, Layer.Helm,
                Layer.RightHand
            },
            {
                Layer.Mount, Layer.Invalid, Layer.Shirt, Layer.Pants,
                Layer.Shoes, Layer.InnerLegs, Layer.InnerTorso, Layer.Ring,
                Layer.Talisman, Layer.Bracelet, Layer.Face, Layer.Arms,
                Layer.Gloves, Layer.OuterLegs, Layer.MiddleTorso, Layer.Neck,
                Layer.Hair, Layer.OuterTorso, Layer.Waist, Layer.FacialHair,
                Layer.Earrings, Layer.LeftHand, Layer.Cloak, Layer.Helm,
                Layer.RightHand
            },
            {
                Layer.Mount, Layer.Invalid, Layer.Shirt, Layer.Pants,
                Layer.Shoes, Layer.InnerLegs, Layer.InnerTorso, Layer.Ring,
                Layer.Talisman, Layer.Bracelet, Layer.Face, Layer.Arms,
                Layer.Gloves, Layer.OuterLegs, Layer.MiddleTorso, Layer.Neck,
                Layer.Hair, Layer.OuterTorso, Layer.Waist, Layer.FacialHair,
                Layer.Earrings, Layer.LeftHand, Layer.Cloak, Layer.Helm,
                Layer.RightHand
            },
            {
                Layer.Mount, Layer.Invalid, Layer.Shirt, Layer.Pants,
                Layer.Shoes, Layer.InnerLegs, Layer.InnerTorso, Layer.Ring,
                Layer.Talisman, Layer.Bracelet, Layer.Face, Layer.Arms,
                Layer.Gloves, Layer.OuterLegs, Layer.MiddleTorso, Layer.Neck,
                Layer.Hair, Layer.OuterTorso, Layer.Waist, Layer.FacialHair,
                Layer.Earrings, Layer.LeftHand, Layer.Cloak, Layer.Helm,
                Layer.RightHand
            },

            {
                Layer.Mount, Layer.Invalid, Layer.Shirt, Layer.Pants,
                Layer.Shoes, Layer.InnerLegs, Layer.InnerTorso, Layer.Ring,
                Layer.Talisman, Layer.Bracelet, Layer.Face, Layer.Arms,
                Layer.Gloves, Layer.OuterLegs, Layer.MiddleTorso, Layer.Neck,
                Layer.Hair, Layer.OuterTorso, Layer.Waist, Layer.FacialHair,
                Layer.Earrings, Layer.LeftHand, Layer.Cloak, Layer.Helm,
                Layer.RightHand
            }
        };
    }
}