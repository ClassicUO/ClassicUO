// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Assets;
using System;
using System.IO;

namespace ClassicUO.Game.Managers
{
    internal static class SeasonManager
    {
        private static ushort[] _springLandTile;
        private static ushort[] _summerLandTile;
        private static ushort[] _fallLandTile;
        private static ushort[] _winterLandTile;
        private static ushort[] _desolationLandTile;

        private static ushort[] _springGraphic;
        private static ushort[] _summerGraphic;
        private static ushort[] _fallGraphic;
        private static ushort[] _winterGraphic;
        private static ushort[] _desolationGraphic;

        private static readonly string _seasonsFilePath = Path.Combine(CUOEnviroment.ExecutablePath, "Data", "Client");
        private static readonly string _seasonsFile = Path.Combine(_seasonsFilePath, "seasons.txt");

        static SeasonManager()
        {
            LoadSeasonFile();
        }

        public static void LoadSeasonFile()
        {
            _springLandTile = new ushort[ArtLoader.MAX_LAND_DATA_INDEX_COUNT];
            _summerLandTile = new ushort[ArtLoader.MAX_LAND_DATA_INDEX_COUNT];
            _fallLandTile = new ushort[ArtLoader.MAX_LAND_DATA_INDEX_COUNT];
            _winterLandTile = new ushort[ArtLoader.MAX_LAND_DATA_INDEX_COUNT];
            _desolationLandTile = new ushort[ArtLoader.MAX_LAND_DATA_INDEX_COUNT];

            _springGraphic = new ushort[ArtLoader.MAX_STATIC_DATA_INDEX_COUNT];
            _summerGraphic = new ushort[ArtLoader.MAX_STATIC_DATA_INDEX_COUNT];
            _fallGraphic = new ushort[ArtLoader.MAX_STATIC_DATA_INDEX_COUNT];
            _winterGraphic = new ushort[ArtLoader.MAX_STATIC_DATA_INDEX_COUNT];
            _desolationGraphic = new ushort[ArtLoader.MAX_STATIC_DATA_INDEX_COUNT];

            if (!File.Exists(_seasonsFile))
            {
                CreateDefaultSeasonsFile();
            }

            using (StreamReader reader = new StreamReader(_seasonsFile))
            {
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();

                    if (string.IsNullOrEmpty(line) || line.StartsWith("#") || line.StartsWith("//"))
                    {
                        continue;
                    }

                    string[] seasonLine = line.Split(',');

                    if (seasonLine.Length < 4)
                    {
                        continue;
                    }

                    ushort orig = seasonLine[2].StartsWith("0x", StringComparison.InvariantCultureIgnoreCase) ?
                        Convert.ToUInt16(seasonLine[2], 16) :
                        Convert.ToUInt16(seasonLine[2]);

                    ushort replace = seasonLine[3].StartsWith("0x", StringComparison.InvariantCultureIgnoreCase) ?
                        Convert.ToUInt16(seasonLine[3], 16) :
                        Convert.ToUInt16(seasonLine[3]);

                    bool isStatic = seasonLine[1].StartsWith("static", StringComparison.InvariantCultureIgnoreCase);

                    switch (seasonLine[0].ToLower())
                    {
                        case "spring":

                            if (isStatic)
                            {
                                _springGraphic[orig] = replace;
                            }
                            else
                            {
                                _springLandTile[orig] = replace;
                            }

                            break;

                        case "summer":

                            if (isStatic)
                            {
                                _summerGraphic[orig] = replace;
                            }
                            else
                            {
                                _summerLandTile[orig] = replace;
                            }

                            break;

                        case "fall":

                            if (isStatic)
                            {
                                _fallGraphic[orig] = replace;
                            }
                            else
                            {
                                _fallLandTile[orig] = replace;
                            }

                            break;

                        case "winter":

                            if (isStatic)
                            {
                                _winterGraphic[orig] = replace;
                            }
                            else
                            {
                                _winterLandTile[orig] = replace;
                            }

                            break;

                        case "desolation":
                            if (isStatic)
                            {
                                _desolationGraphic[orig] = replace;
                            }
                            else
                            {
                                _desolationLandTile[orig] = replace;
                            }

                            break;
                    }
                }
            }
        }

        public static ushort GetSeasonGraphic(Season season, ushort graphic)
        {
            switch (season)
            {
                case Season.Spring: return _springGraphic[graphic] == 0 ? graphic : _springGraphic[graphic];
                case Season.Summer: return _summerGraphic[graphic] == 0 ? graphic : _summerGraphic[graphic];
                case Season.Fall: return _fallGraphic[graphic] == 0 ? graphic : _fallGraphic[graphic];
                case Season.Winter: return _winterGraphic[graphic] == 0 ? graphic : _winterGraphic[graphic];
                case Season.Desolation: return _desolationGraphic[graphic] == 0 ? graphic : _desolationGraphic[graphic];
            }

            return graphic;
        }

