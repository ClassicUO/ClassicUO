#region license

// Copyright (C) 2020 project dust765
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
using System;
using System.Collections.Generic;
using ClassicUO.Game;
using ClassicUO.Game.GameObjects;

namespace ClassicUO.Dust765.Managers
{
    public enum SpellCircle
    {
        First,
        Second,
        Third,
        Fourth,
        Fifth,
        Sixth,
        Seventh,
        Eighth,
        Ninth,
        Eleventh,
        Twelfth
    }

    public enum SpellAction : ushort
    {
        Unknown,
        Clumsy,
        CreateFood,
        Feeblemind,
        MiniHeal,
        MagicArrow,
        NightSight,
        ReactiveArmor,
        Weaken,
        Agility,
        Cunning,
        Cure,
        Harm,
        MagicTrap,
        MagicUntrap,
        Protection,
        Strength,
        Bless,
        Fireball,
        MagicLock,
        Poison,
        Telekinesis,
        Teleport,
        Unlock,
        WallOfStone,
        ArchCure,
        ArchProtection,
        Curse,
        FireField,
        GreaterHeal,
        Lightning,
        ManaDrain,
        Recall,
        BladeSpirits,
        DispelField,
        Incognito,
        MagicReflection,
        MindBlast,
        Paralyze,
        PoisonField,
        SummonCreature,
        Dispel,
        EnergyBolt,
        Explosion,
        Invisibility,
        Mark,
        MassCurse,
        ParalyzeField,
        Reveal,
        ChainLightning,
        EnergyField,
        Flamestrike,
        GateTravel,
        ManaVampire,
        MassDispel,
        MeteorSwarm,
        Polymorph,
        Earthquake,
        EnergyVortex,
        Ressurection,
        AirElemental,
        SummonDaemon,
        EarthElemental,
        FireElemental,
        WaterElemental = 64,
        CleansebyFire = 201,        // third circle?
        CloseWounds = 202,          // third circle?
        ConsecrateWeapon = 203,     // NO
        DispelEvil = 204,           // NO
        DivineFury = 205,           // NO
        EnemyofOne = 206,           // NO
        HolyLight = 207,            // NO
        NobleSacrifice = 208,       // NO
        RemoveCurse = 209,          // third circle?
        SacredJourney = 210,        // NO                        
        FocusAttack = 501,          // NO
        DeathStrike = 502,          // NO
        AnimalForm = 503,           // NO
        KiAttack = 504,             // NO
        SurpriseAttack = 505,       // NO
        Backstab = 506,             // NO
        Shadowjump = 507,           // NO
        MirrorImage = 508,          // NO
        HonorableExecution = 401,   // NO
        Confidence = 402,           // NO
        Evasion = 403,              // NO
        CounterAttack = 404,        // NO
        LightningStrike = 405,      // NO
        MomentumStrike = 406,       // NO
        ArcaneCircle = 601,         // NO
        GiftofRenewal = 602,        // Fourth circle?
        ImmolatingWeapon = 603,     // NO
        AttuneWeapon = 604,         // Third circle?
        Thunderstorm = 605,         // Third circle?
        NaturesFury = 606,          // Third circle?    
        SummonFey = 607,            // Third circle? 
        SummonFiend = 608,          // Third circle? 
        ReaperForm = 609,           // Third circle? 
        Wildfire = 610,             // Third circle? 
        EssenceofWind = 611,        // Third circle?     
        DryadAllure = 612,          // Third circle? 
        EtherealVoyage = 613,       // Third circle? 
        WordofDeath = 614,          // Third circle? 
        GiftofLife = 615,           // Fourth circle?
        ArcaneEmpowerment = 616,     // Fourth circle?
        AnimateDead = 101,          // Third circle? 
        BloodOath = 102,            // Third circle? 
        CorpseSkin = 103,           // Third circle? 
        CurseWeapon = 104,          // Second circle? 
        EvilOmen = 105,             // First circle? 
        HorrificBeast = 106,        // Fourth circle?     
        LichForm = 107,             // Fourth circle?  
        MindRot = 108,              // Third circle? 
        PainSpike = 109,            // Second circle? 
        PoisonStrike = 110,         // Third circle? 
        Strangle = 111,             // Third circle? 
        SummonFamiliar = 112,       // Third circle?     
        VampiricEmbrace = 113,      // Third circle?   
        VengefulSpirit = 114,       // Fourth circle?   
        Wither = 115,               // Second circle?
        WraithForm = 116,           // Third circle? 
        Exorcism = 117,             // Fourth circle? 
    }

    internal class SpellHandle
    {
        private SpellAction _value = SpellAction.Unknown;

        private DateTime _lastCreation = DateTime.Now;

        public DateTime Created => _lastCreation;

        public TimeSpan Elapsed => _lastCreation - DateTime.Now;

        public SpellAction Value
        {
            get { return _value; }
            set
            {
                _value = value;
                _lastCreation = DateTime.Now;
            }
        }
    }

    internal static class SpellManager
    {
        private static Dictionary<uint, SpellHandle> _mobiles = new Dictionary<uint, SpellHandle>();

        public static SpellAction _lastSpell = SpellAction.Unknown;
        public static SpellAction LastSpell => _lastSpell;

