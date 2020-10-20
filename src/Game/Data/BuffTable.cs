﻿#region license

// Copyright (C) 2020 ClassicUO Development Community on Github
// 
// This project is an alternative client for the game Ultima Online.
// The goal of this is to develop a lightweight client considering
// new technologies.
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.

#endregion

namespace ClassicUO.Game.Data
{
    internal enum BuffIconType : short
    {
        DismountPrevention = 0x3E9,
        NoRearm = 0x3EA,
        //Currently, no 0x3EB or 0x3EC
        NightSight = 0x3ED, //*
        DeathStrike,
        EvilOmen,
        HonoredDebuff,
        AchievePerfection,
        DivineFury,         //*
        EnemyOfOne,         //*
        HidingAndOrStealth, //*
        ActiveMeditation,   //*
        BloodOathCaster,    //*
        BloodOathCurse,     //*
        CorpseSkin,         //*
        Mindrot,            //*
        PainSpike,          //*
        Strangle,
        GiftOfRenewal,     //*
        AttuneWeapon,      //*
        Thunderstorm,      //*
        EssenceOfWind,     //*
        EtherealVoyage,    //*
        GiftOfLife,        //*
        ArcaneEmpowerment, //*
        MortalStrike,
        ReactiveArmor, //*
        Protection,    //*
        ArchProtection,
        MagicReflection, //*
        Incognito,       //*
        Disguised,
        AnimalForm,
        Polymorph,
        Invisibility, //*
        Paralyze,     //*
        Poison,
        Bleed,
        Clumsy,     //*
        FeebleMind, //*
        Weaken,     //*
        Curse,      //*
        MassCurse,
        Agility,  //*
        Cunning,  //*
        Strength, //*
        Bless,    //*
        Sleep,
        StoneForm,
        SpellPlague,
        Berserk,
        MassSleep,
        Fly,
        Inspire,
        Invigorate,
        Resilience,
        Perseverance,
        TribulationTarget,
        DespairTarget,
        FishPie = 0x426,
        HitLowerAttack,
        HitLowerDefense,
        DualWield,
        Block,
        DefenseMastery,
        DespairCaster,
        Healing,
        SpellFocusingBuff,
        SpellFocusingDebuff,
        RageFocusingDebuff,
        RageFocusingBuff,
        Warding,
        TribulationCaster,
        ForceArrow,
        Disarm,
        Surge,
        Feint,
        TalonStrike,
        PsychicAttack,
        ConsecrateWeapon,
        GrapesOfWrath,
        EnemyOfOneDebuff,
        HorrificBeast,
        LichForm,
        VampiricEmbrace,
        CurseWeapon,
        ReaperForm,
        ImmolatingWeapon,
        Enchant,
        HonorableExecution,
        Confidence,
        Evasion,
        CounterAttack,
        LightningStrike,
        MomentumStrike,
        OrangePetals,
        RoseOfTrinsic,
        PoisonImmunity,
        Veterinary,
        Perfection,
        Honored,
        ManaPhase,
        FanDancerFanFire,
        Rage,
        Webbing,
        MedusaStone,
        TrueFear,
        AuraOfNausea,
        HowlOfCacophony,
        GazeDespair,
        HiryuPhysicalResistance,
        RuneBeetleCorruption,
        BloodwormAnemia,
        RotwormBloodDisease,
        SkillUseDelay,
        FactionStatLoss,
        HeatOfBattleStatus,
        CriminalStatus,
        ArmorPierce,
        SplinteringEffect,
        SwingSpeedDebuff,
        WraithForm,
        CityTradeDeal = 0x466,
        HumilityDebuff = 0x467,
        Spirituality,
        Humility,
        // Skill Masteries
        Rampage,
        Stagger, // Debuff
        Toughness,
        Thrust,
        Pierce, // Debuff
        PlayingTheOdds,
        FocusedEye,
        Onslaught, // Debuff
        ElementalFury,
        ElementalFuryDebuff, // Debuff
        CalledShot,
        Knockout,
        SavingThrow,
        Conduit,
        EtherealBurst,
        MysticWeapon,
        ManaShield,
        AnticipateHit,
        Warcry,
        Shadow,
        WhiteTigerForm,
        Bodyguard,
        HeightenedSenses,
        Tolerance,
        DeathRay,
        DeathRayDebuff,
        Intuition,
        EnchantedSummoning,
        ShieldBash,
        Whispering,
        CombatTraining,
        InjectedStrikeDebuff,
        InjectedStrike,
        UnknownTomato,
        PlayingTheOddsDebuff,
        DragonTurtleDebuff,
        Boarding,
        Potency,
        ThrustDebuff,
        FistsOfFury, // 1169
        BarrabHemolymphConcentrate,
        JukariBurnPoiltice,
        KurakAmbushersEssence,
        BarakoDraftOfMight,
        UraliTranceTonic,
        SakkhraProphylaxis,
        MysticalPolymorphTotem = 1188
    }

