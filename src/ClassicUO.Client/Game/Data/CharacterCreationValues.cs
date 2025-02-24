// SPDX-License-Identifier: BSD-2-Clause

using System.Linq;
using ClassicUO.Assets;

namespace ClassicUO.Game.Data
{
    internal class CharacterCreationValues
    {
        private static readonly ushort[] HumanSkinTone =
        {
            0x03E9, 0x03F1, 0x03F9, 0x0401, 0x0409, 0x0411, 0x0419, 0x0421, 0x03EA, 0x03F2, 0x03FA, 0x0402, 0x040A,
            0x0412, 0x041A, 0x0421, 0x03EB, 0x03F3, 0x03FB, 0x0403, 0x040B, 0x0413, 0x041B, 0x0421, 0x03EC, 0x03F4,
            0x03FC, 0x0404, 0x040C, 0x0414, 0x041C, 0x0421, 0x03ED, 0x03F5, 0x03FD, 0x0405, 0x040D, 0x0415, 0x041D,
            0x0421, 0x03EE, 0x03F6, 0x03FE, 0x0406, 0x040E, 0x0416, 0x041E, 0x0421, 0x03EF, 0x03F7, 0x03FF, 0x0407,
            0x040F, 0x0417, 0x041F, 0x0421, 0x03F0, 0x03F8, 0x0400, 0x0408, 0x0410, 0x0418, 0x0420, 0x0421
        };
        private static readonly ushort[] ElfSkinTone =
        {
            0x04DD, 0x076B, 0x0834, 0x042F, 0x024C, 0x024D, 0x024E, 0x00BE, 0x04A6, 0x0360, 0x0374, 0x0366, 0x03E7,
            0x03DD, 0x0352, 0x0902, 0x076C, 0x0383, 0x0578, 0x03E8, 0x0373, 0x0388, 0x0384, 0x0375, 0x053E, 0x0380,
            0x0381, 0x0382, 0x076A, 0x03E4, 0x051C, 0x03E5
        };
        private static readonly ushort[] GargoyleSkinTone =
        {
            0x06DA, 0x06DB, 0x06DC, 0x06DD, 0x06DE, 0x06DF, 0x06E0, 0x06E1, 0x06E2, 0x06E3, 0x06E4, 0x06E5, 0x06E6,
            0x06E7, 0x06E8, 0x06E9, 0x06EA, 0x06EB, 0x06EC, 0x06ED, 0x06EE, 0x06EF, 0x06F0, 0x06F1, 0x06F2, 0x06DA,
            0x06DB, 0x06DC
        };
        private static readonly ushort[] HumanHairColor =
        {
            0x044D, 0x0455, 0x045D, 0x0465, 0x046D, 0x0475, 0x044E, 0x0456, 0x045E, 0x0466, 0x046E, 0x0476, 0x044F,
            0x0457, 0x045F, 0x0467, 0x046F, 0x0477, 0x0450, 0x0458, 0x0460, 0x0468, 0x0470, 0x0478, 0x0451, 0x0459,
            0x0461, 0x0469, 0x0471, 0x0479, 0x0452, 0x045A, 0x0462, 0x046A, 0x0472, 0x047A, 0x0453, 0x045B, 0x0463,
            0x046B, 0x0473, 0x047B, 0x0454, 0x045C, 0x0464, 0x046C, 0x0474, 0x047C
        };
        private static readonly ushort[] ElfHairColor =
        {
            0x0033, 0x0034, 0x0035, 0x0036, 0x0037, 0x0038, 0x0100, 0x06B7, 0x0206, 0x0210, 0x026B, 0x02C2, 0x02C8,
            0x01E3, 0x0238, 0x0368, 0x059C, 0x0852, 0x008D, 0x008E, 0x008F, 0x0090, 0x0091, 0x0158, 0x0159, 0x015A,
            0x015B, 0x015C, 0x015D, 0x01BC, 0x0724, 0x0057, 0x0127, 0x012E, 0x01F2, 0x0250, 0x031C, 0x031D, 0x031E,
            0x031F, 0x0320, 0x0321, 0x0322, 0x0323, 0x0324, 0x0325, 0x0385, 0x0386, 0x0387, 0x0388, 0x0389, 0x0385,
            0x0386, 0x0387
        };
        private static readonly ushort[] GargoyleHairColor =
        {
            0x0708, 0x070A, 0x070C, 0x070E, 0x0710, 0x0762, 0x0764, 0x0767, 0x076A, 0x06F2, 0x06F0, 0x06EE, 0x06E3,
            0x06E1, 0x06DF, 0x0708, 0x070A, 0x070C
        };
        private static readonly int[] HumanHairLabels =
        {
            3000340, 3000341, 3000342, 3000343, 3000344, 3000345, 3000346, 3000347, 3000348, 3000349
        };
        private static readonly int[] HumanHairGraphics =
        {
            0, 8251, 8252, 8253, 8260, 8261, 8266, 8263, 8264, 8265
        };
        private static readonly int[] HumanFacialLabels =
        {
            3000340, 3000351, 3000352, 3000353, 3000354, 1011060, 1011061, 3000357
        };
        private static readonly int[] HumanFacialGraphics =
        {
            0, 8256, 8254, 8255, 8257, 8267, 8268, 8269
        };
        private static readonly int[] HumanFemaleHairLabels =
        {
            3000340, 3000341, 3000342, 3000343, 3000344, 3000345, 3000346, 3000347, 3000349, 3000350
        };
        private static readonly int[] HumanFemaleHairGraphics =
        {
            0, 8251, 8252, 8253, 8260, 8261, 8266, 8263, 8265, 8262
        };
        private static readonly int[] ElfHairLabels =
        {
            3000340, 1074385, 1074386, 1074387, 1074388, 1074390, 1074391, 1074392, 1074394
        };
        private static readonly int[] ElfHairGraphics =
        {
            0, 0x2FBF, 0x2FC0, 0x2FC1, 0x2FC2, 0x2FCD, 0x2FCE, 0x2FCF, 0x2FD1
        };
        private static readonly int[] ElfFemaleHairLabels =
        {
            3000340, 1074386, 1074387, 1074388, 1074389, 1074391, 1074392, 1074393, 1074394
        };
        private static readonly int[] ElfFemaleHairGraphics =
        {
            0, 0x2FC0, 0x2FC1, 0x2FC2, 0x2FCC, 0x2FCE, 0x2FCF, 0x2FD0, 0x2FD1
        };
        private static readonly int[] GargoyleHairLabels =
        {
            3000340, 1112310, 1112311, 1112312, 1112313, 1112314, 1112315, 1112316, 1112317
        };
        private static readonly int[] GargoyleHairGraphics =
        {
            0, 0x4258, 0x4259, 0x425A, 0x425B, 0x425C, 0x425D, 0x425E, 0x425F
        };
        private static readonly int[] GargoyleFacialLabels =
        {
            3000340, 1112310, 1112311, 1112312, 1112313
        };
        private static readonly int[] GargoyleFacialGraphics =
        {
            0, 0x42AD, 0x42AE, 0x42AF, 0x42B0
        };
        private static readonly int[] GargoyleFemaleHairLabels =
        {
            3000340, 1112310, 1112311, 1112312, 1112313, 1112314, 1112315, 1112316, 1112317
        };
        private static readonly int[] GargoyleFemaleHairGraphics =
        {
            0, 0x4261, 0x4262, 0x4273, 0x4274, 0x4275, 0x42AA, 0x42AB, 0x42B1
        };

