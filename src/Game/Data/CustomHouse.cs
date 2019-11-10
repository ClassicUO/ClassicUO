using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassicUO.Game.Data
{
    abstract class CustomHouseObject
    {
        public int Category;
        public int FeatureMask;

        public virtual bool Parse(string text)
        {
            return false;
        }
    }

    abstract class CustomHouseObjectCategory<T> where T : CustomHouseObject
    {
        public int Index;
        public List<T> Items = new List<T>();
    }

    class CustomHouseWall : CustomHouseObject
    {
        private const int GRAPHICS_COUNT = 8;


        public int Style,
                   TID,
                   South1,
                   South2,
                   South3,
                   Corner,
                   East1,
                   East2,
                   East3,
                   Post,
                   WindowS,
                   AltWindowS,
                   WindowE,
                   AltWindowE,
                   SecondAltWindowS,
                   SecondAltWindowE;
        public ushort[] Graphics = new ushort[GRAPHICS_COUNT];
        public ushort[] WindowGraphics = new ushort[GRAPHICS_COUNT];

        public int Contains(ushort graphic)
        {
            for (int i = 0; i < GRAPHICS_COUNT; i++)
            {
                if (Graphics[i] == graphic || WindowGraphics[i] == graphic)
                    return i;
            }

            return -1;
        }

        public override bool Parse(string text)
        {
            string[] scanf = text.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);

            bool result = false;

            if (scanf.Length >= 17)
            {
                result = true;

                Category = int.Parse(scanf[0]);
                Style = int.Parse(scanf[1]);
                TID = int.Parse(scanf[2]);
                South1 = int.Parse(scanf[3]);
                South2 = int.Parse(scanf[4]);
                South3 = int.Parse(scanf[5]);
                Corner = int.Parse(scanf[6]);
                East1 = int.Parse(scanf[7]);
                East2 = int.Parse(scanf[8]);
                East3 = int.Parse(scanf[9]);
                Post = int.Parse(scanf[10]);
                WindowS = int.Parse(scanf[11]);
                AltWindowS = int.Parse(scanf[12]);
                WindowE = int.Parse(scanf[13]);
                AltWindowE = int.Parse(scanf[14]);
                SecondAltWindowS = int.Parse(scanf[15]);
                SecondAltWindowE = int.Parse(scanf[16]);
                FeatureMask = int.Parse(scanf[17]);
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

            if ((AltWindowE == 0) && (WindowE != 0))
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
   
    class CustomHouseWallCategory : CustomHouseObjectCategory<CustomHouseWall>
    {

    }

    class CustomHouseFloor : CustomHouseObject
    {
        private const int GRAPHICS_COUNT = 16;

        public int F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12, F13, F14, F15, F16;
        public ushort[] Graphics = new ushort[GRAPHICS_COUNT];

        public override bool Parse(string text)
        {
            string[] scanf = text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            bool result = false;

            if (scanf.Length >= 17)
            {
                result = true;
                Category = int.Parse(scanf[0]);
                F1 = int.Parse(scanf[1]);
                F2 = int.Parse(scanf[2]);
                F3 = int.Parse(scanf[3]);
                F4 = int.Parse(scanf[4]);
                F5 = int.Parse(scanf[5]);
                F6 = int.Parse(scanf[6]);
                F7 = int.Parse(scanf[7]);
                F8 = int.Parse(scanf[8]);
                F9 = int.Parse(scanf[9]);
                F10 = int.Parse(scanf[10]);
                F11 = int.Parse(scanf[11]);
                F12 = int.Parse(scanf[12]);
                F13 = int.Parse(scanf[13]);
                F14 = int.Parse(scanf[14]);
                F15 = int.Parse(scanf[15]);
                F16 = int.Parse(scanf[16]);
                FeatureMask = int.Parse(scanf[17]);
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

        public int Contains(ushort graphic)
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

    class CustomHouseRoof : CustomHouseObject
    {
        private const int GRAPHICS_COUNT = 16;

        public int Style,
                   TID,
                   North,
                   East,
                   South,
                   West,
                   NSCrosspiece,
                   EWCrosspiece,
                   NDent,
                   SDent,
                   WDent,
                   NTPiece,
                   ETPiece,
                   STPiece,
                   WTPiece,
                   XPiece,
                   Extra,
                   Piece;
        public ushort[] Graphics = new ushort[GRAPHICS_COUNT];

        public override bool Parse(string text)
        {
            string[] scanf = text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            bool result = false;

            if (scanf.Length >= 19)
            {
                result = true;

                Category = int.Parse(scanf[0]);
                Style = int.Parse(scanf[1]);
                TID = int.Parse(scanf[2]);
                North = int.Parse(scanf[3]);
                East = int.Parse(scanf[4]);
                South = int.Parse(scanf[5]);
                West = int.Parse(scanf[6]);
                NSCrosspiece = int.Parse(scanf[7]);
                EWCrosspiece = int.Parse(scanf[8]);
                NDent = int.Parse(scanf[9]);
                SDent = int.Parse(scanf[10]);
                WDent = int.Parse(scanf[11]);
                NTPiece = int.Parse(scanf[12]);
                ETPiece = int.Parse(scanf[13]);
                STPiece = int.Parse(scanf[14]);
                WTPiece = int.Parse(scanf[15]);
                XPiece = int.Parse(scanf[16]);
                Extra = int.Parse(scanf[17]);
                Piece = int.Parse(scanf[18]);
                FeatureMask = int.Parse(scanf[19]);
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

        public int Contains(ushort graphic)
        {
            for (int i = 0; i < GRAPHICS_COUNT; i++)
            {
                if (Graphics[i] == graphic)
                    return i;
            }

            return -1;
        }
    }
   
    class CustomHouseRoofCategory : CustomHouseObjectCategory<CustomHouseRoof>
    {

    }

    class CustomHouseMisc : CustomHouseObject
    {
        private const int GRAPHICS_COUNT = 8;

        public int Style, TID, Piece1, Piece2, Piece3, Piece4, Piece5, Piece6, Piece7, Piece8;  
        public ushort[] Graphics = new ushort[GRAPHICS_COUNT];

        public override bool Parse(string text)
        {
            string[] scanf = text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            bool result = false;

            if (scanf.Length >= 11)
            {
                result = true;

                Category = int.Parse(scanf[0]);
                Style = int.Parse(scanf[1]);
                TID = int.Parse(scanf[2]);
                Piece1 = int.Parse(scanf[3]);
                Piece2 = int.Parse(scanf[4]);
                Piece3 = int.Parse(scanf[5]);
                Piece4 = int.Parse(scanf[6]);
                Piece5 = int.Parse(scanf[7]);
                Piece6 = int.Parse(scanf[8]);
                Piece7 = int.Parse(scanf[9]);
                Piece8 = int.Parse(scanf[10]);
                FeatureMask = int.Parse(scanf[11]);
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

        public int Contains(ushort graphic)
        {
            for (int i = 0; i < GRAPHICS_COUNT; i++)
            {
                if (Graphics[i] == graphic)
                    return i;
            }

            return -1;
        }
    }

    class CustomHouseMiscCategory : CustomHouseObjectCategory<CustomHouseMisc>
    {

    }

    class CustomHouseDoor : CustomHouseObject
    {
        private const int GRAPHICS_COUNT = 8;

        public int Piece1,
                   Piece2,
                   Piece3,
                   Piece4,
                   Piece5,
                   Piece6,
                   Piece7,
                   Piece8;
        public ushort[] Graphics = new ushort[GRAPHICS_COUNT];

        public override bool Parse(string text)
        {
            string[] scanf = text.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);

            bool result = false;

            if (scanf.Length >= 9)
            {
                result = true;

                Category = int.Parse(scanf[0]);
                Piece1 = int.Parse(scanf[1]);
                Piece2 = int.Parse(scanf[2]);
                Piece3 = int.Parse(scanf[3]);
                Piece4 = int.Parse(scanf[4]);
                Piece5 = int.Parse(scanf[5]);
                Piece6 = int.Parse(scanf[6]);
                Piece7 = int.Parse(scanf[7]);
                Piece8 = int.Parse(scanf[8]);
                FeatureMask = int.Parse(scanf[9]);
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

        public int Contains(ushort graphic)
        {
            for (int i = 0; i < GRAPHICS_COUNT; i++)
            {
                if (Graphics[i] == graphic)
                    return i;
            }

            return -1;
        }
    }

    class CustomHouseTeleport : CustomHouseObject
    {
        private const int GRAPHICS_COUNT = 16;

        public int F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12, F13, F14, F15, F16;
        public ushort[] Graphics = new ushort[GRAPHICS_COUNT];

        public override bool Parse(string text)
        {
            string[] scanf = text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            bool result = false;

            if (scanf.Length >= 17)
            {
                result = true;
                Category = int.Parse(scanf[0]);
                F1 = int.Parse(scanf[1]);
                F2 = int.Parse(scanf[2]);
                F3 = int.Parse(scanf[3]);
                F4 = int.Parse(scanf[4]);
                F5 = int.Parse(scanf[5]);
                F6 = int.Parse(scanf[6]);
                F7 = int.Parse(scanf[7]);
                F8 = int.Parse(scanf[8]);
                F9 = int.Parse(scanf[9]);
                F10 = int.Parse(scanf[10]);
                F11 = int.Parse(scanf[11]);
                F12 = int.Parse(scanf[12]);
                F13 = int.Parse(scanf[13]);
                F14 = int.Parse(scanf[14]);
                F15 = int.Parse(scanf[15]);
                F16 = int.Parse(scanf[16]);
                FeatureMask = int.Parse(scanf[17]);
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

        public int Contains(ushort graphic)
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

    class CustomHouseStair : CustomHouseObject
    {
        private const int GRAPHICS_COUNT = 9;

        public int Block,
                   North,
                   East,
                   South,
                   West,
                   Squared1,
                   Squared2,
                   Rounded1,
                   Rounded2,
                   MultiNorth,
                   MultiEast,
                   MultiSouth,
                   MultiWest;
        public ushort[] Graphics = new ushort[GRAPHICS_COUNT];

        public override bool Parse(string text)
        {
            string[] scanf = text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            bool result = false;

            if (scanf.Length >= 14)
            {
                result = true;

                Category = int.Parse(scanf[0]);
                Block = int.Parse(scanf[1]);
                North = int.Parse(scanf[2]);
                East = int.Parse(scanf[3]);
                South = int.Parse(scanf[4]);
                West = int.Parse(scanf[5]);
                Squared1 = int.Parse(scanf[6]);
                Squared2 = int.Parse(scanf[7]);
                Rounded1 = int.Parse(scanf[8]);
                Rounded2 = int.Parse(scanf[9]);
                MultiNorth = int.Parse(scanf[10]);
                MultiEast = int.Parse(scanf[11]);
                MultiSouth = int.Parse(scanf[12]);
                MultiWest = int.Parse(scanf[13]);
                FeatureMask = int.Parse(scanf[14]);
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

        public int Contains(ushort graphic)
        {
            for (int i = 0; i < GRAPHICS_COUNT; i++)
            {
                if (Graphics[i] == graphic)
                    return i;
            }

            return -1;
        }
    }

    class CustomHousePlaceInfo : CustomHouseObject
    {
        private const int GRAPHICS_COUNT = 1;
        public int Graphic,
                   Top,
                   Bottom,
                   AdjUN,
                   AdjLN,
                   AdjUE,
                   AdjLE,
                   AdjUS,
                   AdjLS,
                   AdjUW,
                   AdjLW,
                   DirectSupports,
                   CanGoW,
                   CanGoN,
                   CanGoNWS;

        public ushort[] Graphics = new ushort[GRAPHICS_COUNT];

        public override bool Parse(string text)
        {
            string[] scanf = text.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);

            bool result = false;

            if (scanf.Length >= 16)
            {
                result = true;

                // skip scanf[0]
                Graphic = int.Parse(scanf[1]);
                Top = int.Parse(scanf[2]);
                Bottom = int.Parse(scanf[3]);
                AdjUN = int.Parse(scanf[4]);
                AdjLN = int.Parse(scanf[5]);
                AdjUE = int.Parse(scanf[6]);
                AdjLE = int.Parse(scanf[7]);
                AdjUS = int.Parse(scanf[8]);
                AdjLS = int.Parse(scanf[9]);
                AdjUW = int.Parse(scanf[10]);
                AdjLW = int.Parse(scanf[11]);
                DirectSupports = int.Parse(scanf[12]);
                CanGoW = int.Parse(scanf[13]);
                CanGoN = int.Parse(scanf[14]);
                CanGoNWS = int.Parse(scanf[15]);
            }

            if (result)
            {
                Graphics[0] = (ushort) Graphic;
            }

            return result;
        }

        public int Contains(ushort graphic)
        {
            for (int i = 0; i < GRAPHICS_COUNT; i++)
            {
                if (Graphics[i] == graphic)
                    return i;
            }

            return -1;
        }
    }
}
