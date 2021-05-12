#region license

// Copyright (c) 2021, andreakarasho
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. All advertising materials mentioning features or use of this software
//    must display the following acknowledgement:
//    This product includes software developed by andreakarasho - https://github.com/andreakarasho
// 4. Neither the name of the copyright holder nor the
//    names of its contributors may be used to endorse or promote products
//    derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS ''AS IS'' AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

using System.Runtime.CompilerServices;
using ClassicUO.Data;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.IO.Resources;

namespace ClassicUO.Utility
{
    internal static class GameObjectHelper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

                if (g < TileDataLoader.Instance?.StaticData?.Length)
                {
                    ref StaticTiles data = ref TileDataLoader.Instance.StaticData[g];

                    if (!data.IsNoDiagonal || data.IsAnimated && World.Player != null && World.Player.Race == RaceType.GARGOYLE)
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}