        public static ushort GetLandSeasonGraphic(Season season, ushort graphic)
        {
            switch (season)
            {
                case Season.Spring: return _springLandTile[graphic] == 0 ? graphic : _springLandTile[graphic];
                case Season.Summer: return _summerLandTile[graphic] == 0 ? graphic : _summerLandTile[graphic];
                case Season.Fall: return _fallLandTile[graphic] == 0 ? graphic : _fallLandTile[graphic];
                case Season.Winter: return _winterLandTile[graphic] == 0 ? graphic : _winterLandTile[graphic];
                case Season.Desolation: return _desolationLandTile[graphic] == 0 ? graphic : _desolationLandTile[graphic];
            }

            return graphic;
        }

        #region CreateDefaultFile

        private static void CreateDefaultSeasonsFile()
        {
            if (File.Exists(_seasonsFile))
            {
                return;
            }

            using (StreamWriter writer = new StreamWriter(_seasonsFile))
            {
                writer.WriteLine("spring,static,0x0CA7,0x0C84");
                writer.WriteLine("spring,static,0x0CAC,0x0C46");
                writer.WriteLine("spring,static,0x0CAD,0x0C48");
                writer.WriteLine("spring,static,0x0CAE,0x0CB5");
                writer.WriteLine("spring,static,0x0C4A,0x0CB5");
                writer.WriteLine("spring,static,0x0CAF,0x0C4E");
                writer.WriteLine("spring,static,0x0CB0,0x0C4D");
                writer.WriteLine("spring,static,0x0CB6,0x0D2B");
                writer.WriteLine("spring,static,0x0D0D,0x0D2B");
                writer.WriteLine("spring,static,0x0D14,0x0D2B");
                writer.WriteLine("spring,static,0x0D0C,0x0D29");
                writer.WriteLine("spring,static,0x0D0E,0x0CBE");
                writer.WriteLine("spring,static,0x0D0F,0x0CBF");
                writer.WriteLine("spring,static,0x0D10,0x0CC0");
                writer.WriteLine("spring,static,0x0D11,0x0C87");
                writer.WriteLine("spring,static,0x0D12,0x0C38");
                writer.WriteLine("spring,static,0x0D13,0x0D2F");
                writer.WriteLine("fall,static,0x0CD1,0x0CD2");
                writer.WriteLine("fall,static,0x0CD4,0x0CD5");
                writer.WriteLine("fall,static,0x0CDB,0x0CDC");
                writer.WriteLine("fall,static,0x0CDE,0x0CDF");
                writer.WriteLine("fall,static,0x0CE1,0x0CE2");
                writer.WriteLine("fall,static,0x0CE4,0x0CE5");
                writer.WriteLine("fall,static,0x0CE7,0x0CE8");
                writer.WriteLine("fall,static,0x0D95,0x0D97");
                writer.WriteLine("fall,static,0x0D99,0x0D9B");
                writer.WriteLine("fall,static,0x0CCE,0x0CCF");
                writer.WriteLine("fall,static,0x0CE9,0x0D3F");
                writer.WriteLine("fall,static,0x0C9E,0x0D3F");
                writer.WriteLine("fall,static,0x0CEA,0x0D40");
                writer.WriteLine("fall,static,0x0C84,0x1B22");
                writer.WriteLine("fall,static,0x0CB0,0x1B22");
                writer.WriteLine("fall,static,0x0C8B,0x0CC6");
                writer.WriteLine("fall,static,0x0C8C,0x0CC6");
                writer.WriteLine("fall,static,0x0C8D,0x0CC6");
                writer.WriteLine("fall,static,0x0C8E,0x0CC6");
                writer.WriteLine("fall,static,0x0CA7,0x0C48");
                writer.WriteLine("fall,static,0x0CAC,0x1B1F");
                writer.WriteLine("fall,static,0x0CAD,0x1B20");
                writer.WriteLine("fall,static,0x0CAE,0x1B21");
                writer.WriteLine("fall,static,0x0CAF,0x0D0D");
                writer.WriteLine("fall,static,0x0CB5,0x0D10");
                writer.WriteLine("fall,static,0x0CB6,0x0D2B");
                writer.WriteLine("fall,static,0x0CC7,0x0C4E");
                writer.WriteLine("winter,static,0x0CA7,0x0CC6");
                writer.WriteLine("winter,static,0x0CAC,0x0D3D");
                writer.WriteLine("winter,static,0x0CAD,0x0D33");
                writer.WriteLine("winter,static,0x0CAE,0x0D33");
                writer.WriteLine("winter,static,0x0CB5,0x0D33");
                writer.WriteLine("winter,static,0x0CAF,0x17CD");
                writer.WriteLine("winter,static,0x0C87,0x17CD");
                writer.WriteLine("winter,static,0x0C89,0x17CD");
                writer.WriteLine("winter,static,0x0D16,0x17CD");
                writer.WriteLine("winter,static,0x0D17,0x17CD");
                writer.WriteLine("winter,static,0x0D32,0x17CD");
                writer.WriteLine("winter,static,0x0D33,0x17CD");
                writer.WriteLine("winter,static,0x0CB0,0x17CD");
                writer.WriteLine("winter,static,0x0C8E,0x1B8D");
                writer.WriteLine("winter,static,0x0C99,0x1B8D");
                writer.WriteLine("winter,static,0x0C46,0x1B9D");
                writer.WriteLine("winter,static,0x0C49,0x1B9D");
                writer.WriteLine("winter,static,0x0C45,0x1B9C");
                writer.WriteLine("winter,static,0x0C48,0x1B9C");
                writer.WriteLine("winter,static,0x0CBF,0x1B9C");
                writer.WriteLine("winter,static,0x0C4E,0x1B9C");
                writer.WriteLine("winter,static,0x0D2B,0x1B9C");
                writer.WriteLine("winter,static,0x0C85,0x1B9C");
                writer.WriteLine("winter,static,0x0D15,0x1B9C");
                writer.WriteLine("winter,static,0x0D29,0x1B9C");
                writer.WriteLine("winter,static,0x0CB1,0x17CD");
                writer.WriteLine("winter,static,0x0CB2,0x17CD");
                writer.WriteLine("winter,static,0x0CB3,0x17CD");
                writer.WriteLine("winter,static,0x0CB4,0x17CD");
                writer.WriteLine("winter,static,0x0CB7,0x17CD");
                writer.WriteLine("winter,static,0x0CC5,0x17CD");
                writer.WriteLine("winter,static,0x0D0C,0x17CD");
                writer.WriteLine("winter,static,0x0CB6,0x17CD");
                writer.WriteLine("winter,static,0x0C37,0x1B1F");
                writer.WriteLine("winter,static,0x0C38,0x1B1F");
                writer.WriteLine("winter,static,0x0C47,0x1B1F");
                writer.WriteLine("winter,static,0x0C4A,0x1B1F");
                writer.WriteLine("winter,static,0x0C4B,0x1B1F");
                writer.WriteLine("winter,static,0x0C4D,0x1B1F");
                writer.WriteLine("winter,static,0x0C8C,0x1B1F");
                writer.WriteLine("winter,static,0x0D2F,0x1B1F");
                writer.WriteLine("winter,static,0x0C8D,0x1B22");
                writer.WriteLine("winter,static,0x0C93,0x1B22");
                writer.WriteLine("winter,static,0x0C94,0x1B22");
                writer.WriteLine("winter,static,0x0C98,0x1B22");
                writer.WriteLine("winter,static,0x0C9F,0x1B22");
                writer.WriteLine("winter,static,0x0CA0,0x1B22");
                writer.WriteLine("winter,static,0x0CA1,0x1B22");
                writer.WriteLine("winter,static,0x0CA2,0x1B22");
                writer.WriteLine("winter,static,0x0CA3,0x1BAE");
                writer.WriteLine("winter,static,0x0CA4,0x1BAE");
                writer.WriteLine("winter,static,0x0D0D,0x1BAE");
                writer.WriteLine("winter,static,0x0D0E,0x1BAE");
                writer.WriteLine("winter,static,0x0D10,0x1BAE");
                writer.WriteLine("winter,static,0x0D12,0x1BAE");
                writer.WriteLine("winter,static,0x0D13,0x1BAE");
                writer.WriteLine("winter,static,0x0D18,0x1BAE");
                writer.WriteLine("winter,static,0x0D19,0x1BAE");
                writer.WriteLine("winter,static,0x0D2D,0x1BAE");
                writer.WriteLine("winter,static,0x0CC7,0x1B20");
                writer.WriteLine("winter,static,0x0C84,0x1B84");
                writer.WriteLine("winter,static,0x0C8B,0x1B84");
                writer.WriteLine("winter,static,0x0CE9,0x0CCA");
                writer.WriteLine("winter,static,0x0C9E,0x0CCA");
                writer.WriteLine("winter,static,0x33A1,0x17CD");
                writer.WriteLine("winter,static,0x33A2,0x17CD");
                writer.WriteLine("winter,static,0x33A3,0x17CD");
                writer.WriteLine("winter,static,0x33A4,0x17CD");
                writer.WriteLine("winter,static,0x33A6,0x17CD");
                writer.WriteLine("winter,static,0x33AB,0x17CD");
                writer.WriteLine("winter,landtile,196,282");
                writer.WriteLine("winter,landtile,197,283");
                writer.WriteLine("winter,landtile,198,284");
                writer.WriteLine("winter,landtile,199,285");
                writer.WriteLine("winter,landtile,248,282");
                writer.WriteLine("winter,landtile,249,283");
                writer.WriteLine("winter,landtile,250,284");
                writer.WriteLine("winter,landtile,251,285");
                writer.WriteLine("winter,landtile,349,937");
                writer.WriteLine("winter,landtile,350,940");
                writer.WriteLine("winter,landtile,351,938");
                writer.WriteLine("winter,landtile,352,939");
                writer.WriteLine("winter,landtile,200,282");
                writer.WriteLine("winter,landtile,201,283");
                writer.WriteLine("winter,landtile,202,284");
                writer.WriteLine("winter,landtile,203,285");
                writer.WriteLine("winter,landtile,204,282");
                writer.WriteLine("winter,landtile,205,283");
                writer.WriteLine("winter,landtile,206,284");
                writer.WriteLine("winter,landtile,207,285");
                writer.WriteLine("winter,landtile,208,282");
                writer.WriteLine("winter,landtile,209,283");
                writer.WriteLine("winter,landtile,210,284");
                writer.WriteLine("winter,landtile,211,285");
                writer.WriteLine("winter,landtile,212,282");
                writer.WriteLine("winter,landtile,213,283");
                writer.WriteLine("winter,landtile,214,284");
                writer.WriteLine("winter,landtile,215,285");
                writer.WriteLine("winter,landtile,216,282");
                writer.WriteLine("winter,landtile,217,283");
                writer.WriteLine("winter,landtile,218,284");
                writer.WriteLine("winter,landtile,219,285");
                writer.WriteLine("winter,landtile,1697,282");
                writer.WriteLine("winter,landtile,1698,283");
                writer.WriteLine("winter,landtile,1699,284");
                writer.WriteLine("winter,landtile,1700,285");
                writer.WriteLine("winter,landtile,1711,282");
                writer.WriteLine("winter,landtile,1712,283");
                writer.WriteLine("winter,landtile,1713,284");
                writer.WriteLine("winter,landtile,1714,285");
                writer.WriteLine("winter,landtile,1715,282");
                writer.WriteLine("winter,landtile,1716,283");
                writer.WriteLine("winter,landtile,1717,284");
                writer.WriteLine("winter,landtile,1718,285");
                writer.WriteLine("winter,landtile,1719,282");
                writer.WriteLine("winter,landtile,1720,283");
                writer.WriteLine("winter,landtile,1721,284");
                writer.WriteLine("winter,landtile,1722,285");
                writer.WriteLine("winter,landtile,1723,282");
                writer.WriteLine("winter,landtile,1724,283");
                writer.WriteLine("winter,landtile,1725,284");
                writer.WriteLine("winter,landtile,1726,285");
                writer.WriteLine("winter,landtile,1727,282");
                writer.WriteLine("winter,landtile,1728,283");
                writer.WriteLine("winter,landtile,1729,284");
                writer.WriteLine("winter,landtile,1730,285");
                writer.WriteLine("winter,landtile,332,932");
                writer.WriteLine("winter,landtile,333,929");
                writer.WriteLine("winter,landtile,334,930");
                writer.WriteLine("winter,landtile,335,931");
                writer.WriteLine("winter,landtile,353,908");
                writer.WriteLine("winter,landtile,354,907");
                writer.WriteLine("winter,landtile,355,905");
                writer.WriteLine("winter,landtile,356,906");
                writer.WriteLine("winter,landtile,357,904");
                writer.WriteLine("winter,landtile,358,903");
                writer.WriteLine("winter,landtile,359,902");
                writer.WriteLine("winter,landtile,360,901");
                writer.WriteLine("winter,landtile,361,912");
                writer.WriteLine("winter,landtile,362,911");
                writer.WriteLine("winter,landtile,363,909");
                writer.WriteLine("winter,landtile,364,910");
                writer.WriteLine("winter,landtile,369,916");
                writer.WriteLine("winter,landtile,370,915");
                writer.WriteLine("winter,landtile,371,914");
                writer.WriteLine("winter,landtile,372,913");
                writer.WriteLine("winter,landtile,1351,917");
                writer.WriteLine("winter,landtile,1352,918");
                writer.WriteLine("winter,landtile,1353,919");
                writer.WriteLine("winter,landtile,1354,920");
                writer.WriteLine("winter,landtile,1355,921");
                writer.WriteLine("winter,landtile,1356,922");
                writer.WriteLine("winter,landtile,1357,923");
                writer.WriteLine("winter,landtile,1358,924");
                writer.WriteLine("winter,landtile,1359,925");
                writer.WriteLine("winter,landtile,1360,927");
                writer.WriteLine("winter,landtile,1361,928");
                writer.WriteLine("winter,landtile,1362,930");
                writer.WriteLine("winter,landtile,1363,933");
                writer.WriteLine("winter,landtile,1364,934");
                writer.WriteLine("winter,landtile,1365,935");
                writer.WriteLine("winter,landtile,1366,936");
                writer.WriteLine("winter,landtile,804,931");
                writer.WriteLine("winter,landtile,805,929");
                writer.WriteLine("winter,landtile,806,926");
                writer.WriteLine("winter,landtile,807,925");
                writer.WriteLine("winter,landtile,808,932");
                writer.WriteLine("winter,landtile,809,930");
                writer.WriteLine("winter,landtile,810,928");
                writer.WriteLine("winter,landtile,811,927");
                writer.WriteLine("winter,landtile,812,919");
                writer.WriteLine("winter,landtile,813,920");
                writer.WriteLine("winter,landtile,814,917");
                writer.WriteLine("winter,landtile,815,921");
                writer.WriteLine("winter,landtile,3,282");
                writer.WriteLine("winter,landtile,4,283");
                writer.WriteLine("winter,landtile,5,284");
                writer.WriteLine("winter,landtile,6,285");
                writer.WriteLine("winter,landtile,121,910");
                writer.WriteLine("winter,landtile,122,909");
                writer.WriteLine("winter,landtile,123,912");
                writer.WriteLine("winter,landtile,124,911");
                writer.WriteLine("winter,landtile,125,906");
                writer.WriteLine("winter,landtile,126,905");
                writer.WriteLine("winter,landtile,130,908");
                writer.WriteLine("winter,landtile,131,907");
                writer.WriteLine("winter,landtile,133,904");
                writer.WriteLine("winter,landtile,134,904");
                writer.WriteLine("winter,landtile,135,903");
                writer.WriteLine("winter,landtile,136,903");
                writer.WriteLine("winter,landtile,137,902");
                writer.WriteLine("winter,landtile,138,902");
                writer.WriteLine("winter,landtile,139,901");
                writer.WriteLine("winter,landtile,140,901");
                writer.WriteLine("winter,landtile,871,917");
                writer.WriteLine("winter,landtile,872,918");
                writer.WriteLine("winter,landtile,873,919");
                writer.WriteLine("winter,landtile,874,920");
                writer.WriteLine("winter,landtile,875,921");
                writer.WriteLine("winter,landtile,876,922");
                writer.WriteLine("winter,landtile,877,923");
                writer.WriteLine("winter,landtile,878,924");
                writer.WriteLine("winter,landtile,879,925");
                writer.WriteLine("winter,landtile,880,926");
                writer.WriteLine("winter,landtile,881,927");
                writer.WriteLine("winter,landtile,882,928");
                writer.WriteLine("winter,landtile,883,929");
                writer.WriteLine("winter,landtile,884,930");
                writer.WriteLine("winter,landtile,885,931");
                writer.WriteLine("winter,landtile,886,932");
                writer.WriteLine("winter,landtile,887,933");
                writer.WriteLine("winter,landtile,888,934");
                writer.WriteLine("winter,landtile,889,935");
                writer.WriteLine("winter,landtile,890,936");
                writer.WriteLine("winter,landtile,891,937");
                writer.WriteLine("winter,landtile,892,938");
                writer.WriteLine("winter,landtile,893,939");
                writer.WriteLine("winter,landtile,894,940");
                writer.WriteLine("winter,landtile,365,916");
                writer.WriteLine("winter,landtile,366,915");
                writer.WriteLine("winter,landtile,367,913");
                writer.WriteLine("winter,landtile,368,914");
                writer.WriteLine("winter,landtile,236,278");
                writer.WriteLine("winter,landtile,237,279");
                writer.WriteLine("winter,landtile,238,276");
                writer.WriteLine("winter,landtile,239,277");
                writer.WriteLine("winter,landtile,240,305");
                writer.WriteLine("winter,landtile,241,302");
                writer.WriteLine("winter,landtile,242,303");
                writer.WriteLine("winter,landtile,243,304");
                writer.WriteLine("winter,landtile,244,272");
                writer.WriteLine("winter,landtile,245,273");
                writer.WriteLine("winter,landtile,246,274");
                writer.WriteLine("winter,landtile,247,275");
                writer.WriteLine("winter,landtile,561,268");
                writer.WriteLine("winter,landtile,562,269");
                writer.WriteLine("winter,landtile,563,270");
                writer.WriteLine("winter,landtile,564,271");
                writer.WriteLine("winter,landtile,565,272");
                writer.WriteLine("winter,landtile,566,273");
                writer.WriteLine("winter,landtile,567,274");
                writer.WriteLine("winter,landtile,568,275");
                writer.WriteLine("winter,landtile,569,276");
                writer.WriteLine("winter,landtile,570,277");
                writer.WriteLine("winter,landtile,571,278");
                writer.WriteLine("winter,landtile,572,279");
                writer.WriteLine("winter,landtile,573,1861");
                writer.WriteLine("winter,landtile,574,1862");
                writer.WriteLine("winter,landtile,575,1863");
                writer.WriteLine("winter,landtile,576,1864");
                writer.WriteLine("winter,landtile,577,1865");
                writer.WriteLine("winter,landtile,578,1866");
                writer.WriteLine("winter,landtile,579,1867");
                writer.WriteLine("winter,landtile,1741,1868");
                writer.WriteLine("winter,landtile,1742,1869");
                writer.WriteLine("winter,landtile,1743,1870");
                writer.WriteLine("winter,landtile,1744,1871");
                writer.WriteLine("winter,landtile,1745,1872");
                writer.WriteLine("winter,landtile,1746,1873");
                writer.WriteLine("winter,landtile,1747,1874");
                writer.WriteLine("winter,landtile,1748,1875");
                writer.WriteLine("winter,landtile,1749,1876");
                writer.WriteLine("winter,landtile,1750,1877");
                writer.WriteLine("winter,landtile,1751,1878");
                writer.WriteLine("winter,landtile,1752,1879");
                writer.WriteLine("winter,landtile,1753,1880");
                writer.WriteLine("winter,landtile,1754,1881");
                writer.WriteLine("winter,landtile,1755,1882");
                writer.WriteLine("winter,landtile,1756,1883");
                writer.WriteLine("winter,landtile,1757,1884");
                writer.WriteLine("winter,landtile,1758,282");
                writer.WriteLine("winter,landtile,1759,283");
                writer.WriteLine("winter,landtile,1760,284");
                writer.WriteLine("winter,landtile,1761,285");
                writer.WriteLine("winter,landtile,26,379");
                writer.WriteLine("winter,landtile,27,378");
                writer.WriteLine("winter,landtile,28,377");
                writer.WriteLine("winter,landtile,29,380");
                writer.WriteLine("winter,landtile,30,381");
                writer.WriteLine("winter,landtile,31,382");
                writer.WriteLine("winter,landtile,32,383");
                writer.WriteLine("winter,landtile,33,384");
                writer.WriteLine("winter,landtile,34,385");
                writer.WriteLine("winter,landtile,35,386");
                writer.WriteLine("winter,landtile,36,387");
                writer.WriteLine("winter,landtile,37,388");
                writer.WriteLine("winter,landtile,38,389");
                writer.WriteLine("winter,landtile,39,390");
                writer.WriteLine("winter,landtile,40,391");
                writer.WriteLine("winter,landtile,41,392");
                writer.WriteLine("winter,landtile,42,393");
                writer.WriteLine("winter,landtile,43,394");
                writer.WriteLine("winter,landtile,44,387");
                writer.WriteLine("winter,landtile,45,388");
                writer.WriteLine("winter,landtile,46,383");
                writer.WriteLine("winter,landtile,47,380");
                writer.WriteLine("winter,landtile,48,383");
                writer.WriteLine("winter,landtile,49,378");
                writer.WriteLine("winter,landtile,50,379");
                writer.WriteLine("winter,landtile,141,379");
                writer.WriteLine("winter,landtile,142,386");
                writer.WriteLine("winter,landtile,143,385");
                writer.WriteLine("winter,landtile,144,393");
                writer.WriteLine("winter,landtile,145,378");
                writer.WriteLine("winter,landtile,146,387");
                writer.WriteLine("winter,landtile,147,391");
                writer.WriteLine("winter,landtile,148,392");
                writer.WriteLine("winter,landtile,149,377");
                writer.WriteLine("winter,landtile,150,379");
                writer.WriteLine("winter,landtile,151,383");
                writer.WriteLine("winter,landtile,152,380");
                writer.WriteLine("winter,landtile,153,387");
                writer.WriteLine("winter,landtile,154,388");
                writer.WriteLine("winter,landtile,155,393");
                writer.WriteLine("winter,landtile,156,391");
                writer.WriteLine("winter,landtile,157,387");
                writer.WriteLine("winter,landtile,158,385");
                writer.WriteLine("winter,landtile,159,385");
                writer.WriteLine("winter,landtile,160,389");
                writer.WriteLine("winter,landtile,161,379");
                writer.WriteLine("winter,landtile,162,384");
                writer.WriteLine("winter,landtile,163,380");
                writer.WriteLine("winter,landtile,164,379");
                writer.WriteLine("winter,landtile,165,378");
                writer.WriteLine("winter,landtile,166,378");
                writer.WriteLine("winter,landtile,167,394");
                writer.WriteLine("winter,landtile,1521,282");
                writer.WriteLine("winter,landtile,1522,283");
                writer.WriteLine("winter,landtile,1523,284");
                writer.WriteLine("winter,landtile,1524,285");
                writer.WriteLine("winter,landtile,1529,282");
                writer.WriteLine("winter,landtile,1530,283");
                writer.WriteLine("winter,landtile,1531,284");
                writer.WriteLine("winter,landtile,1532,285");
                writer.WriteLine("winter,landtile,1533,282");
                writer.WriteLine("winter,landtile,1534,283");
                writer.WriteLine("winter,landtile,1535,284");
                writer.WriteLine("winter,landtile,1536,285");
                writer.WriteLine("winter,landtile,1537,282");
                writer.WriteLine("winter,landtile,1538,283");
                writer.WriteLine("winter,landtile,1539,284");
                writer.WriteLine("winter,landtile,1540,285");
                writer.WriteLine("winter,landtile,741,379");
                writer.WriteLine("winter,landtile,742,385");
                writer.WriteLine("winter,landtile,743,389");
                writer.WriteLine("winter,landtile,744,393");
                writer.WriteLine("winter,landtile,745,378");
                writer.WriteLine("winter,landtile,746,384");
                writer.WriteLine("winter,landtile,747,388");
                writer.WriteLine("winter,landtile,748,392");
                writer.WriteLine("winter,landtile,749,377");
                writer.WriteLine("winter,landtile,750,385");
                writer.WriteLine("winter,landtile,751,383");
                writer.WriteLine("winter,landtile,752,380");
                writer.WriteLine("winter,landtile,753,391");
                writer.WriteLine("winter,landtile,754,388");
                writer.WriteLine("winter,landtile,755,385");
                writer.WriteLine("winter,landtile,756,384");
                writer.WriteLine("winter,landtile,757,391");
                writer.WriteLine("winter,landtile,758,379");
                writer.WriteLine("winter,landtile,759,393");
                writer.WriteLine("winter,landtile,760,383");
                writer.WriteLine("winter,landtile,761,385");
                writer.WriteLine("winter,landtile,762,391");
                writer.WriteLine("winter,landtile,763,391");
                writer.WriteLine("winter,landtile,764,379");
                writer.WriteLine("winter,landtile,765,384");
                writer.WriteLine("winter,landtile,766,384");
                writer.WriteLine("winter,landtile,767,379");
                writer.WriteLine("winter,landtile,9,282");
                writer.WriteLine("winter,landtile,10,283");
                writer.WriteLine("winter,landtile,11,284");
                writer.WriteLine("winter,landtile,12,285");
                writer.WriteLine("winter,landtile,13,282");
                writer.WriteLine("winter,landtile,14,283");
                writer.WriteLine("winter,landtile,15,284");
                writer.WriteLine("winter,landtile,16,285");
                writer.WriteLine("winter,landtile,17,282");
                writer.WriteLine("winter,landtile,18,283");
                writer.WriteLine("winter,landtile,19,284");
                writer.WriteLine("winter,landtile,20,285");
                writer.WriteLine("winter,landtile,21,282");
                writer.WriteLine("desolation,static,0x1B7E,0x1E34");
                writer.WriteLine("desolation,static,0x0D2B,0x1B15");
                writer.WriteLine("desolation,static,0x0D11,0x122B");
                writer.WriteLine("desolation,static,0x0D14,0x122B");
                writer.WriteLine("desolation,static,0x0D17,0x122B");
                writer.WriteLine("desolation,static,0x0D16,0x1B8D");
                writer.WriteLine("desolation,static,0x0CB9,0x1B8D");
                writer.WriteLine("desolation,static,0x0CBA,0x1B8D");
                writer.WriteLine("desolation,static,0x0CBB,0x1B8D");
                writer.WriteLine("desolation,static,0x0CBC,0x1B8D");
                writer.WriteLine("desolation,static,0x0CBD,0x1B8D");
                writer.WriteLine("desolation,static,0x0CBE,0x1B8D");
                writer.WriteLine("desolation,static,0x0CC7,0x1B0D");
                writer.WriteLine("desolation,static,0x0CE9,0x0ED7");
                writer.WriteLine("desolation,static,0x0CEA,0x0D3F");
                writer.WriteLine("desolation,static,0x0D0F,0x1B1C");
                writer.WriteLine("desolation,static,0x0CB8,0x1CEA");
                writer.WriteLine("desolation,static,0x0C84,0x1B84");
                writer.WriteLine("desolation,static,0x0C8B,0x1B84");
                writer.WriteLine("desolation,static,0x0C9E,0x1182");
                writer.WriteLine("desolation,static,0x0CAD,0x1AE1");
                writer.WriteLine("desolation,static,0x0C4C,0x1B16");
                writer.WriteLine("desolation,static,0x0C8E,0x1B8D");
                writer.WriteLine("desolation,static,0x0C99,0x1B8D");
                writer.WriteLine("desolation,static,0x0CAC,0x1B8D");
                writer.WriteLine("desolation,static,0x0C46,0x1B9D");
                writer.WriteLine("desolation,static,0x0C49,0x1B9D");
                writer.WriteLine("desolation,static,0x0CB6,0x1B9D");
                writer.WriteLine("desolation,static,0x0C45,0x1B9C");
                writer.WriteLine("desolation,static,0x0C48,0x1B9C");
                writer.WriteLine("desolation,static,0x0C4E,0x1B9C");
                writer.WriteLine("desolation,static,0x0C85,0x1B9C");
                writer.WriteLine("desolation,static,0x0CA7,0x1B9C");
                writer.WriteLine("desolation,static,0x0CAE,0x1B9C");
                writer.WriteLine("desolation,static,0x0CAF,0x1B9C");
                writer.WriteLine("desolation,static,0x0CB5,0x1B9C");
                writer.WriteLine("desolation,static,0x0D15,0x1B9C");
                writer.WriteLine("desolation,static,0x0D29,0x1B9C");
                writer.WriteLine("desolation,static,0x0C37,0x1BAE");
                writer.WriteLine("desolation,static,0x0C38,0x1BAE");
                writer.WriteLine("desolation,static,0x0C47,0x1BAE");
                writer.WriteLine("desolation,static,0x0C4A,0x1BAE");
                writer.WriteLine("desolation,static,0x0C4B,0x1BAE");
                writer.WriteLine("desolation,static,0x0C4D,0x1BAE");
                writer.WriteLine("desolation,static,0x0C8C,0x1BAE");
                writer.WriteLine("desolation,static,0x0C8D,0x1BAE");
                writer.WriteLine("desolation,static,0x0C93,0x1BAE");
                writer.WriteLine("desolation,static,0x0C94,0x1BAE");
                writer.WriteLine("desolation,static,0x0C98,0x1BAE");
                writer.WriteLine("desolation,static,0x0C9F,0x1BAE");
                writer.WriteLine("desolation,static,0x0CA0,0x1BAE");
                writer.WriteLine("desolation,static,0x0CA1,0x1BAE");
                writer.WriteLine("desolation,static,0x0CA2,0x1BAE");
                writer.WriteLine("desolation,static,0x0CA3,0x1BAE");
                writer.WriteLine("desolation,static,0x0CA4,0x1BAE");
                writer.WriteLine("desolation,static,0x0CB0,0x1BAE");
                writer.WriteLine("desolation,static,0x0CB1,0x1BAE");
                writer.WriteLine("desolation,static,0x0CB2,0x1BAE");
                writer.WriteLine("desolation,static,0x0CB3,0x1BAE");
                writer.WriteLine("desolation,static,0x0CB4,0x1BAE");
                writer.WriteLine("desolation,static,0x0CB7,0x1BAE");
                writer.WriteLine("desolation,static,0x0CC5,0x1BAE");
                writer.WriteLine("desolation,static,0x0D0C,0x1BAE");
                writer.WriteLine("desolation,static,0x0D0D,0x1BAE");
                writer.WriteLine("desolation,static,0x0D0E,0x1BAE");
                writer.WriteLine("desolation,static,0x0D10,0x1BAE");
                writer.WriteLine("desolation,static,0x0D12,0x1BAE");
                writer.WriteLine("desolation,static,0x0D13,0x1BAE");
                writer.WriteLine("desolation,static,0x0D18,0x1BAE");
                writer.WriteLine("desolation,static,0x0D19,0x1BAE");
                writer.WriteLine("desolation,static,0x0D2D,0x1BAE");
                writer.WriteLine("desolation,static,0x0D2F,0x1BAE");
            }
        }

        #endregion
    }
}