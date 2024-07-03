using System.Collections.Generic;
using ClassicUO.Assets;

namespace ClassicUO.Game.Data
{
    public static class Mounts
    {
        private static readonly Dictionary<ushort, ushort> _mounts = new ()
        {
            { 0x3E90, 0x0114 }, // 16016 Reptalon
            { 0x3E91, 0x0115 }, // 16017
            { 0x3E92, 0x011C }, // 16018
            { 0x3E94, 0x00F3 }, // 16020
            { 0x3E95, 0x00A9 }, // 16021
            { 0x3E97, 0x00C3 }, // 16023 Ethereal Giant Beetle
            { 0x3E98, 0x00C2 }, // 16024 Ethereal Swamp Dragon
            { 0x3E9A, 0x00C1 }, // 16026 Ethereal Ridgeback
            { 0x3E9B, 0x00C0 }, // 16027
            { 0x3E9D, 0x00C0 }, // 16029 Ethereal Unicorn
            { 0x3E9C, 0x00BF }, // 16028 Ethereal Kirin
            { 0x3E9E, 0x00BE }, // 16030
            { 0x3EA0, 0x00E2 }, // 16032 light grey/horse3
            { 0x3EA1, 0x00E4 }, // 16033 greybrown/horse4
            { 0x3EA2, 0x00CC }, // 16034 dark brown/horse
            { 0x3EA3, 0x00D2 }, // 16035 desert ostard
            { 0x3EA4, 0x00DA }, // 16036 frenzied ostard (=zostrich)
            { 0x3EA5, 0x00DB }, // 16037 forest ostard
            { 0x3EA6, 0x00DC }, // 16038 Llama
            { 0x3EA7, 0x0074 }, // 16039 Nightmare / Vortex
            { 0x3EA8, 0x0075 }, // 16040 Silver Steed
            { 0x3EA9, 0x0072 }, // 16041 Nightmare
            { 0x3EAA, 0x0073 }, // 16042 Ethereal Horse
            { 0x3EAB, 0x00AA }, // 16043 Ethereal Llama
            { 0x3EAC, 0x00AB }, // 16044 Ethereal Ostard
            { 0x3EAD, 0x0084 }, // 16045 Kirin
            { 0x3EAF, 0x0078 }, // 16047 War Horse (Blood Red)
            { 0x3EB0, 0x0079 }, // 16048 War Horse (Light Green)
            { 0x3EB1, 0x0077 }, // 16049 War Horse (Light Blue)
            { 0x3EB2, 0x0076 }, // 16050 War Horse (Purple)
            { 0x3EB3, 0x0090 }, // 16051 Sea Horse (Medium Blue)
            { 0x3EB4, 0x007A }, // 16052 Unicorn
            { 0x3EB5, 0x00B1 }, // 16053 Nightmare
            { 0x3EB6, 0x00B2 }, // 16054 Nightmare 4
            { 0x3EB7, 0x00B3 }, // 16055 Dark Steed
            { 0x3EB8, 0x00BC }, // 16056 Ridgeback
            { 0x3EBA, 0x00BB }, // 16058 Ridgeback, Savage
            { 0x3EBB, 0x0319 }, // 16059 Skeletal Mount
            { 0x3EBC, 0x0317 }, // 16060 Beetle
            { 0x3EBD, 0x031A }, // 16061 SwampDragon
            { 0x3EBE, 0x031F }, // 16062 Armored Swamp Dragon
            { 0x3EC3, 0x02D4 }, // 16067 Beetle
            { 0x3ECE, 0x059A }, // serpentine dragon
            { 0x3EC5, 0x00D5 }, // 16069
            { 0x3F3A, 0x00D5 }, // 16186 snow bear ???
            { 0x3EC6, 0x01B0 }, // 16070 Boura
            { 0x3EC7, 0x04E6 }, // 16071 Tiger
            { 0x3EC8, 0x04E7 }, // 16072 Tiger
            { 0x3EC9, 0x042D }, // 16073
            { 0x3ECA, 0x0579 }, // tarantula
            { 0x3ECC, 0x0582 }, // 16016
            { 0x3ED1, 0x05E6 }, // CoconutCrab
            { 0x3ECB, 0x057F }, // Lasher
            { 0x3ED0, 0x05A1 }, // SkeletalCat
            { 0x3ED2, 0x05F6 }, // war boar
            { 0x3ECD, 0x0580 }, // Palomino
            { 0x3ECF, 0x05A0 }, // Eowmu
            { 0x3ED3, 0x05F7 }, // capybara
            { 0x3ED4, 0x060A },
            { 0x3ED5, 0x060B }, // a wolf
            { 0x3ED6, 0x060C }, // an orange dog 2?
            { 0x3ED7, 0x060D },
            { 0x3ED8, 0x060F }, // a black dog?
            { 0x3ED9, 0x0610 }, // a dobberman?
            { 0x3EDA, 0x0590 } // Frostmites Beetles
        };

        public static ushort FixMountGraphic(TileDataLoader tileData, ushort graphic)
        {
            if (graphic == 0x3E9B || graphic == 0x3E9D)
            {
                return 0x00C0;
            }

            // ethereal kirin
            if (graphic == 0x3E9C)
            {
                return 0x00BF;
            }

            var originalGraphic = graphic;
            if (_mounts.TryGetValue(graphic, out var fixedGraphic))
            {
                graphic = fixedGraphic;
            }

            if (tileData.StaticData[originalGraphic].AnimID != 0)
            {
                graphic = tileData.StaticData[originalGraphic].AnimID;
            }

            return graphic;
        }
    }
}