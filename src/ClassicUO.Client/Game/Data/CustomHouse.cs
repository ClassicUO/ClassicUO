#region license

// Copyright (c) 2024, andreakarasho
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

using System;
using System.Collections.Generic;

namespace ClassicUO.Game.Data
{
    public abstract class CustomHouseObject
    {
        public int Category;
        public int FeatureMask;

        public virtual bool Parse(string text)
        {
            return false;
        }

        public abstract int Contains(ushort graphic);
    }

    public abstract class CustomHouseObjectCategory<T> where T : CustomHouseObject
    {
        public int Index;
        public List<T> Items = new List<T>();
    }

    public class CustomHouseWall : CustomHouseObject
    {
        public const int GRAPHICS_COUNT = 8;
        public ushort[] Graphics = new ushort[GRAPHICS_COUNT];


        public int Style, TID, South1, South2, South3, Corner, East1, East2, East3, Post, WindowS, AltWindowS, WindowE, AltWindowE, SecondAltWindowS, SecondAltWindowE;
        public ushort[] WindowGraphics = new ushort[GRAPHICS_COUNT];

        public override int Contains(ushort graphic)
        {
            for (int i = 0; i < GRAPHICS_COUNT; i++)
            {
                if (Graphics[i] == graphic || WindowGraphics[i] == graphic)
                {
                    return i;
                }
            }

            return -1;
        }

        public override bool Parse(string text)
        {
            string[] scanf = text.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            bool result = false;

            if (scanf.Length >= 17)
            {
                result = int.TryParse(scanf[0], out Category) && int.TryParse(scanf[1], out Style) && int.TryParse(scanf[2], out TID) && int.TryParse(scanf[3], out South1) && int.TryParse(scanf[4], out South2) && int.TryParse(scanf[5], out South3) && int.TryParse(scanf[6], out Corner) && int.TryParse(scanf[7], out East1) && int.TryParse(scanf[8], out East2) && int.TryParse(scanf[9], out East3) && int.TryParse(scanf[10], out Post) && int.TryParse(scanf[11], out WindowS) && int.TryParse(scanf[12], out AltWindowS) && int.TryParse(scanf[13], out WindowE) && int.TryParse(scanf[14], out AltWindowE) && int.TryParse(scanf[15], out SecondAltWindowS) && int.TryParse(scanf[16], out SecondAltWindowE) && int.TryParse(scanf[17], out FeatureMask);
            }

            if (result)
            {
                WindowGraphics[0] = Graphics[0] = (ushort) South1;
                WindowGraphics[1] = Graphics[1] = (ushort) South2;
                WindowGraphics[2] = Graphics[2] = (ushort) South3;
                WindowGraphics[3] = Graphics[3] = (ushort) Corner;
                WindowGraphics[4] = Graphics[4] = (ushort) East1;
                WindowGraphics[5] = Graphics[5] = (ushort) East2;
                WindowGraphics[6] = Graphics[6] = (ushort) East3;
                WindowGraphics[7] = Graphics[7] = (ushort) Post;
            }

            if (AltWindowE == 0 && WindowE != 0)
            {
                AltWindowE = WindowE;
                WindowE = 0;
            }

            if (WindowS != 0)
            {
                WindowGraphics[0] = (ushort) WindowS;
            }

            if (AltWindowS != 0)
            {
                WindowGraphics[1] = (ushort) AltWindowS;
            }

            if (SecondAltWindowS != 0)
            {
                WindowGraphics[2] = (ushort) SecondAltWindowS;
            }

            if (WindowE != 0)
            {
                WindowGraphics[4] = (ushort) WindowE;
            }

            if (AltWindowE != 0)
            {
                WindowGraphics[5] = (ushort) AltWindowE;
            }

            if (SecondAltWindowE != 0)
            {
                WindowGraphics[6] = (ushort) SecondAltWindowE;
            }

            return result;
        }
    }

    public class CustomHouseWallCategory : CustomHouseObjectCategory<CustomHouseWall>
    {
    }

    public class CustomHouseFloor : CustomHouseObject
    {
        public const int GRAPHICS_COUNT = 16;

        public int F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12, F13, F14, F15, F16;
        public ushort[] Graphics = new ushort[GRAPHICS_COUNT];

