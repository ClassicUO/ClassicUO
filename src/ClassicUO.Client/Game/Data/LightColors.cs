// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Utility;
using System;
using System.Collections.Generic;
using System.IO;

namespace ClassicUO.Game.Data
{
    internal static class LightColors
    {

        private static readonly Dictionary<ushort, LightShaderData> _shaderdata = new Dictionary<ushort, LightShaderData>();
        private static readonly Dictionary<ushort, ItemLightData> _itemlightdata = new Dictionary<ushort, ItemLightData>();

        public static bool GetHue(ushort id, out ushort color, out bool ishue)
        {

            ishue = false;
            color = ushort.MaxValue;

            if (_itemlightdata.TryGetValue(id, out ItemLightData lightdata))
            {
                ishue = lightdata.IsHue;
                color = lightdata.Color;
                return true;
            }

            switch (id)
            {
                case 0x088C:
                    color = 31;

                    break;

                case 0x0FAC:
                    color = 30;

                    break;

                case 0x0FB1:
                    color = 60;

                    break;

                case 0x1647:
                    color = 61;

                    break;

                case 0x19BB:
                case 0x1F2B:
                    color = 40;

                    break;

                case 0x9F66:
                    color = 0;

                    break;
            }


            if (id < 0x09FB || id > 0x0A14)
            {
                if (id < 0x0A15 || id > 0x0A29)
                {
                    if (id < 0x0B1A || id > 0x0B1F)
                    {
                        if (id < 0x0B20 || id > 0x0B25)
                        {
                            if (id < 0x0B26 || id > 0x0B28)
                            {
                                if (id < 0x0DE1 || id > 0x0DEA)
                                {
                                    if (id < 0x1849 || id > 0x1850)
                                    {
                                        if (id < 0x1853 || id > 0x185A)
                                        {
                                            if (id < 0x197A || id > 0x19A9)
                                            {
                                                if (id < 0x19AB || id > 0x19B6)
                                                {
                                                    if (id >= 0x1ECD && id <= 0x1ECF || id >= 0x1ED0 && id <= 0x1ED2)
                                                    {
                                                        color = 1;
                                                    }
                                                }
                                                else
                                                {
                                                    color = 60;
                                                }
                                            }
                                            else
                                            {
                                                color = 60;
                                            }
                                        }
                                        else
                                        {
                                            color = 61;
                                        }
                                    }
                                    else
                                    {
                                        color = 61;
                                    }
                                }
                                else
                                {
                                    color = 31;
                                }
                            }
                            else
                            {
                                color = 0;
                            }
                        }
                        else
                        {
                            color = 0;
                        }
                    }
                    else
                    {
                        color = 0;
                    }
                }
                else
                {
                    color = 0;
                }
            }
            else
            {
                color = 30;
            }

            if (id == 0x1FD4 || id == 0x0F6C)
            {
                color = 2;
            }

            if (id < 0x0E2D || id > 0x0E30)
            {
                if (id < 0x0E31 || id > 0x0E33)
                {
                    if (id < 0x0E5C || id > 0x0E6A)
                    {
                        if (id < 0x12EE || id > 0x134D)
                        {
                            if (id < 0x306A || id > 0x329B)
                            {
                                if (id < 0x343B || id > 0x346C)
                                {
                                    if (id < 0x3547 || id > 0x354C)
                                    {
                                        if (id < 0x3914 || id > 0x3929)
                                        {
                                            if (id < 0x3946 || id > 0x3964)
                                            {
                                                if (id < 0x3967 || id > 0x397A)
                                                {
                                                    if (id < 0x398C || id > 0x399F)
                                                    {
                                                        if (id < 0x3E02 || id > 0x3E0B)
                                                        {
                                                            if (id < 0x3E27 || id > 0x3E3A)
                                                            {
                                                                switch (id)
                                                                {
                                                                    case 0x40FE:
                                                                        color = 40;

                                                                        break;

                                                                    case 0x40FF:
                                                                        color = 10;

                                                                        break;

                                                                    case 0x4100:
                                                                        color = 20;

                                                                        break;

                                                                    case 0x4101:
                                                                        color = 32;

                                                                        break;

                                                                    default:

                                                                        if (id >= 0x983B && id <= 0x983D || id >= 0x983F && id <= 0x9841)
                                                                        {
                                                                            color = 30;
                                                                        }

                                                                        break;
                                                                }
                                                            }
                                                            else
                                                            {
                                                                color = 31;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            color = 1;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        color = 31;
                                                    }
                                                }
                                                else
                                                {
                                                    color = 6;
                                                }
                                            }
                                            else
                                            {
                                                color = 6;
                                            }
                                        }
                                        else
                                        {
                                            color = 1;
                                        }
                                    }
                                    else
                                    {
                                        color = 31;
                                    }
                                }
                                else
                                {
                                    color = 31;
                                }
                            }
                            else
                            {
                                color = 31;
                            }
                        }
                        else
                        {
                            color = 31;
                        }
                    }
                    else
                    {
                        color = 6;
                    }
                }
                else
                {
                    color = 40;
                }
            }
            else
            {
                color = 62;
            }

            if (color == ushort.MaxValue)
            {
                return false;
            }

            return true;
        }

        private static void MakeDefaultShaders(int count)
        {
            _shaderdata.Clear();

            for (ushort i = 1; i <= count; i++)
            {
                _shaderdata[i] = new LightShaderData((uint)0xFF_FF_FF);
            }

            // green small
            _shaderdata[1] = new LightShaderData((uint)0x00_FF_00, greencurve: LightShaderCurve.A);

            // light blue
            _shaderdata[2] = new LightShaderData((uint)0x7F_7F_FF);

            // dark blue
            _shaderdata[6] = new LightShaderData((uint)0xFF_00_FF, bluecurve: LightShaderCurve.A, redcurve: LightShaderCurve.B);

            // blue
            _shaderdata[10] = new LightShaderData((uint)0x3F_3F_FF);

            // green
            _shaderdata[20] = new LightShaderData((uint)0x00_FF_00);

            // orange
            _shaderdata[30] = new LightShaderData((uint)0xFF_7F_00, greencurve: LightShaderCurve.C, redcurve: LightShaderCurve.C);

            // orange small
            _shaderdata[31] = new LightShaderData((uint)0xFF_7F_00, greencurve: LightShaderCurve.A, redcurve: LightShaderCurve.A);

            // purple
            _shaderdata[32] = new LightShaderData((uint)0xFF_00_FF);

            // red
            _shaderdata[40] = new LightShaderData((uint)0xFF_00_00);

            // yellow
            _shaderdata[50] = new LightShaderData((uint)0xFF_FF_00);

            // yellow small
            _shaderdata[60] = new LightShaderData((uint)0xFF_FF_00, redcurve: LightShaderCurve.A, greencurve: LightShaderCurve.A);

            // yellow medium
            _shaderdata[61] = new LightShaderData((uint)0xFF_FF_00, redcurve: LightShaderCurve.D, greencurve: LightShaderCurve.D);

            // white medium
            _shaderdata[62] = new LightShaderData((uint)0xFF_FF_FF, LightShaderCurve.D, LightShaderCurve.D, LightShaderCurve.D);

            // white small full
            _shaderdata[63] = new LightShaderData((uint)0xFF_FF_FF, LightShaderCurve.E, LightShaderCurve.E, LightShaderCurve.E);
        }

        public static void BuildLightShaderFiles(bool force)
        {
            string path = Path.Combine(CUOEnviroment.ExecutablePath, "Data", "Client");

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            path = Path.Combine(path, "lightshaders.txt");

            if (!File.Exists(path) || force)
            {
                using (StreamWriter writer = new StreamWriter(File.Create(path)))
                {
                    writer.WriteLine("# FORMAT");

                    writer.WriteLine("# ID RGB R_CURVE G_CURVE B_CURVE");
                    writer.WriteLine("# HARD LIMIT IS 63");
                    writer.WriteLine("#");
                    writer.WriteLine("# DEFAULT SHADERS:");

                    foreach (KeyValuePair<ushort, LightShaderData> e in _shaderdata)
                    {
                        writer.WriteLine($"# {e.Key} {e.Value.RGB:X6} {e.Value.RedCurve} {e.Value.GreenCurve} {e.Value.BlueCurve}");
                    }
                }
            }

            TextFileParser lightshadersParser = new TextFileParser(File.ReadAllText(path), new[] { ' ', '\t', ',' }, new[] { '#', ';' }, new[] { '"', '"' });

            while (!lightshadersParser.IsEOF())
            {
                List<string> ss = lightshadersParser.ReadTokens();

                if (ss != null && ss.Count != 0)
                {
                    LightShaderCurve curver = LightShaderCurve.Standard;
                    LightShaderCurve curveg = LightShaderCurve.Standard;
                    LightShaderCurve curveb = LightShaderCurve.Standard;

                    if (ushort.TryParse(ss[0], out ushort id) && ss.Count > 1)
                    {
                        if (ss.Count > 2)
                        {
                            Enum.TryParse(ss[2], out curver);

                            if (ss.Count > 4)
                            {
                                Enum.TryParse(ss[3], out curveg);
                                Enum.TryParse(ss[4], out curveb);
                            }
                            else
                            {
                                curveg = curveb = curver;
                            }
                        }

                        _shaderdata[id] = new LightShaderData(Convert.ToUInt32(ss[1], 16), curver, curveg, curveb);
                    }
                }
            }
        }

        public static void LoadLights()
        {
            string path = Path.Combine(CUOEnviroment.ExecutablePath, "Data", "Client");

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            string lights = Path.Combine(path, "lights.txt");

            if (!File.Exists(lights))
            {
                using (StreamWriter writer = new StreamWriter(lights))
                {
                    writer.WriteLine("# FORMAT");
                    writer.WriteLine("# ITEM_ID LIGHT_SHADER_OR_HUE");
                    writer.WriteLine("#");
                    writer.WriteLine("# Example for shader");
                    writer.WriteLine("# 0xE31 35");
                    writer.WriteLine("#");
                    writer.WriteLine("# Example for hue");
                    writer.WriteLine("# 0xE31 H1234");
                    writer.WriteLine("");
                }
            }

            TextFileParser itemlightsparser = new TextFileParser(File.ReadAllText(lights), new[] { ' ', '\t', ',' }, new[] { '#', ';' }, new[] { '"', '"' });

            while (!itemlightsparser.IsEOF())
            {
                List<string> ss = itemlightsparser.ReadTokens();

                if (ss != null && ss.Count != 0)
                {
                    ItemLightData entry = new ItemLightData();

                    ushort id = ss[0].StartsWith("0x") ? Convert.ToUInt16(ss[0], 16) : Convert.ToUInt16(ss[0]);

                    string color = ss[1];

                    if (color.StartsWith("H"))
                    {
                        color = color.Replace("H", "");
                        entry.Color = ushort.Parse(color);
                        entry.Color--;
                        entry.IsHue = true;
                    }
                    else
                    {
                        entry.Color = ushort.Parse(color);
                        entry.IsHue = false;
                    }

                    _itemlightdata[id] = entry;
                }
            }
        }

        internal static void CreateLightTextures(uint[] buffer, int count)
        {
            MakeDefaultShaders(count);
            BuildLightShaderFiles(false);

            byte[][] lightCurveTables = new byte[6][]
            {
                new byte[32] { 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31},
                new byte[32] { 0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,2,3,4,6,8,10,12,14,16,18,20,22,24,26,28},
                new byte[32] { 0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,2,3,4,5,6,7,8},
                new byte[32] { 0,1,2,4,6,8,11,14,17,20,23,26,29,30,31,31,31,31,31,31,31,31,31,31,31,31,31,31,31,31,31,31},
                new byte[32] { 0,0,0,0,0,0,0,0,1,1,2,2,3,3,4,4,5,6,7,8,9,10,11,12,13,15,17,19,21,23,25,27},
                new byte[32] { 0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,5,10,15,20,25,30,30,18,18,18,18,18,18,18},
            };

            foreach (KeyValuePair<ushort, LightShaderData> entry in _shaderdata)
            {
                for (uint i = 0; i < 32; i++)
                {
                    uint r = (entry.Value.RGB & 0xFF_00_00) >> 16;
                    uint g = (entry.Value.RGB & 0x00_FF_00) >> 8;
                    uint b = (entry.Value.RGB & 0x00_00_FF);

                    buffer[32 * (entry.Key - 1) + i] = 0xFF_00_00_00 |
                        ((lightCurveTables[(uint) entry.Value.BlueCurve][i] * b) / 31) << 16 |
                        ((lightCurveTables[(uint) entry.Value.GreenCurve][i] * g) / 31) << 8 |
                        ((lightCurveTables[(uint) entry.Value.RedCurve][i] * r) / 31);
                }
            }
        }

        public enum LightShaderCurve
        {
            Standard,
            A, // small
            B, // very small and dim
            C, // full, flat
            D, // medium dim
            E  // halo
        }

        private struct ItemLightData
        {
            public ushort Color;
            public bool IsHue;
        }
    }
}