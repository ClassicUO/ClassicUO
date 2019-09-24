#region license

//  Copyright (C) 2019 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
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

using System.Runtime.CompilerServices;

namespace ClassicUO.Game.Data
{
    internal static class StaticFilters
    {
        [MethodImpl(256)]
        public static bool IsTree(ushort g)
        {
            switch (g)
            {
                // giant holiday tree 0x9DBB
                case 0x0CCA:
                case 0x0CCB:
                case 0x0CCC:
                case 0x0CCD:
                case 0x0CD0:
                case 0x0CD3:
                case 0x0CD6:
                case 0x0CD8:
                case 0x0CDA:
                case 0x0CDD:
                case 0x0CE0:
                case 0x0CE3:
                case 0x0CE6:
                case 0x0D41:
                case 0x0D42:
                case 0x0D43:
                case 0x0D44:
                case 0x0D57:
                case 0x0D58:
                case 0x0D59:
                case 0x0D5A:
                case 0x0D5B:
                case 0x0D6E:
                case 0x0D6F:
                case 0x0D70:
                case 0x0D71:
                case 0x0D72:
                case 0x0D84:
                case 0x0D85:
                case 0x0D86:
                case 0x0D94:
                case 0x0D98:
                case 0x0D9C:
                case 0x0DA0:
                case 0x0DA4:
                case 0x0DA8:
                case 0x0C9E:
                case 0x0CA8:
                case 0x0CAA:
                case 0x0CAB:
                case 0x0CC9:
                case 0x0CF8:
                case 0x0CFB:
                case 0x0CFE:
                case 0x0D01:
                case 0x12B6:
                case 0x12B7:
                case 0x12B8:
                case 0x12B9:
                case 0x12BA:
                case 0x12BB:
                case 0x12BC:
                case 0x12BD:

                case 0x3131:
                case 0x3134:
                case 0x3137:
                case 0x313A:
                case 0x0C95:
                case 0x0C96:
                case 0x0C99:
                case 0x0A06:

                    return true;
            }

            return false;
        }