        public override bool Parse(string text)
        {
            string[] scanf = text.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            bool result = false;

            if (scanf.Length >= 17)
            {
                result = int.TryParse(scanf[0], out Category) && int.TryParse(scanf[1], out F1) && int.TryParse(scanf[2], out F2) && int.TryParse(scanf[3], out F3) && int.TryParse(scanf[4], out F4) && int.TryParse(scanf[5], out F5) && int.TryParse(scanf[6], out F6) && int.TryParse(scanf[7], out F7) && int.TryParse(scanf[8], out F8) && int.TryParse(scanf[9], out F9) && int.TryParse(scanf[10], out F10) && int.TryParse(scanf[11], out F11) && int.TryParse(scanf[12], out F12) && int.TryParse(scanf[13], out F13) && int.TryParse(scanf[14], out F14) && int.TryParse(scanf[15], out F15) && int.TryParse(scanf[16], out F16) && int.TryParse(scanf[17], out FeatureMask);
            }

            if (result)
            {
                Graphics[0] = (ushort) F1;
                Graphics[1] = (ushort) F2;
                Graphics[2] = (ushort) F3;
                Graphics[3] = (ushort) F4;
                Graphics[4] = (ushort) F5;
                Graphics[5] = (ushort) F6;
                Graphics[6] = (ushort) F7;
                Graphics[7] = (ushort) F8;
                Graphics[8] = (ushort) F9;
                Graphics[9] = (ushort) F10;
                Graphics[10] = (ushort) F11;
                Graphics[11] = (ushort) F12;
                Graphics[12] = (ushort) F13;
                Graphics[13] = (ushort) F14;
                Graphics[14] = (ushort) F15;
                Graphics[15] = (ushort) F16;
            }

            return result;
        }

        public override int Contains(ushort graphic)
        {
            for (int i = 0; i < GRAPHICS_COUNT; i++)
            {
                if (Graphics[i] == graphic)
                {
                    return i;
                }
            }

            return -1;
        }
    }

    public class CustomHouseRoof : CustomHouseObject
    {
        public const int GRAPHICS_COUNT = 16;
        public ushort[] Graphics = new ushort[GRAPHICS_COUNT];

        public int Style, TID, North, East, South, West, NSCrosspiece, EWCrosspiece, NDent, SDent, WDent, NTPiece, ETPiece, STPiece, WTPiece, XPiece, Extra, Piece;

        public override bool Parse(string text)
        {
            string[] scanf = text.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            bool result = false;

            if (scanf.Length >= 19)
            {
                result = int.TryParse(scanf[0], out Category) && int.TryParse(scanf[1], out Style) && int.TryParse(scanf[2], out TID) && int.TryParse(scanf[3], out North) && int.TryParse(scanf[4], out East) && int.TryParse(scanf[5], out South) && int.TryParse(scanf[6], out West) && int.TryParse(scanf[7], out NSCrosspiece) && int.TryParse(scanf[8], out EWCrosspiece) && int.TryParse(scanf[9], out NDent) && int.TryParse(scanf[10], out SDent) && int.TryParse(scanf[11], out WDent) && int.TryParse(scanf[12], out NTPiece) && int.TryParse(scanf[13], out ETPiece) && int.TryParse(scanf[14], out STPiece) && int.TryParse(scanf[15], out WTPiece) && int.TryParse(scanf[16], out XPiece) && int.TryParse(scanf[17], out Extra) && int.TryParse(scanf[18], out Piece) && int.TryParse(scanf[19], out FeatureMask);
            }

            if (result)
            {
                Graphics[0] = (ushort) North;
                Graphics[1] = (ushort) East;
                Graphics[2] = (ushort) South;
                Graphics[3] = (ushort) West;
                Graphics[4] = (ushort) NSCrosspiece;
                Graphics[5] = (ushort) EWCrosspiece;
                Graphics[6] = (ushort) NDent;
                Graphics[7] = (ushort) SDent;
                Graphics[8] = (ushort) WDent;
                Graphics[9] = (ushort) NTPiece;
                Graphics[10] = (ushort) ETPiece;
                Graphics[11] = (ushort) STPiece;
                Graphics[12] = (ushort) WTPiece;
                Graphics[13] = (ushort) XPiece;
                Graphics[14] = (ushort) Extra;
                Graphics[15] = (ushort) Piece;
            }

            return result;
        }

        public override int Contains(ushort graphic)
        {
            for (int i = 0; i < GRAPHICS_COUNT; i++)
            {
                if (Graphics[i] == graphic)
                {
                    return i;
                }
            }

            return -1;
        }
    }

    public class CustomHouseRoofCategory : CustomHouseObjectCategory<CustomHouseRoof>
    {
    }

    public class CustomHouseMisc : CustomHouseObject
    {
        public const int GRAPHICS_COUNT = 8;
        public ushort[] Graphics = new ushort[GRAPHICS_COUNT];

        public int Style, TID, Piece1, Piece2, Piece3, Piece4, Piece5, Piece6, Piece7, Piece8;

