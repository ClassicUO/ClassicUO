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
using ClassicUO.Game.GameObjects;

namespace ClassicUO.Game.InteropServices.Runtime.Managers
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
        Eighth
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
        WaterElemental = 64
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
                    return SpellCircle.First;

                case SpellAction.Agility:
                case SpellAction.Cunning:
                case SpellAction.Cure:
                case SpellAction.Harm:
                case SpellAction.MagicTrap:
                case SpellAction.MagicUntrap:
                case SpellAction.Protection:
                case SpellAction.Strength:
                    return SpellCircle.Second;

                case SpellAction.Bless:
                case SpellAction.Fireball:
                case SpellAction.MagicLock:
                case SpellAction.Poison:
                case SpellAction.Telekinesis:
                case SpellAction.Teleport:
                case SpellAction.Unlock:
                case SpellAction.WallOfStone:
                    return SpellCircle.Third;

                case SpellAction.ArchCure:
                case SpellAction.ArchProtection:
                case SpellAction.Curse:
                case SpellAction.FireField:
                case SpellAction.GreaterHeal:
                case SpellAction.Lightning:
                case SpellAction.ManaDrain:
                case SpellAction.Recall:
                    return SpellCircle.Fourth;

                case SpellAction.BladeSpirits:
                case SpellAction.DispelField:
                case SpellAction.Incognito:
                case SpellAction.MagicReflection:
                case SpellAction.MindBlast:
                case SpellAction.Paralyze:
                case SpellAction.PoisonField:
                case SpellAction.SummonCreature:
                    return SpellCircle.Fifth;

                case SpellAction.Dispel:
                case SpellAction.EnergyBolt:
                case SpellAction.Explosion:
                case SpellAction.Invisibility:
                case SpellAction.Mark:
                case SpellAction.MassCurse:
                case SpellAction.ParalyzeField:
                case SpellAction.Reveal:
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