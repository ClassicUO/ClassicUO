namespace ClassicUO.Ecs;

// Ported from ClassicUO.Client/Game/Data/SpellsChivalry.cs — data only.
internal static class ChivalrySpellData
{
    public static readonly string[] Names =
    {
        "Cleanse by Fire",
        "Close Wounds",
        "Consecrate Weapon",
        "Dispel Evil",
        "Divine Fury",
        "Enemy of One",
        "Holy Light",
        "Noble Sacrifice",
        "Remove Curse",
        "Sacred Journey",
    };

    public static readonly int[] Ids =
    {
        201,
        202,
        203,
        204,
        205,
        206,
        207,
        208,
        209,
        210,
    };

    public static readonly int[] IconIds =
    {
        0x5100,
        0x5101,
        0x5102,
        0x5103,
        0x5104,
        0x5105,
        0x5106,
        0x5107,
        0x5108,
        0x5109,
    };

    public static readonly string[] PowerWords =
    {
        "Expor Flamus",
        "Obsu Vulni",
        "Consecrus Arma",
        "Dispiro Malas",
        "Divinum Furis",
        "Forul Solum",
        "Augus Luminos",
        "Dium Prostra",
        "Extermo Vomica",
        "Sanctum Viatas",
    };

    public static readonly int[] ManaCost =
    {
        10,
        10,
        10,
        10,
        10,
        20,
        20,
        20,
        20,
        20,
    };

    public static readonly int[] MinSkill =
    {
        5,
        0,
        15,
        35,
        25,
        45,
        55,
        65,
        5,
        5,
    };

    public static readonly int[] TithingCost =
    {
        10,
        10,
        10,
        10,
        10,
        10,
        10,
        30,
        10,
        10,
    };

    public static readonly string[] Reagents =
    {
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
    };
}