        public override bool Parse(string text)
        {
            string[] scanf = text.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            bool result = false;

            if (scanf.Length >= 11)
            {
                result = int.TryParse(scanf[0], out Category) && int.TryParse(scanf[1], out Style) && int.TryParse(scanf[2], out TID) && int.TryParse(scanf[3], out Piece1) && int.TryParse(scanf[4], out Piece2) && int.TryParse(scanf[5], out Piece3) && int.TryParse(scanf[6], out Piece4) && int.TryParse(scanf[7], out Piece5) && int.TryParse(scanf[8], out Piece6) && int.TryParse(scanf[9], out Piece7) && int.TryParse(scanf[10], out Piece8) && int.TryParse(scanf[11], out FeatureMask);
            }

            if (result)
            {
                Graphics[0] = (ushort) Piece1;
                Graphics[1] = (ushort) Piece2;
                Graphics[2] = (ushort) Piece3;
                Graphics[3] = (ushort) Piece4;
                Graphics[4] = (ushort) Piece5;
                Graphics[5] = (ushort) Piece6;
                Graphics[6] = (ushort) Piece7;
                Graphics[7] = (ushort) Piece8;
            }

            return result;
        }

        public override int Contains(ushort graphic)
        {
            for (int i = 0; i < GRAPHICS_COUNT; i++)
            {
                if (Graphics[i] == graphic)
                {
                    return i;
                }
            }

            return -1;
        }
    }

    public class CustomHouseMiscCategory : CustomHouseObjectCategory<CustomHouseMisc>
    {
    }

    public class CustomHouseDoor : CustomHouseObject
    {
        public const int GRAPHICS_COUNT = 8;
        public ushort[] Graphics = new ushort[GRAPHICS_COUNT];

        public int Piece1, Piece2, Piece3, Piece4, Piece5, Piece6, Piece7, Piece8;

        public override bool Parse(string text)
        {
            string[] scanf = text.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            bool result = false;

            if (scanf.Length >= 9)
            {
                result = int.TryParse(scanf[0], out Category) && int.TryParse(scanf[1], out Piece1) && int.TryParse(scanf[2], out Piece2) && int.TryParse(scanf[3], out Piece3) && int.TryParse(scanf[4], out Piece4) && int.TryParse(scanf[5], out Piece5) && int.TryParse(scanf[6], out Piece6) && int.TryParse(scanf[7], out Piece7) && int.TryParse(scanf[8], out Piece8) && int.TryParse(scanf[9], out FeatureMask);
            }

            if (result)
            {
                Graphics[0] = (ushort) Piece1;
                Graphics[1] = (ushort) Piece2;
                Graphics[2] = (ushort) Piece3;
                Graphics[3] = (ushort) Piece4;
                Graphics[4] = (ushort) Piece5;
                Graphics[5] = (ushort) Piece6;
                Graphics[6] = (ushort) Piece7;
                Graphics[7] = (ushort) Piece8;
            }

            return result;
        }

        public override int Contains(ushort graphic)
        {
            for (int i = 0; i < GRAPHICS_COUNT; i++)
            {
                if (Graphics[i] == graphic)
                {
                    return i;
                }
            }

            return -1;
        }
    }

    public class CustomHouseTeleport : CustomHouseObject
    {
        public const int GRAPHICS_COUNT = 16;

        public int F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12, F13, F14, F15, F16;
        public ushort[] Graphics = new ushort[GRAPHICS_COUNT];

        public override bool Parse(string text)
        {
            string[] scanf = text.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            bool result = false;

            if (scanf.Length >= 17)
            {
                result = int.TryParse(scanf[0], out Category) && int.TryParse(scanf[1], out F1) && int.TryParse(scanf[2], out F2) && int.TryParse(scanf[3], out F3) && int.TryParse(scanf[4], out F4) && int.TryParse(scanf[5], out F5) && int.TryParse(scanf[6], out F6) && int.TryParse(scanf[7], out F7) && int.TryParse(scanf[8], out F8) && int.TryParse(scanf[9], out F9) && int.TryParse(scanf[10], out F10) && int.TryParse(scanf[11], out F11) && int.TryParse(scanf[12], out F12) && int.TryParse(scanf[13], out F13) && int.TryParse(scanf[14], out F14) && int.TryParse(scanf[15], out F15) && int.TryParse(scanf[16], out F16) && int.TryParse(scanf[17], out FeatureMask);
            }

            if (result)
            {
                Graphics[0] = (ushort) F1;
                Graphics[1] = (ushort) F2;
                Graphics[2] = (ushort) F3;
                Graphics[3] = (ushort) F4;
                Graphics[4] = (ushort) F5;
                Graphics[5] = (ushort) F6;
                Graphics[6] = (ushort) F7;
                Graphics[7] = (ushort) F8;
                Graphics[8] = (ushort) F9;
                Graphics[9] = (ushort) F10;
                Graphics[10] = (ushort) F11;
                Graphics[11] = (ushort) F12;
                Graphics[12] = (ushort) F13;
                Graphics[13] = (ushort) F14;
                Graphics[14] = (ushort) F15;
                Graphics[15] = (ushort) F16;
            }

            return result;
        }

