#region license

// Copyright (c) 2021, andreakarasho
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. All advertising materials mentioning features or use of this software
//    must display the following acknowledgement:
//    This product includes software developed by andreakarasho - https://github.com/andreakarasho
// 4. Neither the name of the copyright holder nor the
//    names of its contributors may be used to endorse or promote products
//    derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS ''AS IS'' AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

using System;
using System.Collections.Generic;
using System.Text;
using ClassicUO.Game.Managers;
using ClassicUO.Resources;
using ClassicUO.Utility;

namespace ClassicUO.Game.Data
{
    internal class SpellDefinition : IEquatable<SpellDefinition>
    {
        public static SpellDefinition EmptySpell = new SpellDefinition
        (
            "",
            0,
            0,
            "",
            0,
            0,
            0
        );

        internal static Dictionary<string, SpellDefinition> WordToTargettype = new Dictionary<string, SpellDefinition>();


        public SpellDefinition
        (
            string name,
            int index,
            int gumpIconID,
            int gumpSmallIconID,
            string powerwords,
            int manacost,
            int minskill,
            int tithingcost,
            TargetType target,
            params Reagents[] regs
        )
        {
            Name = name;
            ID = index;
            GumpIconID = gumpIconID;
            GumpIconSmallID = gumpSmallIconID;
            Regs = regs;
            ManaCost = manacost;
            MinSkill = minskill;
            PowerWords = powerwords;
            TithingCost = tithingcost;
            TargetType = target;
            AddToWatchedSpell();
        }

        public SpellDefinition
        (
            string name,
            int index,
            int gumpIconID,
            string powerwords,
            int manacost,
            int minskill,
            TargetType target,
            params Reagents[] regs
        )
        {
            Name = name;
            ID = index;
            GumpIconID = gumpIconID;
            GumpIconSmallID = gumpIconID;
            Regs = regs;
            ManaCost = manacost;
            MinSkill = minskill;
            PowerWords = powerwords;
            TithingCost = 0;
            TargetType = target;
            AddToWatchedSpell();
        }

        public SpellDefinition
        (
            string name,
            int index,
            int gumpIconID,
            string powerwords,
            TargetType target,
            params Reagents[] regs
        )
        {
            Name = name;
            ID = index;
            GumpIconID = gumpIconID;
            GumpIconSmallID = gumpIconID - 0x1298;
            Regs = regs;
            ManaCost = 0;
            MinSkill = 0;
            TithingCost = 0;
            PowerWords = powerwords;
            TargetType = target;
            AddToWatchedSpell();
        }

        public bool Equals(SpellDefinition other)
        {
            return ID.Equals(other.ID);
        }

        public readonly int GumpIconID;
        public readonly int GumpIconSmallID;
        public readonly int ID;
        public readonly int ManaCost;
        public readonly int MinSkill;

        public readonly string Name;
        public readonly string PowerWords;
        public readonly Reagents[] Regs;
        public readonly TargetType TargetType;
        public readonly int TithingCost;

        private void AddToWatchedSpell()
        {
            if (!string.IsNullOrEmpty(PowerWords))
            {
                WordToTargettype[PowerWords] = this;
            }
            else if (!string.IsNullOrEmpty(Name))
            {
                WordToTargettype[Name] = this;
            }
        }


        public string CreateReagentListString(string separator)
        {
            ValueStringBuilder sb = new ValueStringBuilder();
            {
                for (int i = 0; i < Regs.Length; i++)
                {
                    switch (Regs[i])
                    {
                        // britanian reagents
                        case Reagents.BlackPearl:
                            sb.Append(ResGeneral.BlackPearl);

                            break;

                        case Reagents.Bloodmoss:
                            sb.Append(ResGeneral.Bloodmoss);

                            break;

                        case Reagents.Garlic:
                            sb.Append(ResGeneral.Garlic);

                            break;

                        case Reagents.Ginseng:
                            sb.Append(ResGeneral.Ginseng);

                            break;

                        case Reagents.MandrakeRoot:
                            sb.Append(ResGeneral.MandrakeRoot);

                            break;

                        case Reagents.Nightshade:
                            sb.Append(ResGeneral.Nightshade);

                            break;

                        case Reagents.SulfurousAsh:
                            sb.Append(ResGeneral.SulfurousAsh);

                            break;

                        case Reagents.SpidersSilk:
                            sb.Append(ResGeneral.SpidersSilk);

                            break;

                        // pagan reagents
                        case Reagents.BatWing:
                            sb.Append(ResGeneral.BatWing);

                            break;

                        case Reagents.GraveDust:
                            sb.Append(ResGeneral.GraveDust);

                            break;

                        case Reagents.DaemonBlood:
                            sb.Append(ResGeneral.DaemonBlood);

                            break;

                        case Reagents.NoxCrystal:
                            sb.Append(ResGeneral.NoxCrystal);

                            break;

                        case Reagents.PigIron:
                            sb.Append(ResGeneral.PigIron);

                            break;

                        default:

                            if (Regs[i] < Reagents.None)
                            {
                                sb.Append(StringHelper.AddSpaceBeforeCapital(Regs[i].ToString()));
                            }

                            break;
                    }

                    if (i < Regs.Length - 1)
                    {
                        sb.Append(separator);
                    }
                }

                string ss = sb.ToString();
                sb.Dispose();
                return ss;
            }
        }

