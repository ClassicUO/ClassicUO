using ClassicUO.Utility;
using System.Collections.Generic;
using System.IO;
using static ClassicUO.Assets.AnimationsLoader;

namespace ClassicUO.Game.Data
{
    internal static class ChairTable
    {
        public static Dictionary<ushort, SittingInfoData> Table = new Dictionary<ushort, SittingInfoData>();

        public static void Load()
        {
            string path = Path.Combine(CUOEnviroment.ExecutablePath, "Data", "Client");

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            string chair = Path.Combine(path, "chair.txt");

            if (!File.Exists(chair))
            {
                using (StreamWriter writer = new StreamWriter(chair))
                {
                    foreach (var item in _defaultTable)
                    {
                        writer.WriteLine($"{item.Graphic},{item.Direction1},{item.Direction2},{item.Direction3},{item.Direction4},{item.OffsetY},{item.MirrorOffsetY}");
                    }
                }
            }

            TextFileParser chairParse = new TextFileParser(File.ReadAllText(chair), new[] { ' ', '\t', ',' }, new[] { '#', ';' }, new[] { '"', '"' });

            while (!chairParse.IsEOF())
            {
                List<string> ss = chairParse.ReadTokens();

                if (ss != null && ss.Count >= 7)
                {
                    ushort.TryParse(ss[0], out ushort graphic);
                    sbyte.TryParse(ss[1], out sbyte d1);
                    sbyte.TryParse(ss[2], out sbyte d2);
                    sbyte.TryParse(ss[3], out sbyte d3);
                    sbyte.TryParse(ss[4], out sbyte d4);
                    sbyte.TryParse(ss[5], out sbyte offsetY);
                    sbyte.TryParse(ss[6], out sbyte mirrorOffsetY);

                    Table.Add(graphic, new SittingInfoData(graphic, d1, d2, d3, d4, offsetY, mirrorOffsetY, false));
                }
            }
        }

