// SPDX-License-Identifier: BSD-2-Clause

namespace ClassicUO.Game.Data
{
    /// <summary>
    /// Named constants for commonly repeated graphic / body IDs used throughout the client.
    /// All values match the original Ultima Online client data.
    /// </summary>
    public static class GraphicConstants
    {
        // ── Human body IDs ──────────────────────────────────────────────
        public const ushort HumanMaleBody = 0x0190;
        public const ushort HumanFemaleBody = 0x0191;
        public const ushort HumanMaleDeadBody = 0x0192;
        public const ushort HumanFemaleDeadBody = 0x0193;

        // Pre-SA human range (alternate, e.g. KR)
        public const ushort HumanKRMaleMin = 0x00B7;
        public const ushort HumanKRMaleMax = 0x00BA;

        // ── Elf body IDs ────────────────────────────────────────────────
        public const ushort ElfMaleBody = 0x025D;
        public const ushort ElfFemaleBody = 0x025E;
        public const ushort ElfMaleDeadBody = 0x025F;
        public const ushort ElfFemaleDeadBody = 0x0260;

        // ── Gargoyle body IDs ───────────────────────────────────────────
        public const ushort GargoyleMaleBody = 0x029A;
        public const ushort GargoyleFemaleBody = 0x029B;
        public const ushort GargoyleMaleGhost = 0x02B6;
        public const ushort GargoyleFemaleGhost = 0x02B7;

        // ── GM / special bodies ─────────────────────────────────────────
        public const ushort GMBody = 0x03DB;
        public const ushort CounselorBody = 0x03DF;
        public const ushort SeerBody = 0x03E2;
        public const ushort HumanBodyExtra1 = 0x02E8;
        public const ushort HumanBodyExtra2 = 0x02E9;
        public const ushort HumanBodyExtra3 = 0x04E5;

        // ── Corpse / container ──────────────────────────────────────────
        public const ushort CorpseGraphic = 0x2006;
        public const ushort HighlightBankBox = 0x0EB0;
        public const ushort BookOfBulkOrders = 0x2AF8;

        // ── Coin graphics ───────────────────────────────────────────────
        public const ushort GoldCoin = 0x0EEA;
        public const ushort SilverCoin = 0x0EED;
        public const ushort CopperCoin = 0x0EF0;

        // ── Mount / riding ──────────────────────────────────────────────
        public const ushort MountInternalGraphic = 0x3E96;
        public const ushort SeaHorseMount = 0x3EB3;
        public const ushort InvalidGraphic = 0xFFFF;

        // ── Hue constants ───────────────────────────────────────────────
        public const ushort HiddenHue = 0x038E;
        public const ushort InvisibleHue = 0x0386;
        public const ushort TransparentHue = 0x0034;
        public const ushort PoisonFieldHue = 0x0020;
        public const ushort ParalyzeFieldHue = 0x0058;
        public const ushort EnergyFieldHue = 0x0070;
        public const ushort FireFieldHue = 0x0044;
        public const ushort WildFireHue = 0x038A;
        public const ushort UnknownFieldHue = 0x0035;

        // ── Multi / ship ────────────────────────────────────────────────
        public const ushort SpellbookItem = 0x1E5E;

        // ── Container gump IDs ──────────────────────────────────────────
        public const ushort ContainerGumpSpellbook = 0x091A;
        public const ushort ContainerGumpKRStorage = 0x092E;

        // ── Clothing / paperdoll layer checks ───────────────────────────
        public const ushort PantsKilt = 0x1411;
        public const ushort ShortPantsMale = 0x0513;
        public const ushort ShortPantsFemale = 0x0514;
        public const ushort FemaleRobe = 0x0504;
        public const ushort LongPants = 0x01EB;
        public const ushort ElvenPants = 0x03E5;
        public const ushort ElvenPants2 = 0x03EB;
        public const ushort SkirtA = 0x01C7;
        public const ushort SkirtB = 0x01E4;
        public const ushort Doublet = 0x0238;
        public const ushort GargishRobe = 0x0229;
        public const ushort CloakRangeMin = 0x04E7;
        public const ushort CloakRangeMax = 0x04EB;
        public const ushort TunicA = 0x1541;
        public const ushort TunicB = 0x1542;
        public const ushort TorsoPlateA = 0x782A;
        public const ushort TorsoPlateB = 0x782B;

        // ── Special robe overrides ──────────────────────────────────────
        public const ushort RobeOverrideA = 0x9985;
        public const ushort RobeOverrideB = 0x9986;
        public const ushort RobeOverrideC = 0xA412;
        public const ushort RobeOverrideD = 0xA2CA;
        public const ushort RobeSpecialA = 0x4B9D;
        public const ushort RobeSpecialB = 0x7816;
        public const ushort RobeCheckMax = 0x2687;
        public const ushort RobeCheckMin = 0x2683;
        public const ushort RobeRangeA_Min = 0x204E;
        public const ushort RobeRangeA_Max = 0x204F;
        public const ushort RobeSpecialC = 0x2FB9;
        public const ushort RobeSpecialD = 0x3173;

        // ── Light-source item ranges ────────────────────────────────────
        // (see LightColors.cs for full mapping)
        public const ushort CandleRangeMin = 0x09FB;
        public const ushort CandleRangeMax = 0x0A14;
        public const ushort CandleRangeMin2 = 0x0A15;
        public const ushort CandleRangeMax2 = 0x0A29;
        public const ushort ForgeRangeMin = 0x0B1A;
        public const ushort ForgeRangeMax = 0x0B1F;
        public const ushort ForgeRangeMin2 = 0x0B20;
        public const ushort ForgeRangeMax2 = 0x0B25;

        // ── Fire / lava animated tile ranges (ItemView.cs) ──────────────
        public const ushort FireBowlRangeMin = 0x3E02;
        public const ushort FireBowlRangeMax = 0x3E0B;
        public const ushort LavaTileRangeMin = 0x3914;
        public const ushort LavaTileRangeMax = 0x3929;

        // ── Gargoyle body IDs used in decimal in some places ────────────
        // Graphic == 666 is GargoyleMaleBody (0x029A) and 667 is GargoyleFemaleBody (0x029B)
        // provided here for reference only; prefer the hex constants above.
    }
}