        public static ushort[] GetSkinPallet(RaceType race)
        {
            switch (race)
            {
                case RaceType.HUMAN: return HumanSkinTone;

                case RaceType.ELF: return ElfSkinTone;

                case RaceType.GARGOYLE: return GargoyleSkinTone;
            }

            return new ushort[]
            {
            };
        }

        public static ushort[] GetHairPallet(RaceType race)
        {
            switch (race)
            {
                case RaceType.HUMAN: return HumanHairColor;

                case RaceType.ELF: return ElfHairColor;

                case RaceType.GARGOYLE: return GargoyleHairColor;
            }

            return new ushort[]
            {
            };
        }

        public static ComboContent GetHairComboContent(bool isFemale, RaceType race)
        {
            switch (race)
            {
                case RaceType.HUMAN:

                    if (isFemale)
                    {
                        return new ComboContent(HumanFemaleHairLabels, HumanFemaleHairGraphics);
                    }
                    else
                    {
                        return new ComboContent(HumanHairLabels, HumanHairGraphics);
                    }

                case RaceType.ELF:

                    if (isFemale)
                    {
                        return new ComboContent(ElfFemaleHairLabels, ElfFemaleHairGraphics);
                    }
                    else
                    {
                        return new ComboContent(ElfHairLabels, ElfHairGraphics);
                    }

                case RaceType.GARGOYLE:

                    if (isFemale)
                    {
                        return new ComboContent(GargoyleFemaleHairLabels, GargoyleFemaleHairGraphics);
                    }
                    else
                    {
                        return new ComboContent(GargoyleHairLabels, GargoyleHairGraphics);
                    }
            }

            return new ComboContent
            (
                new int[]
                {
                },
                new int[]
                {
                }
            );
        }

        public static ComboContent GetFacialHairComboContent(RaceType race)
        {
            switch (race)
            {
                case RaceType.HUMAN: return new ComboContent(HumanFacialLabels, HumanFacialGraphics);

                case RaceType.GARGOYLE: return new ComboContent(GargoyleFacialLabels, GargoyleFacialGraphics);
            }

            return new ComboContent
            (
                new int[]
                {
                },
                new int[]
                {
                }
            );
        }

        internal class ComboContent
        {
            private readonly int[] _ids;

            public ComboContent(int[] labels, int[] ids)
            {
                _ids = ids;

                Labels = labels.Select(o => Client.Game.UO.FileManager.Clilocs.GetString(o)).ToArray();
            }

            public string[] Labels { get; }

            public int GetGraphic(int index)
            {
                return _ids[index];
            }
        }
    }
}