        private static readonly SittingInfoData[] _defaultTable =
        {
            new SittingInfoData
            (
                0x0459,
                0,
                -1,
                4,
                -1,
                2,
                2,
                false
            ),
            new SittingInfoData
            (
                0x045A,
                -1,
                2,
                -1,
                6,
                2,
                2,
                false
            ),
            new SittingInfoData
            (
                0x045B,
                0,
                -1,
                4,
                -1,
                2,
                2,
                false
            ),
            new SittingInfoData
            (
                0x045C,
                -1,
                2,
                -1,
                6,
                2,
                2,
                false
            ),
            new SittingInfoData
            (
                0x0A2A,
                0,
                2,
                4,
                6,
                -4,
                -4,
                false
            ),
            new SittingInfoData
            (
                0x0A2B,
                0,
                2,
                4,
                6,
                -8,
                -8,
                false
            ),
            new SittingInfoData
            (
                0x0B2C,
                -1,
                2,
                -1,
                6,
                2,
                2,
                false
            ),
            new SittingInfoData
            (
                0x0B2D,
                0,
                -1,
                4,
                -1,
                2,
                2,
                false
            ),
            new SittingInfoData
            (
                0x0B2E,
                4,
                4,
                4,
                4,
                0,
                0,
                false
            ),
            new SittingInfoData
            (
                0x0B2F,
                2,
                2,
                2,
                2,
                6,
                6,
                false
            ),
            new SittingInfoData
            (
                0x0B30,
                6,
                6,
                6,
                6,
                -8,
                8,
                true
            ),
            new SittingInfoData
            (
                0x0B31,
                0,
                0,
                0,
                0,
                0,
                4,
                true
            ),
            new SittingInfoData
            (
                0x0B32,
                4,
                4,
                4,
                4,
                0,
                0,
                false
            ),
            new SittingInfoData
            (
                0x0B33,
                2,
                2,
                2,
                2,
                0,
                0,
                false
            ),
            new SittingInfoData
            (
                0x0B4E,
                2,
                2,
                2,
                2,
                0,
                0,
                false
            ),
            new SittingInfoData
            (
                0x0B4F,
                4,
                4,
                4,
                4,
                0,
                0,
                false
            ),
            new SittingInfoData
            (
                0x0B50,
                0,
                0,
                0,
                0,
                0,
                0,
                true
            ),
            new SittingInfoData
            (
                0x0B51,
                6,
                6,
                6,
                6,
                0,
                0,
                true
            ),
            new SittingInfoData
            (
                0x0B52,
                2,
                2,
                2,
                2,
                0,
                0,
                false
            ),
            new SittingInfoData
            (
                0x0B53,
                4,
                4,
                4,
                4,
                0,
                0,
                false
            ),
            new SittingInfoData
            (
                0x0B54,
                0,
                0,
                0,
                0,
                0,
                0,
                true
            ),
            new SittingInfoData
            (
                0x0B55,
                6,
                6,
                6,
                6,
                0,
                0,
                true
            ),
            new SittingInfoData
            (
                0x0B56,
                2,
                2,
                2,
                2,
                4,
                4,
                false
            ),
            new SittingInfoData
            (
                0x0B57,
                4,
                4,
                4,
                4,
                4,
                4,
                false
            ),
            new SittingInfoData
            (
                0x0B58,
                6,
                6,
                6,
                6,
                0,
                8,
                true
            ),
            new SittingInfoData
            (
                0x0B59,
                0,
                0,
                0,
                0,
                0,
                8,
                true
            ),
            new SittingInfoData
            (
                0x0B5A,
                2,
                2,
                2,
                2,
                8,
                8,
                false
            ),
            new SittingInfoData
            (
                0x0B5B,
                4,
                4,
                4,
                4,
                8,
                8,
                false
            ),
            new SittingInfoData
            (
                0x0B5C,
                0,
                0,
                0,
                0,
                0,
                8,
                true
            ),
            new SittingInfoData
            (
                0x0B5D,
                6,
                6,
                6,
                6,
                0,
                8,
                true
            ),
            new SittingInfoData
            (
                0x0B5E,
                0,
                2,
                4,
                6,
                -8,
                -8,
                false
            ),
            new SittingInfoData
            (
                0x0B5F,
                -1,
                2,
                -1,
                6,
                3,
                14,
                false
            ),
            new SittingInfoData
            (
                0x0B60,
                -1,
                2,
                -1,
                6,
                3,
                14,
                false
            ),
            new SittingInfoData
            (
                0x0B61,
                -1,
                2,
                -1,
                6,
                3,
                14,
                false
            ),
            new SittingInfoData
            (
                0x0B62,
                -1,
                2,
                -1,
                6,
                3,
                10,
                false
            ),
            new SittingInfoData
            (
                0x0B63,
                -1,
                2,
                -1,
                6,
                3,
                10,
                false
            ),
            new SittingInfoData
            (
                0x0B64,
                -1,
                2,
                -1,
                6,
                3,
                10,
                false
            ),
            new SittingInfoData
            (
                0x0B65,
                0,
                -1,
                4,
                -1,
                3,
                10,
                false
            ),
            new SittingInfoData
            (
                0x0B66,
                0,
                -1,
                4,
                -1,
                3,
                10,
                false
            ),
            new SittingInfoData
            (
                0x0B67,
                0,
                -1,
                4,
                -1,
                3,
                10,
                false
            ),
            new SittingInfoData
            (
                0x0B68,
                0,
                -1,
                4,
                -1,
                3,
                10,
                false
            ),
            new SittingInfoData
            (
                0x0B69,
                0,
                -1,
                4,
                -1,
                3,
                10,
                false
            ),
            new SittingInfoData
            (
                0x0B6A,
                0,
                -1,
                4,
                -1,
                3,
                10,
                false
            ),
            new SittingInfoData
            (
                0x0B91,
                4,
                4,
                4,
                4,
                6,
                6,
                false
            ),
            new SittingInfoData
            (
                0x0B92,
                4,
                4,
                4,
                4,
                6,
                6,
                false
            ),
            new SittingInfoData
            (
                0x0B93,
                2,
                2,
                2,
                2,
                6,
                6,
                false
            ),
            new SittingInfoData
            (
                0x0B94,
                2,
                2,
                2,
                2,
                6,
                6,
                false
            ),
            new SittingInfoData
            (
                0x0CF3,
                -1,
                2,
                -1,
                6,
                2,
                8,
                false
            ),
            new SittingInfoData
            (
                0x0CF4,
                -1,
                2,
                -1,
                6,
                2,
                8,
                false
            ),
            new SittingInfoData
            (
                0x0CF6,
                0,
                -1,
                4,
                -1,
                2,
                8,
                false
            ),
            new SittingInfoData
            (
                0x0CF7,
                0,
                -1,
                4,
                -1,
                2,
                8,
                false
            ),
            new SittingInfoData
            (
                0x0E50,
                4,
                4,
                4,
                4,
                4,
                4,
                false
            ), // SOUTH ONLY
            new SittingInfoData
            (
                0x0E51,
                4,
                4,
                4,
                4,
                4,
                4,
                false
            ), // SOUTH ONLY
            new SittingInfoData
            (
                0x0E52,
                2,
                2,
                2,
                2,
                0,
                0,
                false
            ), // EAST ONLY
            new SittingInfoData
            (
                0x0E53,
                2,
                2,
                2,
                2,
                0,
                0,
                false
            ), // EAST ONLY
            new SittingInfoData
            (
                0x1049,
                -1,
                2,
                -1,
                6,
                2,
                2,
                false
            ), // EAST/WEST
            new SittingInfoData
            (
                0x104A,
                0,
                -1,
                4,
                -1,
                2,
                2,
                false
            ), // NORTH/SOUTH
            new SittingInfoData
            (
                0x11FC,
                0,
                2,
                4,
                6,
                2,
                7,
                false
            ), // ANY
            new SittingInfoData
            (
                0x1207,
                0,
                -1,
                4,
                -1,
                3,
                10,
                false
            ), // NORTH/SOUTH
            new SittingInfoData
            (
                0x1208,
                0,
                -1,
                4,
                -1,
                3,
                10,
                false
            ), // NORTH/SOUTH
            new SittingInfoData
            (
                0x1209,
                0,
                -1,
                4,
                -1,
                3,
                10,
                false
            ), // NORTH/SOUTH
            new SittingInfoData
            (
                0x120A,
                0,
                -1,
                4,
                -1,
                3,
                10,
                false
            ), // NORTH/SOUTH
            new SittingInfoData
            (
                0x120B,
                0,
                -1,
                4,
                -1,
                3,
                10,
                false
            ), // NORTH/SOUTH
            new SittingInfoData
            (
                0x120C,
                0,
                -1,
                4,
                -1,
                3,
                10,
                false
            ), // NORTH/SOUTH
            new SittingInfoData
            (
                0x1218,
                4,
                4,
                4,
                4,
                4,
                4,
                false
            ), // SOUTH ONLY
            new SittingInfoData
            (
                0x1219,
                2,
                2,
                2,
                2,
                4,
                4,
                false
            ), // EAST ONLY
            new SittingInfoData
            (
                0x121A,
                0,
                0,
                0,
                0,
                0,
                8,
                true
            ), // NORTH ONLY
            new SittingInfoData
            (
                0x121B,
                6,
                6,
                6,
                6,
                0,
                8,
                true
            ), // WEST ONLY
            new SittingInfoData
            (
                0x1527,
                2,
                2,
                2,
                2,
                0,
                0,
                false
            ),
            new SittingInfoData
            (
                0x1771,
                0,
                2,
                4,
                6,
                0,
                0,
                false
            ),
            new SittingInfoData
            (
                0x1776,
                0,
                2,
                4,
                6,
                0,
                0,
                false
            ),
            new SittingInfoData
            (
                0x1779,
                0,
                2,
                4,
                6,
                0,
                0,
                false
            ),
            new SittingInfoData
            (
                0x1DC7,
                -1,
                2,
                -1,
                6,
                3,
                10,
                false
            ),
            new SittingInfoData
            (
                0x1DC8,
                -1,
                2,
                -1,
                6,
                3,
                10,
                false
            ),
            new SittingInfoData
            (
                0x1DC9,
                -1,
                2,
                -1,
                6,
                3,
                10,
                false
            ),
            new SittingInfoData
            (
                0x1DCA,
                0,
                -1,
                4,
                -1,
                3,
                10,
                false
            ),
            new SittingInfoData
            (
                0x1DCB,
                0,
                -1,
                4,
                -1,
                3,
                10,
                false
            ),
            new SittingInfoData
            (
                0x1DCC,
                0,
                -1,
                4,
                -1,
                3,
                10,
                false
            ),
            new SittingInfoData
            (
                0x1DCD,
                -1,
                2,
                -1,
                6,
                3,
                10,
                false
            ),
            new SittingInfoData
            (
                0x1DCE,
                -1,
                2,
                -1,
                6,
                3,
                10,
                false
            ),
            new SittingInfoData
            (
                0x1DCF,
                -1,
                2,
                -1,
                6,
                3,
                10,
                false
            ),
            new SittingInfoData
            (
                0x1DD0,
                0,
                -1,
                4,
                -1,
                3,
                10,
                false
            ),
            new SittingInfoData
            (
                0x1DD1,
                0,
                -1,
                4,
                -1,
                3,
                10,
                false
            ),
            new SittingInfoData
            (
                0x1DD2,
                -1,
                2,
                -1,
                6,
                3,
                10,
                false
            ),

            new SittingInfoData
            (
                0x2A58,
                4,
                4,
                4,
                4,
                0,
                0,
                false
            ),
            new SittingInfoData
            (
                0x2A59,
                2,
                2,
                2,
                2,
                0,
                0,
                false
            ),
            new SittingInfoData
            (
                0x2A5A,
                0,
                2,
                4,
                6,
                0,
                0,
                false
            ),
            new SittingInfoData
            (
                0x2A5B,
                0,
                2,
                4,
                6,
                10,
                10,
                false
            ),
            new SittingInfoData
            (
                0x2A7F,
                0,
                2,
                4,
                6,
                0,
                0,
                false
            ),
            new SittingInfoData
            (
                0x2A80,
                0,
                2,
                4,
                6,
                0,
                0,
                false
            ),
            new SittingInfoData
            (
                0x2DDF,
                0,
                2,
                4,
                6,
                2,
                2,
                false
            ),
            new SittingInfoData
            (
                0x2DE0,
                0,
                2,
                4,
                6,
                2,
                2,
                false
            ),
            new SittingInfoData
            (
                0x2DE3,
                2,
                2,
                2,
                2,
                4,
                4,
                false
            ),
            new SittingInfoData
            (
                0x2DE4,
                4,
                4,
                4,
                4,
                4,
                4,
                false
            ),
            new SittingInfoData
            (
                0x2DE5,
                6,
                6,
                6,
                6,
                4,
                4,
                false
            ),
            new SittingInfoData
            (
                0x2DE6,
                0,
                0,
                0,
                0,
                4,
                4,
                false
            ),
            new SittingInfoData
            (
                0x2DEB,
                0,
                0,
                0,
                0,
                4,
                4,
                false
            ),
            new SittingInfoData
            (
                0x2DEC,
                4,
                4,
                4,
                4,
                4,
                4,
                false
            ),
            new SittingInfoData
            (
                0x2DED,
                2,
                2,
                2,
                2,
                4,
                4,
                false
            ),
            new SittingInfoData
            (
                0x2DEE,
                6,
                6,
                6,
                6,
                4,
                4,
                false
            ),
            new SittingInfoData
            (
                0x2DF5,
                0,
                2,
                4,
                6,
                4,
                4,
                false
            ),
            new SittingInfoData
            (
                0x2DF6,
                0,
                2,
                4,
                6,
                4,
                4,
                false
            ),
            new SittingInfoData
            (
                0x3088,
                0,
                2,
                4,
                6,
                4,
                4,
                false
            ),
            new SittingInfoData
            (
                0x3089,
                0,
                2,
                4,
                6,
                4,
                4,
                false
            ),
            new SittingInfoData
            (
                0x308A,
                0,
                2,
                4,
                6,
                4,
                4,
                false
            ),
            new SittingInfoData
            (
                0x308B,
                0,
                2,
                4,
                6,
                4,
                4,
                false
            ),
            new SittingInfoData
            (
                0x319A,
                -1,
                2,
                -1,
                6,
                2,
                2,
                false
            ), // EAST/WEST
            new SittingInfoData
            (
                0x319B,
                0,
                -1,
                4,
                -1,
                2,
                2,
                false
            ), // NORTH/SOUTH
            new SittingInfoData
            (
                0x35ED,
                0,
                2,
                4,
                6,
                0,
                0,
                false
            ),
            new SittingInfoData
            (
                0x35EE,
                0,
                2,
                4,
                6,
                0,
                0,
                false
            ),

            new SittingInfoData
            (
                0x3DFF,
                0,
                -1,
                4,
                -1,
                2,
                2,
                false
            ),
            new SittingInfoData
            (
                0x3E00,
                -1,
                2,
                -1,
                6,
                2,
                2,
                false
            ),
            new SittingInfoData
            (
                0x4023,
                4,
                4,
                4,
                4,
                4,
                4,
                false
            ), // SOUTH ONLY
            new SittingInfoData
            (
                0x4024,
                2,
                2,
                2,
                2,
                0,
                0,
                false
            ), // EAST ONLY
            new SittingInfoData
            (
                0x4027,
                4,
                4,
                4,
                4,
                4,
                4,
                false
            ), // SOUTH ONLY
            new SittingInfoData
            (
                0x4028,
                4,
                4,
                4,
                4,
                4,
                4,
                false
            ), // SOUTH ONLY
            new SittingInfoData
            (
                0x4029,
                2,
                2,
                2,
                2,
                0,
                0,
                false
            ), // EAST ONLY
            new SittingInfoData
            (
                0x402A,
                2,
                2,
                2,
                2,
                0,
                0,
                false
            ), // EAST ONLY
            new SittingInfoData
            (
                0x4BDC,
                4,
                4,
                4,
                4,
                4,
                4,
                false
            ), // SOUTH ONLY
            new SittingInfoData
            (
                0x4C1B,
                4,
                4,
                4,
                4,
                4,
                4,
                false
            ), // SOUTH ONLY
            new SittingInfoData
            (
                0x4C1E,
                2,
                2,
                2,
                2,
                6,
                6,
                false
            ), // EAST ONLY
            new SittingInfoData
            (
                0x4C80,
                4,
                4,
                4,
                4,
                4,
                4,
                false
            ), // SOUTH ONLY
            new SittingInfoData
            (
                0x4C81,
                2,
                2,
                2,
                2,
                0,
                0,
                false
            ), // EAST ONLY
            new SittingInfoData
            (
                0x4C82,
                4,
                4,
                4,
                4,
                4,
                4,
                false
            ), // SOUTH ONLY
            new SittingInfoData
            (
                0x4C83,
                4,
                4,
                4,
                4,
                4,
                4,
                false
            ), // SOUTH ONLY
            new SittingInfoData
            (
                0x4C84,
                2,
                2,
                2,
                2,
                0,
                0,
                false
            ), // EAST ONLY
            new SittingInfoData
            (
                0x4C85,
                2,
                2,
                2,
                2,
                0,
                0,
                false
            ), // EAST ONLY
            new SittingInfoData
            (
                0x4C86,
                4,
                4,
                4,
                4,
                4,
                4,
                false
            ), // SOUTH ONLY
            new SittingInfoData
            (
                0x4C87,
                4,
                4,
                4,
                4,
                4,
                4,
                false
            ), // SOUTH ONLY
            new SittingInfoData
            (
                0x4C88,
                2,
                2,
                2,
                2,
                0,
                0,
                false
            ), // EAST ONLY
            new SittingInfoData
            (
                0x4C89,
                2,
                2,
                2,
                2,
                0,
                0,
                false
            ), // EAST ONLY
            new SittingInfoData
            (
                0x4C8A,
                2,
                2,
                2,
                2,
                0,
                0,
                false
            ), // EAST ONLY
            new SittingInfoData
            (
                0x4C8B,
                2,
                2,
                2,
                2,
                0,
                0,
                false
            ), // EAST ONLY
            new SittingInfoData
            (
                0x4C8C,
                2,
                2,
                2,
                2,
                0,
                0,
                false
            ), // EAST ONLY
            new SittingInfoData
            (
                0x4C8D,
                4,
                4,
                4,
                4,
                4,
                4,
                false
            ), // SOUTH ONLY
            new SittingInfoData
            (
                0x4C8E,
                4,
                4,
                4,
                4,
                4,
                4,
                false
            ), // SOUTH ONLY
            new SittingInfoData
            (
                0x4C8F,
                4,
                4,
                4,
                4,
                4,
                4,
                false
            ), // SOUTH ONLY
            new SittingInfoData
            (
                0x4DE0,
                2,
                2,
                2,
                2,
                0,
                0,
                false
            ), // EAST ONLY
            new SittingInfoData
            (
                0x63BC,
                0,
                -1,
                4,
                -1,
                3,
                10,
                false
            ),
            new SittingInfoData
            (
                0x63BD,
                0,
                -1,
                4,
                -1,
                3,
                10,
                false
            ),
            new SittingInfoData
            (
                0x63C3,
                -1,
                2,
                -1,
                6,
                3,
                14,
                false
            ),
            new SittingInfoData
            (
                0x63C4,
                -1,
                2,
                -1,
                6,
                3,
                14,
                false
            ),
            new SittingInfoData
            (
                0x996C,
                4,
                4,
                4,
                4,
                4,
                4,
                false
            ), // SOUTH ONLY
            new SittingInfoData
            (
                0x9977,
                2,
                2,
                2,
                2,
                0,
                0,
                false
            ), // EAST ONLY
            new SittingInfoData
            (
                0x9C57,
                6,
                6,
                6,
                6,
                6,
                4,
                false
            ), // WEST ONLY
            new SittingInfoData
            (
                0x9C58,
                6,
                6,
                6,
                6,
                6,
                4,
                false
            ), // WEST ONLY
            new SittingInfoData
            (
                0x9C59,
                0,
                0,
                0,
                0,
                4,
                4,
                false
            ), // NORTH ONLY
            new SittingInfoData
            (
                0x9C5A,
                0,
                0,
                0,
                0,
                4,
                4,
                false
            ), // NORTH ONLY
            new SittingInfoData
            (
                0x9C5D,
                6,
                6,
                6,
                6,
                6,
                4,
                false
            ), // WEST ONLY
            new SittingInfoData
            (
                0x9C5E,
                6,
                6,
                6,
                6,
                6,
                4,
                false
            ), // WEST ONLY
            new SittingInfoData
            (
                0x9C5F,
                6,
                6,
                6,
                6,
                6,
                4,
                false
            ), // WEST ONLY
            new SittingInfoData
            (
                0x9C60,
                0,
                0,
                0,
                0,
                4,
                4,
                false
            ), // NORTH ONLY
            new SittingInfoData
            (
                0x9C61,
                0,
                0,
                0,
                0,
                4,
                4,
                false
            ), // NORTH ONLY
            new SittingInfoData
            (
                0x9C62,
                0,
                0,
                0,
                0,
                4,
                4,
                false
            ), // NORTH ONLY
            new SittingInfoData
            (
                0x9E8E,
                0,
                0,
                0,
                0,
                4,
                4,
                false
            ), // NORTH ONLY
            new SittingInfoData
            (
                0x9E8F,
                6,
                6,
                6,
                6,
                6,
                4,
                false
            ), // WEST ONLY
            new SittingInfoData
            (
                0x9E90,
                2,
                2,
                2,
                2,
                0,
                0,
                false
            ), // EAST ONLY
            new SittingInfoData
            (
                0x9E91,
                4,
                4,
                4,
                4,
                4,
                4,
                false
            ), // SOUTH ONLY
            new SittingInfoData
            (
                0x9E9F,
                0,
                0,
                0,
                0,
                4,
                4,
                false
            ), // NORTH ONLY
            new SittingInfoData
            (
                0x9EA0,
                6,
                6,
                6,
                6,
                6,
                4,
                false
            ), // WEST ONLY
            new SittingInfoData
            (
                0x9EA1,
                4,
                4,
                4,
                4,
                4,
                4,
                false
            ), // SOUTH ONLY
            new SittingInfoData
            (
                0x9EA2,
                2,
                2,
                2,
                2,
                0,
                0,
                false
            ), // EAST ONLY
            new SittingInfoData
            (
                0xA05C,
                6,
                6,
                6,
                6,
                6,
                4,
                false
            ), // WEST ONLY
            new SittingInfoData
            (
                0xA05D,
                4,
                4,
                4,
                4,
                4,
                4,
                false
            ), // SOUTH ONLY
            new SittingInfoData
            (
                0xA05E,
                0,
                0,
                0,
                0,
                4,
                4,
                false
            ), // NORTH ONLY
            new SittingInfoData
            (
                0xA05F,
                2,
                2,
                2,
                2,
                0,
                0,
                false
            ), // EAST ONLY
            new SittingInfoData
            (
                0xA211,
                0,
                2,
                4,
                6,
                -4,
                -4,
                false
            ), // ANY
            new SittingInfoData
            (
                0xA4EA,
                4,
                4,
                4,
                4,
                4,
                4,
                false
            ), // SOUTH ONLY
            new SittingInfoData
            (
                0xA4EB,
                2,
                2,
                2,
                2,
                0,
                0,
                false
            ), // EAST ONLY
            new SittingInfoData
            (
                0xA586,
                4,
                4,
                4,
                4,
                4,
                4,
                false
            ), // SOUTH ONLY
            new SittingInfoData
            (
                0xA587,
                2,
                2,
                2,
                2,
                0,
                0,
                false
            ) // EAST ONLY
        };
    }
}
