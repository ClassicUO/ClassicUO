using System.Collections.Generic;

namespace ClassicUO;

internal static class Mounts
{
    private static readonly Dictionary<ushort, MountInfo> _mounts = new();

    static Mounts()
    {
        _mounts[0x3E90] = new(0x0114, 0x3E90, 0); // 16016 Reptalon
        _mounts[0x3E91] = new(0x0115, 0x3E91, 0); // 16017
        _mounts[0x3E92] = new(0x011C, 0x3E92, 0); // 16018
        _mounts[0x3E94] = new(0x00F3, 0x3E94, 0); // 16020
        _mounts[0x3E95] = new(0x00A9, 0x3E95, 0); // 16021
        _mounts[0x3E97] = new(0x00C3, 0x3E97, 0); // 16023 Ethereal Giant Beetle
        _mounts[0x3E98] = new(0x00C2, 0x3E98, 0); // 16024 Ethereal Swamp Dragon
        _mounts[0x3E9A] = new(0x00C1, 0x3E9A, 0); // 16026 Ethereal Ridgeback
        _mounts[0x3E9B] = new(0x00C0, 0x3E9B, -9); // 16027
        _mounts[0x3E9D] = new(0x00C0, 0x3E9D, -9); // 16029 Ethereal Unicorn
        _mounts[0x3E9C] = new(0x00BF, 0x3E9C, 0); // 16028 Ethereal Kirin
        _mounts[0x3E9E] = new(0x00BE, 0x3E9E, 0); // 16030
        _mounts[0x3EA0] = new(0x00E2, 0x3EA0, 0); // 16032 light grey/horse3
        _mounts[0x3EA1] = new(0x00E4, 0x3EA1, 0); // 16033 greybrown/horse4
        _mounts[0x3EA2] = new(0x00CC, 0x3EA2, 0); // 16034 dark brown/horse
        _mounts[0x3EA3] = new(0x00D2, 0x3EA3, 0); // 16035 desert ostard
        _mounts[0x3EA4] = new(0x00DA, 0x3EA4, 0); // 16036 frenzied ostard (=zostrich)
        _mounts[0x3EA5] = new(0x00DB, 0x3EA5, 0); // 16037 forest ostard
        _mounts[0x3EA6] = new(0x00DC, 0x3EA6, 0); // 16038 Llama
        _mounts[0x3EA7] = new(0x0074, 0x3EA7, 0); // 16039 Nightmare / Vortex
        _mounts[0x3EA8] = new(0x0075, 0x3EA8, 0); // 16040 Silver Steed
        _mounts[0x3EA9] = new(0x0072, 0x3EA9, 0); // 16041 Nightmare
        _mounts[0x3EAA] = new(0x0073, 0x3EAA, 0); // 16042 Ethereal Horse
        _mounts[0x3EAB] = new(0x00AA, 0x3EAB, 0); // 16043 Ethereal Llama
        _mounts[0x3EAC] = new(0x00AB, 0x3EAC, 0); // 16044 Ethereal Ostard
        _mounts[0x3EAD] = new(0x0084, 0x3EAD, 0); // 16045 Kirin
        _mounts[0x3EAF] = new(0x0078, 0x3EAF, 0); // 16047 War Horse (Blood Red)
        _mounts[0x3EB0] = new(0x0079, 0x3EB0, 0); // 16048 War Horse (Light Green)
        _mounts[0x3EB1] = new(0x0077, 0x3EB1, 0); // 16049 War Horse (Light Blue)
        _mounts[0x3EB2] = new(0x0076, 0x3EB2, 0); // 16050 War Horse (Purple)
        _mounts[0x3EB3] = new(0x0090, 0x3EB3, 0); // 16051 Sea Horse (Medium Blue)
        _mounts[0x3EB4] = new(0x007A, 0x3EB4, -9); // 16052 Unicorn
        _mounts[0x3EB5] = new(0x00B1, 0x3EB5, 0); // 16053 Nightmare
        _mounts[0x3EB6] = new(0x00B2, 0x3EB6, 0); // 16054 Nightmare 4
        _mounts[0x3EB7] = new(0x00B3, 0x3EB7, 0); // 16055 Dark Steed
        _mounts[0x3EB8] = new(0x00BC, 0x3EB8, 0); // 16056 Ridgeback
        _mounts[0x3EBA] = new(0x00BB, 0x3EBA, 0); // 16058 Ridgeback, Savage
        _mounts[0x3EBB] = new(0x0319, 0x3EBB, -9); // 16059 Skeletal Mount
        _mounts[0x3EBC] = new(0x0317, 0x3EBC, 0); // 16060 Beetle
        _mounts[0x3EBD] = new(0x031A, 0x3EBD, 0); // 16061 SwampDragon
        _mounts[0x3EBE] = new(0x031F, 0x3EBE, 0); // 16062 Armored Swamp Dragon
        _mounts[0x3EC3] = new(0x02D4, 0x3EC3, 0); // 16067 Beetle
        _mounts[0x3ECE] = new(0x059A, 0x3ECE, 0); // serpentine dragon
        _mounts[0x3EC5] = new(0x00D5, 0x3EC5, 0); // 16069
        _mounts[0x3F3A] = new(0x00D5, 0x3F3A, 0); // 16186 snow bear ???
        _mounts[0x3EC6] = new(0x01B0, 0x3EC6, 9); // 16070 Boura
        _mounts[0x3EC7] = new(0x04E6, 0x3EC7, 18); // 16071 Tiger
        _mounts[0x3EC8] = new(0x04E7, 0x3EC8, 18); // 16072 Tiger
        _mounts[0x3EC9] = new(0x042D, 0x3EC9, 3); // 16073
        _mounts[0x3ECA] = new(0x0579, 0x3ECA, 9); // tarantula
        _mounts[0x3ECC] = new(0x0582, 0x3ECC, 0); // 16016
        _mounts[0x3ED1] = new(0x05E6, 0x3ED1, 0); // CoconutCrab
        _mounts[0x3ECB] = new(0x057F, 0x3ECB, 0); // Lasher
        _mounts[0x3ED0] = new(0x05A1, 0x3ED0, 18); // SkeletalCat
        _mounts[0x3ED2] = new(0x05F6, 0x3ED2, 9); // war boar
        _mounts[0x3ECD] = new(0x0580, 0x3ECD, 0); // Palomino
        _mounts[0x3ECF] = new(0x05A0, 0x3ECF, 9); // Eowmu
        _mounts[0x3ED3] = new(0x05F7, 0x3ED3, 18); // capybara
        _mounts[0x3ED4] = new(0x060A, 0x3ED4, 0); // (no description provided)
        _mounts[0x3ED5] = new(0x060B, 0x3ED5, 0); // a wolf
        _mounts[0x3ED6] = new(0x060C, 0x3ED6, 0); // an orange dog 2?
        _mounts[0x3ED7] = new(0x060D, 0x3ED7, 0); // (no description provided)
        _mounts[0x3ED8] = new(0x060F, 0x3ED8, 0); // a black dog?
        _mounts[0x3ED9] = new(0x0610, 0x3ED9, 0); // a doberman?
        _mounts[0x3EDA] = new(0x0590, 0x3EDA, 9); // Frostmites Beetles
    }

    public static bool TryGet(ushort animId, out MountInfo mountInfo)
    {
        return _mounts.TryGetValue(animId, out mountInfo);
    }
}

internal readonly struct MountInfo
{
    public readonly ushort Graphic;
    public readonly ushort AnimationId;
    public readonly sbyte OffsetY;


    public MountInfo(ushort graphic, ushort animId, sbyte offsetY)
    {
        Graphic = graphic;
        AnimationId = animId;
        OffsetY = offsetY;
    }
}