        public static void Load()
        {
        }

        public static void Unload()
        {
        }

        private static void ChatHandlers_OnSpellCast(Mobile mob, SpellAction value)
        {
            if (mob == World.Player)
            {
                _lastSpell = value;
                GameActions.Print($"Player casting {_lastSpell}");
            }

            SpellHandle spell = default;

            if (!_mobiles.TryGetValue(mob.Serial, out spell))
                _mobiles.Add(mob.Serial, spell = new SpellHandle());

            spell.Value = value;
        }

        public static TimeSpan GetCastDelay(SpellCircle circle)
        {
            return TimeSpan.FromSeconds(0.5 + (0.25 * (int)circle));
        }

        public static SpellCircle GetCircle(SpellAction spell)
        {
            switch (spell)
            {
                case SpellAction.Clumsy:
                case SpellAction.CreateFood:
                case SpellAction.Feeblemind:
                case SpellAction.MiniHeal:
                case SpellAction.MagicArrow:
                case SpellAction.NightSight:
                case SpellAction.ReactiveArmor:
                case SpellAction.Weaken:
                case SpellAction.EvilOmen:
                    return SpellCircle.First;

                case SpellAction.Agility:
                case SpellAction.Cunning:
                case SpellAction.Cure:
                case SpellAction.Harm:
                case SpellAction.MagicTrap:
                case SpellAction.MagicUntrap:
                case SpellAction.Protection:
                case SpellAction.Strength:
                case SpellAction.CurseWeapon:
                case SpellAction.PainSpike:
                case SpellAction.CleansebyFire:
                case SpellAction.Thunderstorm:
                    return SpellCircle.Second;

                case SpellAction.Bless:
                case SpellAction.Fireball:
                case SpellAction.MagicLock:
                case SpellAction.Poison:
                case SpellAction.Telekinesis:
                case SpellAction.Teleport:
                case SpellAction.Unlock:
                case SpellAction.WallOfStone:
                case SpellAction.CloseWounds:
                case SpellAction.NaturesFury:
                case SpellAction.SummonFey:
                case SpellAction.SummonFiend:
                case SpellAction.ReaperForm:
                case SpellAction.DryadAllure:
                case SpellAction.EtherealVoyage:
                case SpellAction.WordofDeath:
                case SpellAction.Wildfire:
                case SpellAction.EssenceofWind:
                
                    return SpellCircle.Third;

                case SpellAction.ArchCure:
                case SpellAction.ArchProtection:
                case SpellAction.Curse:
                case SpellAction.FireField:
                case SpellAction.GreaterHeal:
                case SpellAction.Lightning:
                case SpellAction.ManaDrain:
                case SpellAction.Recall:
                case SpellAction.RemoveCurse:
                case SpellAction.ArcaneCircle:
                case SpellAction.HorrificBeast:
                case SpellAction.Exorcism:
                case SpellAction.AnimateDead:
                case SpellAction.BloodOath:
                case SpellAction.CorpseSkin:
                case SpellAction.PoisonStrike:
                case SpellAction.Wither:
                case SpellAction.MindRot:
                case SpellAction.GiftofLife:
                case SpellAction.AttuneWeapon:
                    return SpellCircle.Fourth;

                case SpellAction.BladeSpirits:
                case SpellAction.DispelField:
                case SpellAction.Incognito:
                case SpellAction.MagicReflection:
                case SpellAction.MindBlast:
                case SpellAction.Paralyze:
                case SpellAction.PoisonField:
                case SpellAction.SummonCreature:
                case SpellAction.VampiricEmbrace:
                case SpellAction.Strangle:
                    return SpellCircle.Fifth;

                case SpellAction.Dispel:
                case SpellAction.EnergyBolt:
                case SpellAction.Explosion:
                case SpellAction.Invisibility:
                case SpellAction.Mark:
                case SpellAction.MassCurse:
                case SpellAction.ParalyzeField:
                case SpellAction.Reveal:
                case SpellAction.VengefulSpirit:
                case SpellAction.SummonFamiliar:
                case SpellAction.WraithForm:
                case SpellAction.LichForm:
                    return SpellCircle.Sixth;

                case SpellAction.ChainLightning:
                case SpellAction.EnergyField:
                case SpellAction.Flamestrike:
                case SpellAction.GateTravel:
                case SpellAction.ManaVampire:
                case SpellAction.MassDispel:
                case SpellAction.MeteorSwarm:
                case SpellAction.Polymorph:
                    return SpellCircle.Seventh;

                case SpellAction.Earthquake:
                case SpellAction.EnergyVortex:
                case SpellAction.Ressurection:
                case SpellAction.AirElemental:
                case SpellAction.SummonDaemon:
                case SpellAction.EarthElemental:
                case SpellAction.FireElemental:
                case SpellAction.WaterElemental:
                    return SpellCircle.Eighth;
                case SpellAction.GiftofRenewal:
                    return SpellCircle.Twelfth;
            }
            throw new InvalidOperationException();
        }

        public static SpellHandle Acquire(uint serial)
        {
            SpellHandle spell = default;

            if (!_mobiles.TryGetValue(serial, out spell))
                _mobiles.Add(serial, spell = new SpellHandle());

            return spell;
        }

    }
}