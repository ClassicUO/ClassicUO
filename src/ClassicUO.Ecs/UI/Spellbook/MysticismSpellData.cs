namespace ClassicUO.Ecs;

// Ported from ClassicUO.Client/Game/Data/SpellsMysticism.cs — data only.
internal static class MysticismSpellData
{
    public static readonly string[] Names =
    {
        "Nether Bolt",
        "Healing Stone",
        "Purge Magic",
        "Enchant",
        "Sleep",
        "Eagle Strike",
        "Animated Weapon",
        "Stone Form",
        "Spell Trigger",
        "Mass Sleep",
        "Cleansing Winds",
        "Bombard",
        "Spell Plague",
        "Hail Storm",
        "Nether Cyclone",
        "Rising Colossus",
    };

    public static readonly int[] Ids =
    {
        678,
        679,
        680,
        681,
        682,
        683,
        684,
        685,
        686,
        687,
        688,
        689,
        690,
        691,
        692,
        693,
    };

    public static readonly int[] IconIds =
    {
        0x5DC0,
        0x5DC1,
        0x5DC2,
        0x5DC3,
        0x5DC4,
        0x5DC5,
        0x5DC6,
        0x5DC7,
        0x5DC8,
        0x5DC9,
        0x5DCA,
        0x5DCB,
        0x5DCC,
        0x5DCD,
        0x5DCE,
        0x5DCF,
    };

    public static readonly string[] PowerWords =
    {
        "In Corp Ylem",
        "Kal In Mani",
        "An Ort Sanct",
        "In Ort Ylem",
        "In Zu",
        "Kal Por Xen",
        "In Jux Por Ylem",
        "In Rel Ylem",
        "In Vas Ort Ex",
        "Vas Zu",
        "In Vas Mani Hur",
        "Corp Por Ylem",
        "Vas Rel Jux Ort",
        "Kal Des Ylem",
        "Grav Hur",
        "Kal Vas Xen Corp Ylem",
    };

    public static readonly int[] ManaCost =
    {
        4,
        4,
        6,
        6,
        8,
        9,
        11,
        11,
        14,
        14,
        20,
        20,
        40,
        50,
        50,
        50,
    };

    public static readonly int[] MinSkill =
    {
        0,
        0,
        8,
        8,
        20,
        20,
        33,
        33,
        45,
        45,
        58,
        58,
        70,
        70,
        83,
        83,
    };

    public static readonly int[] TithingCost =
    {
        0,
        0,
        0,
        0,
        0,
        0,
        0,
        0,
        0,
        0,
        0,
        0,
        0,
        0,
        0,
        0,
    };

    public static readonly string[] Reagents =
    {
        "Black Pearl, Sulfurous Ash",
        "Bone, Garlic, Ginseng, Spider's Silk",
        "Fertile Dirt, Garlic, Mandrake Root, Sulfurous Ash",
        "Spider's Silk, Mandrake Root, Sulfurous Ash",
        "Nightshade, Spider's Silk, Black Pearl",
        "Blood Moss, Bone, Mandrake Root, Spider's Silk",
        "Bone, Black Pearl, Mandrake Root, Nightshade",
        "Blood Moss, Fertile Dirt, Garlic",
        "Dragons Blood, Garlic, Mandrake Root, Spider's Silk",
        "Ginseng, Nightshade, Spider's Silk",
        "Dragons Blood, Garlic, Ginseng, Mandrake Root",
        "Blood Moss, Dragons Blood, Garlic, Sulfurous Ash",
        "Demon Bone, Dragons Blood, Nightshade, Sulfurous Ash",
        "Dragons Blood, Black Pearl, Blood Moss, Mandrake Root",
        "Mandrake Root, Nightshade, Sulfurous Ash, Blood Moss",
        "Demon Bone, Dragons Blood, Fertile Dirt, Nightshade",
    };
}
