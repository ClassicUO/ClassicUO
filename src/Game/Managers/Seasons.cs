#region license
// Copyright (C) 2020 ClassicUO Development Community on Github
// 
// This project is an alternative client for the game Ultima Online.
// The goal of this is to develop a lightweight client considering
// new technologies.
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassicUO.Game.Managers
{
    enum Seasons
    {
        Spring,
        Summer,
        Fall,
        Winter,
        Desolation
    }

    static class SeasonManager
    {
        private readonly static ushort[] _winterGraphic = new ushort[Constants.MAX_LAND_DATA_INDEX_COUNT];

        static SeasonManager()
        {
            _winterGraphic[196] = 0x011A;
            _winterGraphic[197] = 0x011B;
            _winterGraphic[198] = 0x011C;
            _winterGraphic[199] = 0x011D;
            _winterGraphic[206] = 0x05C0;
            _winterGraphic[248] = 0x011A;
            _winterGraphic[249] = 0x011B;
            _winterGraphic[250] = 0x011C;
            _winterGraphic[251] = 0x011D;
            _winterGraphic[804] = 0x0391;
            _winterGraphic[805] = 0x0392;
            _winterGraphic[806] = 0x0393;
            _winterGraphic[807] = 0x0394;
            _winterGraphic[808] = 0x0395;
            _winterGraphic[809] = 0x0396;
            _winterGraphic[1521] = 0x011A;
            _winterGraphic[1522] = 0x011B;
            _winterGraphic[1523] = 0x011C;
            _winterGraphic[1524] = 0x011D;
            _winterGraphic[1529] = 0x011A;
            _winterGraphic[1530] = 0x011B;
            _winterGraphic[1531] = 0x011C;
            _winterGraphic[1532] = 0x011D;
            _winterGraphic[1533] = 0x011B;
            _winterGraphic[1534] = 0x011C;
            _winterGraphic[1535] = 0x011D;
            _winterGraphic[1536] = 0x011B;
            _winterGraphic[1537] = 0x011C;
            _winterGraphic[1538] = 0x011D;
            _winterGraphic[1539] = 0x011C;
            _winterGraphic[1540] = 0x011D;
        }

        public static ushort GetSeasonGraphic(Seasons season, ushort graphic)
        {
            switch (season)
            {
                case Seasons.Spring: return GetSpringGraphic(graphic);
                case Seasons.Fall: return GetFallGraphic(graphic);
                case Seasons.Desolation: return GetDesolationGraphic(graphic);
            }

            return graphic;
        }

        public static ushort GetLandSeasonGraphic(Seasons season, ushort graphic)
        {
            if (season != Seasons.Winter)
                return graphic;

            ushort buf = _winterGraphic[graphic];

            if (buf != 0)
            {
                graphic = buf;
            }

            return graphic;
        }

        private static ushort GetSpringGraphic(ushort graphic)
        {
            switch (graphic)
            {
                case 0x0CA7:
                    graphic = 0x0C84;
                    break;
                case 0x0CAC:
                    graphic = 0x0C46;
                    break;
                case 0x0CAD:
                    graphic = 0x0C48;
                    break;
                case 0x0CAE:
                case 0x0CB5:
                    graphic = 0x0C4A;
                    break;
                case 0x0CAF:
                    graphic = 0x0C4E;
                    break;
                case 0x0CB0:
                    graphic = 0x0C4D;
                    break;
                case 0x0CB6:
                case 0x0D0D:
                case 0x0D14:
                    graphic = 0x0D2B;
                    break;
                case 0x0D0C:
                    graphic = 0x0D29;
                    break;
                case 0x0D0E:
                    graphic = 0x0CBE;
                    break;
                case 0x0D0F:
                    graphic = 0x0CBF;
                    break;
                case 0x0D10:
                    graphic = 0x0CC0;
                    break;
                case 0x0D11:
                    graphic = 0x0C87;
                    break;
                case 0x0D12:
                    graphic = 0x0C38;
                    break;
                case 0x0D13:
                    graphic = 0x0D2F;
                    break;
                default:
                    break;
            }

            return graphic;
        }

        private static ushort GetSummerGraphic(ushort graphic)
        {
            return graphic;
        }

        private static ushort GetFallGraphic(ushort graphic)
        {
            switch (graphic)
            {
                case 0x0CD1:
                    graphic = 0x0CD2;
                    break;
                case 0x0CD4:
                    graphic = 0x0CD5;
                    break;
                case 0x0CDB:
                    graphic = 0x0CDC;
                    break;
                case 0x0CDE:
                    graphic = 0x0CDF;
                    break;
                case 0x0CE1:
                    graphic = 0x0CE2;
                    break;
                case 0x0CE4:
                    graphic = 0x0CE5;
                    break;
                case 0x0CE7:
                    graphic = 0x0CE8;
                    break;
                case 0x0D95:
                    graphic = 0x0D97;
                    break;
                case 0x0D99:
                    graphic = 0x0D9B;
                    break;
                case 0x0CCE:
                    graphic = 0x0CCF;
                    break;
                case 0x0CE9:
                case 0x0C9E:
                    graphic = 0x0D3F;
                    break;
                case 0x0CEA:
                    graphic = 0x0D40;
                    break;
                case 0x0C84:
                case 0x0CB0:
                    graphic = 0x1B22;
                    break;
                case 0x0C8B:
                case 0x0C8C:
                case 0x0C8D:
                case 0x0C8E:
                    graphic = 0x0CC6;
                    break;
                case 0x0CA7:
                    graphic = 0x0C48;
                    break;
                case 0x0CAC:
                    graphic = 0x1B1F;
                    break;
                case 0x0CAD:
                    graphic = 0x1B20;
                    break;
                case 0x0CAE:
                    graphic = 0x1B21;
                    break;
                case 0x0CAF:
                    graphic = 0x0D0D;
                    break;
                case 0x0CB5:
                    graphic = 0x0D10;
                    break;
                case 0x0CB6:
                    graphic = 0x0D2B;
                    break;
                case 0x0CC7:
                    graphic = 0x0C4E;
                    break;
                default:
                    break;
            }

            return graphic;
        }

        private static ushort GetWinterGraphic(ushort graphic)
            => graphic;

        private static ushort GetDesolationGraphic(ushort graphic)
        {
            switch (graphic)
            {
                case 0x1B7E:
                    graphic = 0x1E34;
                    break;
                case 0x0D2B:
                    graphic = 0x1B15;
                    break;
                case 0x0D11:
                case 0x0D14:
                case 0x0D17:
                    graphic = 0x122B;
                    break;
                case 0x0D16:
                case 0x0CB9:
                case 0x0CBA:
                case 0x0CBB:
                case 0x0CBC:
                case 0x0CBD:
                case 0x0CBE:
                    graphic = 0x1B8D;
                    break;
                case 0x0CC7:
                    graphic = 0x1B0D;
                    break;
                case 0x0CE9:
                    graphic = 0x0ED7;
                    break;
                case 0x0CEA:
                    graphic = 0x0D3F;
                    break;
                case 0x0D0F:
                    graphic = 0x1B1C;
                    break;
                case 0x0CB8:
                    graphic = 0x1CEA;
                    break;
                case 0x0C84:
                case 0x0C8B:
                    graphic = 0x1B84;
                    break;
                case 0x0C9E:
                    graphic = 0x1182;
                    break;
                case 0x0CAD:
                    graphic = 0x1AE1;
                    break;
                case 0x0C4C:
                    graphic = 0x1B16;
                    break;
                case 0x0C8E:
                case 0x0C99:
                case 0x0CAC:
                    graphic = 0x1B8D;
                    break;
                case 0x0C46:
                case 0x0C49:
                case 0x0CB6:
                    graphic = 0x1B9D;
                    break;
                case 0x0C45:
                case 0x0C48:
                case 0x0C4E:
                case 0x0C85:
                case 0x0CA7:
                case 0x0CAE:
                case 0x0CAF:
                case 0x0CB5:
                case 0x0D15:
                case 0x0D29:
                    graphic = 0x1B9C;
                    break;
                case 0x0C37:
                case 0x0C38:
                case 0x0C47:
                case 0x0C4A:
                case 0x0C4B:
                case 0x0C4D:
                case 0x0C8C:
                case 0x0C8D:
                case 0x0C93:
                case 0x0C94:
                case 0x0C98:
                case 0x0C9F:
                case 0x0CA0:
                case 0x0CA1:
                case 0x0CA2:
                case 0x0CA3:
                case 0x0CA4:
                case 0x0CB0:
                case 0x0CB1:
                case 0x0CB2:
                case 0x0CB3:
                case 0x0CB4:
                case 0x0CB7:
                case 0x0CC5:
                case 0x0D0C:
                case 0x0D0D:
                case 0x0D0E:
                case 0x0D10:
                case 0x0D12:
                case 0x0D13:
                case 0x0D18:
                case 0x0D19:
                case 0x0D2D:
                case 0x0D2F:
                    graphic = 0x1BAE;
                    break;
                default:
                    break;
            }

            return graphic;
        }
    }
}
