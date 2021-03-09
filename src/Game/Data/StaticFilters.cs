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

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using ClassicUO.Configuration;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using ClassicUO.Utility;

namespace ClassicUO.Game.Data
{
    [Flags]
    internal enum STATIC_TILES_FILTER_FLAGS : byte
    {
        STFF_CAVE = 0x01,
        STFF_STUMP = 0x02,
        STFF_STUMP_HATCHED = 0x04,
        STFF_VEGETATION = 0x08,
        STFF_WATER = 0x10
    }

    internal static class StaticFilters
    {
        private static readonly STATIC_TILES_FILTER_FLAGS[] _filteredTiles = new STATIC_TILES_FILTER_FLAGS[Constants.MAX_STATIC_DATA_INDEX_COUNT];

        public static readonly List<ushort> CaveTiles = new List<ushort>();
        public static readonly List<ushort> TreeTiles = new List<ushort>();

        public static void Load()
        {
            string path = Path.Combine(CUOEnviroment.ExecutablePath, "Data", "Client");

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            string cave = Path.Combine(path, "cave.txt");
            string vegetation = Path.Combine(path, "vegetation.txt");
            string trees = Path.Combine(path, "tree.txt");


            if (!File.Exists(cave))
            {
                using (StreamWriter writer = new StreamWriter(cave))
                {
                    for (int i = 0x053B; i < 0x0553 + 1; i++)
                    {
                        if (i != 0x0550)
                        {
                            writer.WriteLine(i);
                        }
                    }
                }
            }

            if (!File.Exists(vegetation))
            {
                using (StreamWriter writer = new StreamWriter(vegetation))
                {
                    ushort[] vegetationTiles =
                    {
                        0x0D45, 0x0D46, 0x0D47, 0x0D48, 0x0D49, 0x0D4A, 0x0D4B, 0x0D4C, 0x0D4D, 0x0D4E, 0x0D4F,
                        0x0D50, 0x0D51, 0x0D52, 0x0D53, 0x0D54, 0x0D5C, 0x0D5D, 0x0D5E, 0x0D5F, 0x0D60, 0x0D61,
                        0x0D62, 0x0D63, 0x0D64, 0x0D65, 0x0D66, 0x0D67, 0x0D68, 0x0D69, 0x0D6D, 0x0D73, 0x0D74,
                        0x0D75, 0x0D76, 0x0D77, 0x0D78, 0x0D79, 0x0D7A, 0x0D7B, 0x0D7C, 0x0D7D, 0x0D7E, 0x0D7F,
                        0x0D80, 0x0D83, 0x0D87, 0x0D88, 0x0D89, 0x0D8A, 0x0D8B, 0x0D8C, 0x0D8D, 0x0D8E, 0x0D8F,
                        0x0D90, 0x0D91, 0x0D93, 0x12B6, 0x12B7, 0x12BC, 0x12BD, 0x12BE, 0x12BF, 0x12C0, 0x12C1,
                        0x12C2, 0x12C3, 0x12C4, 0x12C5, 0x12C6, 0x12C7, 0x0CB9, 0x0CBC, 0x0CBD, 0x0CBE, 0x0CBF,
                        0x0CC0, 0x0CC1, 0x0CC3, 0x0CC5, 0x0CC6, 0x0CC7, 0x0CF3, 0x0CF4, 0x0CF5, 0x0CF6, 0x0CF7,
                        0x0D04, 0x0D06, 0x0D07, 0x0D08, 0x0D09, 0x0D0A, 0x0D0B, 0x0D0C, 0x0D0D, 0x0D0E, 0x0D0F,
                        0x0D10, 0x0D11, 0x0D12, 0x0D13, 0x0D14, 0x0D15, 0x0D16, 0x0D17, 0x0D18, 0x0D19, 0x0D28,
                        0x0D29, 0x0D2A, 0x0D2B, 0x0D2D, 0x0D34, 0x0D36, 0x0DAE, 0x0DAF, 0x0DBA, 0x0DBB, 0x0DBC,
                        0x0DBD, 0x0DBE, 0x0DC1, 0x0DC2, 0x0DC3, 0x0C83, 0x0C84, 0x0C85, 0x0C86, 0x0C87, 0x0C88,
                        0x0C89, 0x0C8A, 0x0C8B, 0x0C8C, 0x0C8D, 0x0C8E, 0x0C93, 0x0C94, 0x0C98, 0x0C9F, 0x0CA0,
                        0x0CA1, 0x0CA2, 0x0CA3, 0x0CA4, 0x0CA7, 0x0CAC, 0x0CAD, 0x0CAE, 0x0CAF, 0x0CB0, 0x0CB1,
                        0x0CB2, 0x0CB3, 0x0CB4, 0x0CB5, 0x0CB6, 0x0C45, 0x0C46, 0x0C49, 0x0C47, 0x0C48, 0x0C4A,
                        0x0C4B, 0x0C4C, 0x0C4D, 0x0C4E, 0x0C37, 0x0C38, 0x0CBA, 0x0D2F, 0x0D32, 0x0D33, 0x0D3F,
                        0x0D40, 0x0CE9
                    };

                    for (int i = 0; i < vegetationTiles.Length; i++)
                    {
                        ushort g = vegetationTiles[i];

                        if (TileDataLoader.Instance.StaticData[g].IsImpassable)
                        {
                            continue;
                        }

                        writer.WriteLine(g);
                    }
                }
            }

            if (!File.Exists(trees))
            {
                using (StreamWriter writer = new StreamWriter(trees))
                using (StreamWriter writerveg = new StreamWriter(vegetation, true))
                {
                    ushort[] treeTiles =
                    {
                        0x0C95, 0x0C96, 0x0C99, 0x0C9B, 0x0C9C, 0x0C9D, 0x0C9E, 0x0CA6, 0x0CA8, 0x0CAA, 0x0CAB,
                        0x0CC9, 0x0CCA, 0x0CCB, 0x0CCC, 0x0CCD, 0x0CD0, 0x0CD3, 0x0CD6, 0x0CD8, 0x0CDA, 0x0CDD,
                        0x0CE0, 0x0CE3, 0x0CE6, 0x0CF8, 0x0CFB, 0x0CFE, 0x0D01, 0x0D37, 0x0D38, 0x0D41, 0x0D42,
                        0x0D43, 0x0D44, 0x0D57, 0x0D58, 0x0D59, 0x0D5A, 0x0D5B, 0x0D6E, 0x0D6F, 0x0D70, 0x0D71,
                        0x0D72, 0x0D84, 0x0D85, 0x0D86, 0x0D94, 0x0D98, 0x0D9C, 0x0DA0, 0x0DA4, 0x0DA8, 0x12B6,
                        0x12B7, 0x12B8, 0x12B9, 0x12BA, 0x12BB, 0x12BC, 0x12BD
                    };

                    for (int i = 0; i < treeTiles.Length; i++)
                    {
                        ushort graphic = treeTiles[i];
                        byte flag = 1;

                        switch (graphic)
                        {
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
                                flag = 0;

                                break;
                        }

                        if (!TileDataLoader.Instance.StaticData[graphic].IsImpassable)
                        {
                            writerveg.WriteLine(graphic);
                        }
                        else
                        {
                            writer.WriteLine($"{graphic}={flag}");
                        }
                    }
                }
            }


            TextFileParser caveParser = new TextFileParser(File.ReadAllText(cave), new[] { ' ', '\t', ',' }, new[] { '#', ';' }, new[] { '"', '"' });

            while (!caveParser.IsEOF())
            {
                List<string> ss = caveParser.ReadTokens();

                if (ss != null && ss.Count != 0)
                {
                    if (ushort.TryParse(ss[0], out ushort graphic))
                    {
                        _filteredTiles[graphic] |= STATIC_TILES_FILTER_FLAGS.STFF_CAVE;
                        CaveTiles.Add(graphic);
                    }
                }
            }


            TextFileParser stumpsParser = new TextFileParser(File.ReadAllText(trees), new[] { ' ', '\t', ',', '=' }, new[] { '#', ';' }, new[] { '"', '"' });

            while (!stumpsParser.IsEOF())
            {
                List<string> ss = stumpsParser.ReadTokens();

                if (ss != null && ss.Count >= 2)
                {
                    STATIC_TILES_FILTER_FLAGS flag = STATIC_TILES_FILTER_FLAGS.STFF_STUMP;

                    if (byte.TryParse(ss[1], out byte f) && f != 0)
                    {
                        flag |= STATIC_TILES_FILTER_FLAGS.STFF_STUMP_HATCHED;
                    }

                    if (ushort.TryParse(ss[0], out ushort graphic))
                    {
                        _filteredTiles[graphic] |= flag;
                        TreeTiles.Add(graphic);
                    }
                }
            }


            TextFileParser vegetationParser = new TextFileParser(File.ReadAllText(vegetation), new[] { ' ', '\t', ',' }, new[] { '#', ';' }, new[] { '"', '"' });

            while (!vegetationParser.IsEOF())
            {
                List<string> ss = vegetationParser.ReadTokens();

                if (ss != null && ss.Count != 0)
                {
                    if (ushort.TryParse(ss[0], out ushort graphic))
                    {
                        _filteredTiles[graphic] |= STATIC_TILES_FILTER_FLAGS.STFF_VEGETATION;
                    }
                }
            }
        }

