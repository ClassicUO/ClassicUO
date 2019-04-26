#region license
//  Copyright (C) 2019 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
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
using System.Text;
using System.Collections.Generic;

using ClassicUO.Utility;
using ClassicUO.Game.Managers;

namespace ClassicUO.Game.Data
{
    internal readonly struct SpellDefinition : IEquatable<SpellDefinition>
    {
        public static SpellDefinition EmptySpell = new SpellDefinition();
        internal static Dictionary<string, TargetType> WordToTargettype = new Dictionary<string, TargetType>();

        public SpellDefinition(string name, int index, int gumpIconID, int gumpSmallIconID, string powerwords, int manacost, int minkill, int tithingcost, params Reagents[] regs)
        {
            Name = name;
            ID = index;
            GumpIconID = gumpIconID;
            GumpIconSmallID = gumpSmallIconID;
            Regs = regs;
            ManaCost = manacost;
            MinSkill = minkill;
            PowerWords = powerwords;
            TithingCost = tithingcost;
        }

        public SpellDefinition(string name, int index, int gumpIconID, string powerwords, int manacost, int minkill, params Reagents[] regs)
        {
            Name = name;
            ID = index;
            GumpIconID = gumpIconID;
            GumpIconSmallID = gumpIconID;
            Regs = regs;
            ManaCost = manacost;
            MinSkill = minkill;
            PowerWords = powerwords;
            TithingCost = 0;
        }

        public SpellDefinition(string name, int index, int gumpIconID, string powerwords, TargetType target, params Reagents[] regs)
        {
            Name = name;
            ID = index;
            GumpIconID = gumpIconID;
            GumpIconSmallID = gumpIconID - 0x1298;
            Regs = regs;
            ManaCost = 0;
            MinSkill = 0;
            TithingCost = 0;
            WordToTargettype.Add(PowerWords = powerwords, target);
        }

        public readonly string Name;
        public readonly int ID;
        public readonly int GumpIconID;
        public readonly int GumpIconSmallID;
        public readonly Reagents[] Regs;
        public readonly string PowerWords;
        public readonly int ManaCost;
        public readonly int MinSkill;
        public readonly int TithingCost;

        public string CreateReagentListString(string separator)
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < Regs.Length; i++)
            {
                switch (Regs[i])
                {
                    // britanian reagents
                    case Reagents.BlackPearl:
                        sb.Append("Black Pearl");

                        break;
                    case Reagents.Bloodmoss:
                        sb.Append("Bloodmoss");

                        break;
                    case Reagents.Garlic:
                        sb.Append("Garlic");

                        break;
                    case Reagents.Ginseng:
                        sb.Append("Ginseng");

                        break;
                    case Reagents.MandrakeRoot:
                        sb.Append("Mandrake Root");

                        break;
                    case Reagents.Nightshade:
                        sb.Append("Nightshade");

                        break;
                    case Reagents.SulfurousAsh:
                        sb.Append("Sulfurous Ash");

                        break;
                    case Reagents.SpidersSilk:
                        sb.Append("Spiders Silk");

                        break;
                    // pagan reagents
                    case Reagents.BatWing:
                        sb.Append("Bat Wing");

                        break;
                    case Reagents.GraveDust:
                        sb.Append("Grave Dust");

                        break;
                    case Reagents.DaemonBlood:
                        sb.Append("Daemon Blood");

                        break;
                    case Reagents.NoxCrystal:
                        sb.Append("Nox Crystal");

                        break;
                    case Reagents.PigIron:
                        sb.Append("Pig Iron");

                        break;
                    default:
                        if (Regs[i] < Reagents.None)
                            sb.Append(StringHelper.AddSpaceBeforeCapital(Regs[i].ToString()));
                        else
                            sb.Append("Unknown reagent");

                        break;
                }

                if (i < Regs.Length - 1)
                    sb.Append(separator);
            }

            return sb.ToString();
        }

        public bool Equals(SpellDefinition other)
        {
            return ID.Equals(other.ID);
        }
    }
}