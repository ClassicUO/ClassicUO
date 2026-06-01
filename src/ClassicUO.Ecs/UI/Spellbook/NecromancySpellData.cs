namespace ClassicUO.Ecs;

// Ported from ClassicUO.Client/Game/Data/SpellsNecromancy.cs — data only.
internal static class NecromancySpellData
{
    public static readonly string[] Names =
    {
        "Animate Dead", "Blood Oath", "Corpse Skin", "Curse Weapon", "Evil Omen",
        "Horrific Beast", "Lich Form", "Mind Rot", "Pain Spike", "Poison Strike",
        "Strangle", "Summon Familiar", "Vampiric Embrace", "Vengeful Spirit", "Wither",
        "Wraith Form", "Exorcism"
    };

    public static readonly int[] Ids =
    {
        101, 102, 103, 104, 105, 106, 107, 108, 109, 110,
        111, 112, 113, 114, 115, 116, 117
    };

    public static readonly int[] IconIds =
    {
        0x5000, 0x5001, 0x5002, 0x5003, 0x5004, 0x5005, 0x5006, 0x5007, 0x5008, 0x5009,
        0x500A, 0x500B, 0x500C, 0x500D, 0x500E, 0x500F, 0x5010
    };

    public static readonly string[] PowerWords =
    {
        "Uus Corp", "In Jux Mani Xen", "In Agle Corp Ylem", "An Sanct Gra Char", "Pas Tym An Sanct",
        "Rel Xen Vas Bal", "Rel Xen Corp Ort", "Wis An Ben", "In Sar", "In Vas Nox",
        "In Bal Nox", "Kal Xen Bal", "Rel Xen An Sanct", "Kal Xen Bal Beh", "Kal Vas An Flam",
        "Rel Xen Um", "Ort Corp Grav"
    };

    public static readonly int[] ManaCost =
    {
        23, 13, 11, 7, 11, 11, 25, 17, 5, 17,
        29, 17, 25, 41, 23, 17, 40
    };

    public static readonly int[] MinSkill =
    {
        40, 20, 20, 0, 20, 40, 70, 30, 20, 50,
        65, 30, 99, 80, 60, 20, 80
    };

    public static readonly int[] TithingCost =
    {
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0
    };

    public static readonly string[] Reagents =
    {
        "Daemon Blood, Grave Dust",
        "Daemon Blood",
        "Bat Wing, Grave Dust",
        "Pig Iron",
        "Bat Wing, Nox Crystal",
        "Bat Wing, Daemon Blood",
        "Daemon Blood, Grave Dust, Nox Crystal",
        "Bat Wing, Daemon Blood, Pig Iron",
        "Grave Dust, Pig Iron",
        "Nox Crystal",
        "Daemon Blood, Nox Crystal",
        "Bat Wing, Daemon Blood, Grave Dust",
        "Bat Wing, Nox Crystal, Pig Iron",
        "Bat Wing, Grave Dust, Pig Iron",
        "Grave Dust, Nox Crystal, Pig Iron",
        "Nox Crystal, Pig Iron",
        "Nox Crystal, Grave Dust"
    };
}
