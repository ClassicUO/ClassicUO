#region license
//  Copyright (C) 2018 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
//  (Copyright (c) 2018 ClassicUO Development Team)
//    
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.
#endregion
using ClassicUO.Game.GameObjects;

namespace ClassicUO.Game.Views
{
    public static class LayerOrder
    {
        public const int USED_LAYER_COUNT = 25;

        public static Layer[,] UsedLayers { get; } = new Layer[8, USED_LAYER_COUNT] 
        { 
            { Layer.Mount, Layer.Invalid, Layer.Cloak, Layer.Shirt, Layer.Pants, Layer.Shoes, Layer.InnerLegs, Layer.InnerTorso, Layer.Ring, Layer.Talisman, Layer.Bracelet, Layer.Face, Layer.Arms, Layer.Gloves, Layer.OuterLegs, Layer.MiddleTorso, Layer.Neck, Layer.Hair, Layer.OuterTorso, Layer.Waist, Layer.FacialHair, Layer.Earrings, Layer.OneHanded, Layer.Helm, Layer.TwoHanded },
            { Layer.Mount, Layer.Invalid, Layer.Shirt, Layer.Pants, Layer.Shoes, Layer.InnerLegs, Layer.InnerTorso, Layer.Ring, Layer.Talisman, Layer.Bracelet, Layer.Face, Layer.Arms, Layer.Gloves, Layer.OuterLegs, Layer.MiddleTorso, Layer.Neck, Layer.Hair, Layer.OuterTorso, Layer.Waist, Layer.FacialHair, Layer.Earrings, Layer.OneHanded, Layer.Cloak, Layer.Helm, Layer.TwoHanded },
            { Layer.Mount, Layer.Invalid, Layer.Shirt, Layer.Pants, Layer.Shoes, Layer.InnerLegs, Layer.InnerTorso, Layer.Ring, Layer.Talisman, Layer.Bracelet, Layer.Face, Layer.Arms, Layer.Gloves, Layer.OuterLegs, Layer.MiddleTorso, Layer.Neck, Layer.Hair, Layer.OuterTorso, Layer.Waist, Layer.FacialHair, Layer.Earrings, Layer.OneHanded, Layer.Cloak, Layer.Helm, Layer.TwoHanded }, 
            { Layer.Mount, Layer.Invalid, Layer.Shirt, Layer.Pants, Layer.Shoes, Layer.InnerLegs, Layer.InnerTorso, Layer.Ring, Layer.Talisman, Layer.Bracelet, Layer.Face, Layer.Arms, Layer.Gloves, Layer.OuterLegs, Layer.MiddleTorso, Layer.Neck, Layer.Hair, Layer.OuterTorso, Layer.Waist, Layer.FacialHair, Layer.Earrings, Layer.OneHanded, Layer.Cloak, Layer.Helm, Layer.TwoHanded }, 
            { Layer.Mount, Layer.Invalid, Layer.Shirt, Layer.Pants, Layer.Shoes, Layer.InnerLegs, Layer.InnerTorso, Layer.Ring, Layer.Talisman, Layer.Bracelet, Layer.Face, Layer.Arms, Layer.Gloves, Layer.OuterLegs, Layer.MiddleTorso, Layer.Neck, Layer.Hair, Layer.OuterTorso, Layer.Waist, Layer.FacialHair, Layer.Earrings, Layer.OneHanded, Layer.Cloak, Layer.Helm, Layer.TwoHanded },
            { Layer.Mount, Layer.Invalid, Layer.Shirt, Layer.Pants, Layer.Shoes, Layer.InnerLegs, Layer.InnerTorso, Layer.Ring, Layer.Talisman, Layer.Bracelet, Layer.Face, Layer.Arms, Layer.Gloves, Layer.OuterLegs, Layer.MiddleTorso, Layer.Neck, Layer.Hair, Layer.OuterTorso, Layer.Waist, Layer.FacialHair, Layer.Earrings, Layer.OneHanded, Layer.Cloak, Layer.Helm, Layer.TwoHanded }, 
            { Layer.Mount, Layer.Invalid, Layer.Shirt, Layer.Pants, Layer.Shoes, Layer.InnerLegs, Layer.InnerTorso, Layer.Ring, Layer.Talisman, Layer.Bracelet, Layer.Face, Layer.Arms, Layer.Gloves, Layer.OuterLegs, Layer.MiddleTorso, Layer.Neck, Layer.Hair, Layer.OuterTorso, Layer.Waist, Layer.FacialHair, Layer.Earrings, Layer.OneHanded, Layer.Cloak, Layer.Helm, Layer.TwoHanded },
            { Layer.Mount, Layer.Invalid, Layer.Shirt, Layer.Pants, Layer.Shoes, Layer.InnerLegs, Layer.InnerTorso, Layer.Ring, Layer.Talisman, Layer.Bracelet, Layer.Face, Layer.Arms, Layer.Gloves, Layer.OuterLegs, Layer.MiddleTorso, Layer.Neck, Layer.Hair, Layer.OuterTorso, Layer.Waist, Layer.FacialHair, Layer.Earrings, Layer.OneHanded, Layer.Cloak, Layer.Helm, Layer.TwoHanded }
        };
    }
}