        public override int Contains(ushort graphic)
        {
            for (int i = 0; i < GRAPHICS_COUNT; i++)
            {
                if (Graphics[i] == graphic)
                {
                    return i;
                }
            }

            return -1;
        }
    }

    public class CustomHouseStair : CustomHouseObject
    {
        public const int GRAPHICS_COUNT = 9;

        public int Block, North, East, South, West, Squared1, Squared2, Rounded1, Rounded2, MultiNorth, MultiEast, MultiSouth, MultiWest;
        public ushort[] Graphics = new ushort[GRAPHICS_COUNT];

        public override bool Parse(string text)
        {
            string[] scanf = text.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            bool result = false;

            if (scanf.Length >= 14)
            {
                result = int.TryParse(scanf[0], out Category) && int.TryParse(scanf[1], out Block) && int.TryParse(scanf[2], out North) && int.TryParse(scanf[3], out East) && int.TryParse(scanf[4], out South) && int.TryParse(scanf[5], out West) && int.TryParse(scanf[6], out Squared1) && int.TryParse(scanf[7], out Squared2) && int.TryParse(scanf[8], out Rounded1) && int.TryParse(scanf[9], out Rounded2) && int.TryParse(scanf[10], out MultiNorth) && int.TryParse(scanf[11], out MultiEast) && int.TryParse(scanf[12], out MultiSouth) && int.TryParse(scanf[13], out MultiWest) && int.TryParse(scanf[14], out FeatureMask);
            }

            if (result)
            {
                Graphics[0] = (ushort) (MultiNorth != 0 ? Squared1 : 0);
                Graphics[1] = (ushort) (MultiEast != 0 ? Squared2 : 0);
                Graphics[2] = (ushort) (MultiSouth != 0 ? Rounded1 : 0);
                Graphics[3] = (ushort) (MultiWest != 0 ? Rounded2 : 0);
                Graphics[4] = (ushort) Block;
                Graphics[5] = (ushort) North;
                Graphics[6] = (ushort) East;
                Graphics[7] = (ushort) South;
                Graphics[8] = (ushort) West;
            }

            return result;
        }

        public override int Contains(ushort graphic)
        {
            for (int i = 0; i < GRAPHICS_COUNT; i++)
            {
                if (Graphics[i] == graphic)
                {
                    return i;
                }
            }

            return -1;
        }
    }

    public class CustomHousePlaceInfo : CustomHouseObject
    {
        public const int GRAPHICS_COUNT = 1;
        public int Graphic, Top, Bottom, AdjUN, AdjLN, AdjUE, AdjLE, AdjUS, AdjLS, AdjUW, AdjLW, DirectSupports, CanGoW, CanGoN, CanGoNWS;

        public ushort[] Graphics = new ushort[GRAPHICS_COUNT];

        public override bool Parse(string text)
        {
            string[] scanf = text.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            bool result = false;

            if (scanf.Length >= 16)
            {
                result = int.TryParse(scanf[1], out Graphic) && int.TryParse(scanf[2], out Top) && int.TryParse(scanf[3], out Bottom) && int.TryParse(scanf[4], out AdjUN) && int.TryParse(scanf[5], out AdjLN) && int.TryParse(scanf[6], out AdjUE) && int.TryParse(scanf[7], out AdjLE) && int.TryParse(scanf[8], out AdjUS) && int.TryParse(scanf[9], out AdjLS) && int.TryParse(scanf[10], out AdjUW) && int.TryParse(scanf[11], out AdjLW) && int.TryParse(scanf[12], out DirectSupports) && int.TryParse(scanf[13], out CanGoW) && int.TryParse(scanf[14], out CanGoN) && int.TryParse(scanf[15], out CanGoNWS);
            }

            if (result)
            {
                Graphics[0] = (ushort) Graphic;
            }

            return result;
        }

        public override int Contains(ushort graphic)
        {
            for (int i = 0; i < GRAPHICS_COUNT; i++)
            {
                if (Graphics[i] == graphic)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}