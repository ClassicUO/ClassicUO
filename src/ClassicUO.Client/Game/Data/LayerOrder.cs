#region license

// BSD 2-Clause License
//
// Copyright (c) 2025, andreakarasho
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
//
// - Redistributions of source code must retain the above copyright notice, this
//   list of conditions and the following disclaimer.
//
// - Redistributions in binary form must reproduce the above copyright notice,
//   this list of conditions and the following disclaimer in the documentation
//   and/or other materials provided with the distribution.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
// AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
// FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
// DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
// SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
// CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
// OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
// OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

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