        public static SpellDefinition FullIndexGetSpell(int fullidx)
        {
            if (fullidx < 1 || fullidx > 799)
            {
                return EmptySpell;
            }

            if (fullidx < 100)
            {
                return SpellsMagery.GetSpell(fullidx);
            }

            if (fullidx < 200)
            {
                return SpellsNecromancy.GetSpell(fullidx % 100);
            }

            if (fullidx < 300)
            {
                return SpellsChivalry.GetSpell(fullidx % 100);
            }

            if (fullidx < 500)
            {
                return SpellsBushido.GetSpell(fullidx % 100);
            }

            if (fullidx < 600)
            {
                return SpellsNinjitsu.GetSpell(fullidx % 100);
            }

            if (fullidx < 678)
            {
                return SpellsSpellweaving.GetSpell(fullidx % 100);
            }

            if (fullidx < 700)
            {
                return SpellsMysticism.GetSpell((fullidx - 77) % 100);
            }

            return SpellsMastery.GetSpell(fullidx % 100);
        }

        public static void FullIndexSetModifySpell
        (
            int fullidx,
            int id,
            int iconid,
            int smalliconid,
            int minskill,
            int manacost,
            int tithing,
            string name,
            string words,
            TargetType target,
            params Reagents[] regs
        )
        {
            if (fullidx < 1 || fullidx > 799)
            {
                return;
            }

            SpellDefinition sd = FullIndexGetSpell(fullidx);

            if (sd.ID == fullidx) //we are not using an emptyspell spelldefinition
            {
                if (iconid == 0)
                {
                    iconid = sd.GumpIconID;
                }

                if (smalliconid == 0)
                {
                    smalliconid = sd.GumpIconSmallID;
                }

                if (tithing == 0)
                {
                    tithing = sd.TithingCost;
                }

                if (manacost == 0)
                {
                    manacost = sd.ManaCost;
                }

                if (minskill == 0)
                {
                    minskill = sd.MinSkill;
                }

                if (!string.IsNullOrEmpty(sd.PowerWords) && sd.PowerWords != words)
                {
                    WordToTargettype.Remove(sd.PowerWords);
                }

                if (!string.IsNullOrEmpty(sd.Name) && sd.Name != name)
                {
                    WordToTargettype.Remove(sd.Name);
                }
            }

            sd = new SpellDefinition
            (
                name,
                fullidx,
                iconid,
                smalliconid,
                words,
                manacost,
                minskill,
                tithing,
                target,
                regs
            );

            if (fullidx < 100)
            {
                SpellsMagery.SetSpell(id, in sd);
            }
            else if (fullidx < 200)
            {
                SpellsNecromancy.SetSpell(id, in sd);
            }
            else if (fullidx < 300)
            {
                SpellsChivalry.SetSpell(id, in sd);
            }
            else if (fullidx < 500)
            {
                SpellsBushido.SetSpell(id, in sd);
            }
            else if (fullidx < 600)
            {
                SpellsNinjitsu.SetSpell(id, in sd);
            }
            else if (fullidx < 678)
            {
                SpellsSpellweaving.SetSpell(id, in sd);
            }
            else if (fullidx < 700)
            {
                SpellsMysticism.SetSpell(id - 77, in sd);
            }
            else
            {
                SpellsMastery.SetSpell(id, in sd);
            }
        }
    }
}