        public static void CleanCaveTextures()
        {
            foreach (ushort graphic in CaveTiles)
            {
                ArtTexture texture = ArtLoader.Instance.GetTexture(graphic);

                if (texture != null)
                {
                    texture.Ticks = 0;
                }
            }

            ArtLoader.Instance.CleaUnusedResources(short.MaxValue);
        }

        public static void CleanTreeTextures()
        {
            foreach (ushort graphic in TreeTiles)
            {
                ArtTexture texture = ArtLoader.Instance.GetTexture(graphic);

                if (texture != null)
                {
                    texture.Ticks = 0;
                }
            }

            ArtLoader.Instance.CleaUnusedResources(short.MaxValue);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsTree(ushort g, out int index)
        {
            STATIC_TILES_FILTER_FLAGS flag = _filteredTiles[g];

            if ((flag & STATIC_TILES_FILTER_FLAGS.STFF_STUMP) != 0)
            {
                if ((flag & STATIC_TILES_FILTER_FLAGS.STFF_STUMP_HATCHED) != 0)
                {
                    index = 0;
                }
                else
                {
                    index = 1;
                }

                return true;
            }

            index = 0;

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsVegetation(ushort g)
        {
            return (_filteredTiles[g] & STATIC_TILES_FILTER_FLAGS.STFF_VEGETATION) != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsCave(ushort g)
        {
            return (_filteredTiles[g] & STATIC_TILES_FILTER_FLAGS.STFF_CAVE) != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
                case 4962: return true;

                default: return g >= 6001 && g <= 6012;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsField(ushort g)
        {
            return g >= 0x398C && g <= 0x399F || g >= 0x3967 && g <= 0x397A || g >= 0x3946 && g <= 0x3964 || g >= 0x3914 && g <= 0x3929;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsFireField(ushort g)
        {
            return g >= 0x398C && g <= 0x399F;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsParalyzeField(ushort g)
        {
            return g >= 0x3967 && g <= 0x397A;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsEnergyField(ushort g)
        {
            return g >= 0x3946 && g <= 0x3964;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsPoisonField(ushort g)
        {
            return g >= 0x3914 && g <= 0x3929;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsWallOfStone(ushort g)
        {
            return g == 0x038A;
        }
    }
}