        [MethodImpl(256)]
        public static bool IsVegetation(ushort g)
        {
            switch (g)
            {
                case 0x0D45:
                case 0x0D46:
                case 0x0D47:
                case 0x0D48:
                case 0x0D49:
                case 0x0D4A:
                case 0x0D4B:
                case 0x0D4C:
                case 0x0D4D:
                case 0x0D4E:
                case 0x0D4F:
                case 0x0D50:
                case 0x0D51:
                case 0x0D52:
                case 0x0D53:
                case 0x0D54:
                case 0x0D5C:
                case 0x0D5D:
                case 0x0D5E:
                case 0x0D5F:
                case 0x0D60:
                case 0x0D61:
                case 0x0D62:
                case 0x0D63:
                case 0x0D64:
                case 0x0D65:
                case 0x0D66:
                case 0x0D67:
                case 0x0D68:
                case 0x0D69:
                case 0x0D6D:
                case 0x0D73:
                case 0x0D74:
                case 0x0D75:
                case 0x0D76:
                case 0x0D77:
                case 0x0D78:
                case 0x0D79:
                case 0x0D7A:
                case 0x0D7B:
                case 0x0D7C:
                case 0x0D7D:
                case 0x0D7E:
                case 0x0D7F:
                case 0x0D80:
                case 0x0D83:
                case 0x0D87:
                case 0x0D88:
                case 0x0D89:
                case 0x0D8A:
                case 0x0D8B:
                case 0x0D8C:
                case 0x0D8D:
                case 0x0D8E:
                case 0x0D8F:
                case 0x0D90:
                case 0x0D91:
                case 0x0D93:
                case 0x12B6:
                case 0x12B7:
                case 0x12BC:
                case 0x12BD:
                case 0x12BE:
                case 0x12BF:
                case 0x12C0:
                case 0x12C1:
                case 0x12C2:
                case 0x12C3:
                case 0x12C4:
                case 0x12C5:
                case 0x12C6:
                case 0x12C7:
                case 0x0CB9:
                case 0x0CBC:
                case 0x0CBD:
                case 0x0CBE:
                case 0x0CBF:
                case 0x0CC0:
                case 0x0CC1:
                case 0x0CC3:
                case 0x0CC5:
                case 0x0CC6:
                case 0x0CC7:
                case 0x0CF3:
                case 0x0CF4:
                case 0x0CF5:
                case 0x0CF6:
                case 0x0CF7:
                case 0x0D04:
                case 0x0D05:
                case 0x0D06:
                case 0x0D07:
                case 0x0D08:
                case 0x0D09:
                case 0x0D0A:
                case 0x0D0B:
                case 0x0D0C:
                case 0x0D0D:
                case 0x0D0E:
                case 0x0D0F:
                case 0x0D10:
                case 0x0D11:
                case 0x0D12:
                case 0x0D13:
                case 0x0D14:
                case 0x0D15:
                case 0x0D16:
                case 0x0D17:
                case 0x0D18:
                case 0x0D19:
                case 0x0D28:
                case 0x0D29:
                case 0x0D2A:
                case 0x0D2B:
                case 0x0D2D:
                case 0x0D34:
                case 0x0D36:
                case 0x0DAE:
                case 0x0DAF:
                case 0x0DBA:
                case 0x0DBB:
                case 0x0DBC:
                case 0x0DBD:
                case 0x0DBE:
                case 0x0DC1:
                case 0x0DC2:
                case 0x0DC3:
                case 0x0C83:
                case 0x0C84:
                case 0x0C85:
                case 0x0C86:
                case 0x0C87:
                case 0x0C88:
                case 0x0C89:
                case 0x0C8A:
                case 0x0C8B:
                case 0x0C8C:
                case 0x0C8D:
                case 0x0C8E:
                case 0x0C93:
                case 0x0C94:
                case 0x0C98:
                case 0x0C9F:
                case 0x0CA0:
                case 0x0CA1:
                case 0x0CA2:
                case 0x0CA3:
                case 0x0CA4:
                case 0x0CA7:
                case 0x0CAC:
                case 0x0CAD:
                case 0x0CAE:
                case 0x0CAF:
                case 0x0CB0:
                case 0x0CB1:
                case 0x0CB2:
                case 0x0CB3:
                case 0x0CB4:
                case 0x0CB5:
                case 0x0CB6:
                case 0x0C45:
                case 0x0C46:
                case 0x0C49:
                case 0x0C47:
                case 0x0C48:
                case 0x0C4A:
                case 0x0C4B:
                case 0x0C4C:
                case 0x0C4D:
                case 0x0C4E:
                case 0x0C37:
                case 0x0C38:
                case 0x0CBA:
                case 0x0D2F:
                case 0x0D32:
                case 0x0D33:
                case 0x0D3F:
                case 0x0D40:
                case 0x0CE9:
                case 0x0CEA:
                    return true;
            }

            return false;
        }

        [MethodImpl(256)]
        public static bool IsCave(ushort g)
        {
            return g >= 0x053B && g <= 0x0554 && g != 0x0550;
        }

        [MethodImpl(256)]
        public static bool IsRock(ushort g)
        {
            switch (g)
            {
                case 4945:
                case 4948:
                case 4950:
                case 4953:
                case 4955:
                case 4958:
                case 4959:
                case 4960:
                case 4962:
                    return true;
                default:
                    return g >= 6001 && g <= 6012;
            }
        }

        [MethodImpl(256)]
        public static bool IsField(ushort g)
        {
            return g >= 0x398C && g <= 0x399F ||
                   g >= 0x3967 && g <= 0x397A ||
                   g >= 0x3946 && g <= 0x3964 ||
                   g >= 0x3914 && g <= 0x3929;
        }

        [MethodImpl(256)]
        public static bool IsFireField(ushort g)
        {
            return g >= 0x398C && g <= 0x399F;
        }

        [MethodImpl(256)]
        public static bool IsParalyzeField(ushort g)
        {
            return g >= 0x3967 && g <= 0x397A;
        }

        [MethodImpl(256)]
        public static bool IsEnergyField(ushort g)
        {
            return g >= 0x3946 && g <= 0x3964;
        }

        [MethodImpl(256)]
        public static bool IsPoisonField(ushort g)
        {
            return g >= 0x3914 && g <= 0x3929;
        }

        [MethodImpl(256)]
        public static bool IsWallOfStone(ushort g)
        {
            return g == 0x038A;
        }
    }
}