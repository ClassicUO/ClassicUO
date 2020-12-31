namespace ClassicUO.Game.Managers
{
    internal static class SeasonManager
    {
        private static readonly ushort[] _winterGraphic = new ushort[Constants.MAX_LAND_DATA_INDEX_COUNT];

        static SeasonManager()
        {
            // solid forest tiles
            _winterGraphic[196] = 282;
            _winterGraphic[197] = 283;
            _winterGraphic[198] = 284;
            _winterGraphic[199] = 285;

            _winterGraphic[248] = 282;
            _winterGraphic[249] = 283;
            _winterGraphic[250] = 284;
            _winterGraphic[251] = 285;

            _winterGraphic[349] = 937;
            _winterGraphic[350] = 940;
            _winterGraphic[351] = 938;
            _winterGraphic[352] = 939;

            // transition forest/grass tiles
            _winterGraphic[200] = 282;
            _winterGraphic[201] = 283;
            _winterGraphic[202] = 284;
            _winterGraphic[203] = 285;
            _winterGraphic[204] = 282;
            _winterGraphic[205] = 283;
            _winterGraphic[206] = 284;
            _winterGraphic[207] = 285;
            _winterGraphic[208] = 282;
            _winterGraphic[209] = 283;
            _winterGraphic[210] = 284;
            _winterGraphic[211] = 285;
            _winterGraphic[212] = 282;
            _winterGraphic[213] = 283;
            _winterGraphic[214] = 284;
            _winterGraphic[215] = 285;
            _winterGraphic[216] = 282;
            _winterGraphic[217] = 283;
            _winterGraphic[218] = 284;
            _winterGraphic[219] = 285;

            _winterGraphic[1697] = 282;
            _winterGraphic[1698] = 283;
            _winterGraphic[1699] = 284;
            _winterGraphic[1700] = 285;

            _winterGraphic[1711] = 282;
            _winterGraphic[1712] = 283;
            _winterGraphic[1713] = 284;
            _winterGraphic[1714] = 285;
            _winterGraphic[1715] = 282;
            _winterGraphic[1716] = 283;
            _winterGraphic[1717] = 284;
            _winterGraphic[1718] = 285;
            _winterGraphic[1719] = 282;
            _winterGraphic[1720] = 283;
            _winterGraphic[1721] = 284;
            _winterGraphic[1722] = 285;
            _winterGraphic[1723] = 282;
            _winterGraphic[1724] = 283;
            _winterGraphic[1725] = 284;
            _winterGraphic[1726] = 285;
            _winterGraphic[1727] = 282;
            _winterGraphic[1728] = 283;
            _winterGraphic[1729] = 284;
            _winterGraphic[1730] = 285;

            // transition forest/dirt tiles
            _winterGraphic[332] = 932;
            _winterGraphic[333] = 929;
            _winterGraphic[334] = 930;
            _winterGraphic[335] = 931;

            _winterGraphic[353] = 908;
            _winterGraphic[354] = 907;
            _winterGraphic[355] = 905;
            _winterGraphic[356] = 906;
            _winterGraphic[357] = 904;
            _winterGraphic[358] = 903;
            _winterGraphic[359] = 902;
            _winterGraphic[360] = 901;

            _winterGraphic[361] = 912;
            _winterGraphic[362] = 911;
            _winterGraphic[363] = 909;
            _winterGraphic[364] = 910;

            _winterGraphic[369] = 916;
            _winterGraphic[370] = 915;
            _winterGraphic[371] = 914;
            _winterGraphic[372] = 913;

            _winterGraphic[1351] = 917;
            _winterGraphic[1352] = 918;
            _winterGraphic[1353] = 919;
            _winterGraphic[1354] = 920;
            _winterGraphic[1355] = 921;
            _winterGraphic[1356] = 922;
            _winterGraphic[1357] = 923;
            _winterGraphic[1358] = 924;
            _winterGraphic[1359] = 925;

            _winterGraphic[1360] = 927;
            _winterGraphic[1361] = 928;
            _winterGraphic[1362] = 930;

            _winterGraphic[1363] = 933;
            _winterGraphic[1364] = 934;
            _winterGraphic[1365] = 935;
            _winterGraphic[1366] = 936;

            _winterGraphic[804] = 931;
            _winterGraphic[805] = 929;
            _winterGraphic[806] = 926;
            _winterGraphic[807] = 925;
            _winterGraphic[808] = 932;
            _winterGraphic[809] = 930;
            _winterGraphic[810] = 928;
            _winterGraphic[811] = 927;
            _winterGraphic[812] = 919;
            _winterGraphic[813] = 920;
            _winterGraphic[814] = 917;
            _winterGraphic[815] = 921;

            // solid grass tiles
            _winterGraphic[3] = 282;
            _winterGraphic[4] = 283;
            _winterGraphic[5] = 284;
            _winterGraphic[6] = 285;

            // transition grass/dirt tiles
            _winterGraphic[121] = 910;
            _winterGraphic[122] = 909;
            _winterGraphic[123] = 912;
            _winterGraphic[124] = 911;

            _winterGraphic[125] = 906;
            _winterGraphic[126] = 905;
            _winterGraphic[130] = 908;
            _winterGraphic[131] = 907;

            _winterGraphic[133] = 904;
            _winterGraphic[134] = 904;

            _winterGraphic[135] = 903;
            _winterGraphic[136] = 903;

            _winterGraphic[137] = 902;
            _winterGraphic[138] = 902;

            _winterGraphic[139] = 901;
            _winterGraphic[140] = 901;

            _winterGraphic[871] = 917;
            _winterGraphic[872] = 918;
            _winterGraphic[873] = 919;
            _winterGraphic[874] = 920;
            _winterGraphic[875] = 921;
            _winterGraphic[876] = 922;
            _winterGraphic[877] = 923;
            _winterGraphic[878] = 924;
            _winterGraphic[879] = 925;
            _winterGraphic[880] = 926;
            _winterGraphic[881] = 927;
            _winterGraphic[882] = 928;
            _winterGraphic[883] = 929;
            _winterGraphic[884] = 930;
            _winterGraphic[885] = 931;
            _winterGraphic[886] = 932;
            _winterGraphic[887] = 933;
            _winterGraphic[888] = 934;
            _winterGraphic[889] = 935;
            _winterGraphic[890] = 936;
            _winterGraphic[891] = 937;
            _winterGraphic[892] = 938;
            _winterGraphic[893] = 939;
            _winterGraphic[894] = 940;

            _winterGraphic[365] = 916;
            _winterGraphic[366] = 915;
            _winterGraphic[367] = 913;
            _winterGraphic[368] = 914;


            // transition forest/mountain tiles
            _winterGraphic[236] = 278;
            _winterGraphic[237] = 279;
            _winterGraphic[238] = 276;
            _winterGraphic[239] = 277;

            _winterGraphic[240] = 305;
            _winterGraphic[241] = 302;
            _winterGraphic[242] = 303;
            _winterGraphic[243] = 304;

            _winterGraphic[244] = 272;
            _winterGraphic[245] = 273;
            _winterGraphic[246] = 274;
            _winterGraphic[247] = 275;

            // transition grass/mountain tiles
            _winterGraphic[561] = 268;
            _winterGraphic[562] = 269;
            _winterGraphic[563] = 270;
            _winterGraphic[564] = 271;
            _winterGraphic[565] = 272;
            _winterGraphic[566] = 273;
            _winterGraphic[567] = 274;
            _winterGraphic[568] = 275;
            _winterGraphic[569] = 276;
            _winterGraphic[570] = 277;
            _winterGraphic[571] = 278;
            _winterGraphic[572] = 279;

            _winterGraphic[573] = 1861;
            _winterGraphic[574] = 1862;
            _winterGraphic[575] = 1863;
            _winterGraphic[576] = 1864;
            _winterGraphic[577] = 1865;
            _winterGraphic[578] = 1866;
            _winterGraphic[579] = 1867;

            _winterGraphic[1741] = 1868;
            _winterGraphic[1742] = 1869;
            _winterGraphic[1743] = 1870;
            _winterGraphic[1744] = 1871;
            _winterGraphic[1745] = 1872;
            _winterGraphic[1746] = 1873;
            _winterGraphic[1747] = 1874;
            _winterGraphic[1748] = 1875;
            _winterGraphic[1749] = 1876;
            _winterGraphic[1750] = 1877;
            _winterGraphic[1751] = 1878;
            _winterGraphic[1752] = 1879;
            _winterGraphic[1753] = 1880;
            _winterGraphic[1754] = 1881;
            _winterGraphic[1755] = 1882;
            _winterGraphic[1756] = 1883;
            _winterGraphic[1757] = 1884;

            _winterGraphic[1758] = 282;
            _winterGraphic[1759] = 283;
            _winterGraphic[1760] = 284;
            _winterGraphic[1761] = 285;

            // transition grass/coastline tiles
            _winterGraphic[26] = 379;
            _winterGraphic[27] = 378;
            _winterGraphic[28] = 377;
            _winterGraphic[29] = 380;
            _winterGraphic[30] = 381;
            _winterGraphic[31] = 382;
            _winterGraphic[32] = 383;
            _winterGraphic[33] = 384;
            _winterGraphic[34] = 385;
            _winterGraphic[35] = 386;
            _winterGraphic[36] = 387;
            _winterGraphic[37] = 388;
            _winterGraphic[38] = 389;
            _winterGraphic[39] = 390;
            _winterGraphic[40] = 391;
            _winterGraphic[41] = 392;
            _winterGraphic[42] = 393;
            _winterGraphic[43] = 394;

            _winterGraphic[141] = 379;
            _winterGraphic[142] = 386;
            _winterGraphic[143] = 385;
            _winterGraphic[144] = 393;

            _winterGraphic[145] = 378;
            _winterGraphic[146] = 387;
            _winterGraphic[147] = 391;
            _winterGraphic[148] = 392;

            _winterGraphic[149] = 377;
            _winterGraphic[150] = 379;
            _winterGraphic[151] = 383;
            _winterGraphic[152] = 380;
            _winterGraphic[153] = 387;
            _winterGraphic[154] = 388;
            _winterGraphic[155] = 393;
            _winterGraphic[156] = 391;
            _winterGraphic[157] = 387;
            _winterGraphic[158] = 385;
            _winterGraphic[159] = 385;
            _winterGraphic[160] = 389;
            _winterGraphic[161] = 379;
            _winterGraphic[162] = 384;
            _winterGraphic[163] = 380;
            _winterGraphic[164] = 379;
            _winterGraphic[165] = 378;
            _winterGraphic[166] = 378;
            _winterGraphic[167] = 394;

            _winterGraphic[1521] = 282;
            _winterGraphic[1522] = 283;
            _winterGraphic[1523] = 284;
            _winterGraphic[1524] = 285;
            _winterGraphic[1529] = 282;
            _winterGraphic[1530] = 283;
            _winterGraphic[1531] = 284;
            _winterGraphic[1532] = 285;
            _winterGraphic[1533] = 283;
            _winterGraphic[1534] = 284;
            _winterGraphic[1535] = 285;
            _winterGraphic[1536] = 283;
            _winterGraphic[1537] = 284;
            _winterGraphic[1538] = 285;
            _winterGraphic[1539] = 284;
            _winterGraphic[1540] = 285;
        }

        public static ushort GetSeasonGraphic(Season season, ushort graphic)
        {
            switch (season)
            {
                case Season.Spring: return GetSpringGraphic(graphic);
                case Season.Fall: return GetFallGraphic(graphic);
                case Season.Winter: return GetWinterGraphic(graphic);
                case Season.Desolation: return GetDesolationGraphic(graphic);
            }

            return graphic;
        }

        public static ushort GetLandSeasonGraphic(Season season, ushort graphic)
        {
            if (season != Season.Winter)
            {
                return graphic;
            }

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
            }

            return graphic;
        }

        private static ushort GetWinterGraphic(ushort graphic)
        {
            switch (graphic)
            {
                case 0x0CA7:
                    graphic = 0x0CC6;

                    break;

                case 0x0CAC:
                    graphic = 0x0D3D;

                    break;

                case 0x0CAD:
                case 0x0CAE:
                case 0x0CB5:
                    graphic = 0x0D33;

                    break;

                case 0x0CAF:
                case 0x0CB0:
                    graphic = 0x0D32;

                    break;

                case 0x0C8E:
                case 0x0C99:
                    graphic = 0x1B8D;

                    break;

                case 0x0C46:
                case 0x0C49:
                    graphic = 0x1B9D;

                    break;

                case 0x0C45:
                case 0x0C48:
                case 0x0C4E:
                case 0x0C85:
                case 0x0D15:
                case 0x0D29:
                    graphic = 0x1B9C;

                    break;

                case 0x0CB1:
                case 0x0CB2:
                case 0x0CB3:
                case 0x0CB4:
                case 0x0CB7:
                case 0x0CC5:
                case 0x0D0C:

                case 0x0CB6:
                    graphic = 0x1B9E;

                    break;

                case 0x0C37:
                case 0x0C38:
                case 0x0C47:
                case 0x0C4A:
                case 0x0C4B:
                case 0x0C4D:
                case 0x0C8C:
                case 0x0D2F:
                    graphic = 0x1B1F;

                    break;

                case 0x0C8D:
                case 0x0C93:
                case 0x0C94:
                case 0x0C98:
                case 0x0C9F:
                case 0x0CA0:
                case 0x0CA1:
                case 0x0CA2:
                    graphic = 0x1B22;

                    break;

                case 0x0CA3:
                case 0x0CA4:
                case 0x0D0D:
                case 0x0D0E:
                case 0x0D10:
                case 0x0D12:
                case 0x0D13:
                case 0x0D18:
                case 0x0D19:
                case 0x0D2D:
                    graphic = 0x1BAE;

                    break;

                case 0x0CC7:
                    graphic = 0x1B20;

                    break;

                case 0x0C84:
                case 0x0C8B:
                    graphic = 0x1B84;

                    break;

            }

            return graphic;
        }

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
            }

            return graphic;
        }
    }
}