    internal static class BuffTable
    {
        public static ushort[] Table { get; } =
        {
            0x754C,
            0x754A,
            0x0000,
            0x0000,
            0x755E,
            0x7549,
            0x7551,
            0x7556,
            0x753A,
            0x754D,
            0x754E,
            0x7565,
            0x753B,
            0x7543,
            0x7544,
            0x7546,
            0x755C,
            0x755F,
            0x7566,
            0x7554,
            0x7540,
            0x7568,
            0x754F,
            0x7550,
            0x7553,
            0x753E,
            0x755D,
            0x7563,
            0x7562,
            0x753F,
            0x7559,
            0x7557,
            0x754B,
            0x753D,
            0x7561,
            0x7558,
            0x755B,
            0x7560,
            0x7541,
            0x7545,
            0x7552,
            0x7569,
            0x7548,
            0x755A,
            0x753C,
            0x7547,
            0x7567,
            0x7542,
            0x758A,
            0x758B,
            0x758C,
            0x758D,
            0x0000,
            0x758E,
            0x094B,
            0x094C,
            0x094D,
            0x094E,
            0x094F,
            0x0950,
            0x753E,
            0x5011,
            0x7590,
            0x7591,
            0x7592,
            0x7593,
            0x7594,
            0x7595,
            0x7596,
            0x7598,
            0x7599,
            0x759B,
            0x759C,
            0x759E,
            0x759F,
            0x75A0,
            0x75A1,
            0x75A3,
            0x75A4,
            0x75A5,
            0x75A6,
            0x75A7,
            0x75C0,
            0x75C1,
            0x75C2,
            0x75C3,
            0x75C4,
            0x75F2,
            0x75F3,
            0x75F4,
            0x75F5,
            0x75F6,
            0x75F7,
            0x75F8,
            0x75F9,
            0x75FA,
            0x75FB,
            0x75FC,
            0x75FD,
            0x75FE,
            0x75FF,
            0x7600,
            0x7601,
            0x7602,
            0x7603,
            0x7604,
            0x7605,
            0x7606,
            0x7607,
            0x7608,
            0x7609,
            0x760A,
            0x760B,
            0x760C,
            0x760D,
            0x760E,
            0x760F,
            0x7610,
            0x7611,
            0x7612,
            0x7613,
            0x7614,
            0x7615,
            0x75C5,
            0x75F6,
            0x761B,
            // skill masteries
            0x9bc9,
            0x9bb5,
            0x9bdd,
            0x9bc6,
            0x9bcc,
            0x9bbe,
            0x9bbd,
            0x9bcb,
            0x9bc8,
            0x9bbf,
            0x9bcd,
            0x9bc0,
            0x9bce,
            0x9bc1,
            0x9bc7,
            0x9bc2,
            0x9bb7,
            0x9bca,
            0x9bb6,
            0x9bb8,
            0x9bb9,
            0x9bba,
            0x9bbb,
            0x9bbc,
            0x9bc3,
            0x9bc4,
            0x9bc5,
            0x9bd2,
            0x9bd3,
            0x9bd4,
            0x9bd5,
            0x9bd1,
            0x9bd6,
            0x9bd7,
            0x9bcf,
            0x9bd8,
            0x9bd9,
            0x9bdb,
            0x9bdc,
            0x9bda,
            0x9bd0,
            0x9bde,
            0x9bdf
        };
    }
}