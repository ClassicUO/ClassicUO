// SPDX-License-Identifier: BSD-2-Clause

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
            // Handle Vystia custom spells (1000-1415)
            if (fullidx >= 1000)
            {
                if (fullidx < 1416)
                {
                    if (fullidx < 1032) return SpellsVystiaIceMagic.GetSpell(fullidx - 999);       // 1000-1031 → 1-32 (client dict keys are 1-based)
                    if (fullidx < 1064) return SpellsVystiaNature.GetSpell(fullidx - 1031);       // 1032-1063 → 1-32 (client dict keys are 1-based)
                    if (fullidx < 1096) return SpellsVystiaHex.GetSpell(fullidx - 1063);           // 1064-1095 → 1-32 (client dict keys are 1-based)
                    if (fullidx < 1128) return SpellsVystiaElemental.GetSpell(fullidx - 1095);    // 1096-1127 → 1-32 (client dict keys are 1-based)
                    if (fullidx < 1160) return SpellsVystiaDark.GetSpell(fullidx - 1127);         // 1128-1159 → 1-32 (client dict keys are 1-based)
                    if (fullidx < 1192) return SpellsVystiaDivination.GetSpell(fullidx - 1159);    // 1160-1191 → 1-32 (client dict keys are 1-based)
                    if (fullidx < 1224) return SpellsVystiaNecromancy.GetSpell(fullidx - 1191);    // 1192-1223 → 1-32 (client dict keys are 1-based)
                    if (fullidx < 1256) return SpellsVystiaSummoning.GetSpell(fullidx - 1223);     // 1224-1255 → 1-32 (client dict keys are 1-based)
                    if (fullidx < 1288) return SpellsVystiaShamanic.GetSpell(fullidx - 1255);      // 1256-1287 → 1-32 (client dict keys are 1-based)
                    if (fullidx < 1320) return SpellsVystiaBardic.GetSpell(fullidx - 1287);         // 1288-1319 → 1-32 (client dict keys are 1-based)
                    if (fullidx < 1352) return SpellsVystiaEnchanting.GetSpell(fullidx - 1319);    // 1320-1351 → 1-32 (client dict keys are 1-based)
                    if (fullidx < 1384) return SpellsVystiaIllusion.GetSpell(fullidx - 1351);      // 1352-1383 → 1-32 (client dict keys are 1-based)
                    if (fullidx < 1416) return SpellsVystiaSongweaving.GetSpell(fullidx - 1383);   // 1384-1415 → 1-32 (client dict keys are 1-based)
                }
                return EmptySpell;
            }

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
            // Handle Vystia custom spells (1000-1415)
            if (fullidx >= 1000 && fullidx < 1416)
            {
                SpellDefinition sd = FullIndexGetSpell(fullidx);

                if (sd.ID == fullidx)
                {
                    if (iconid == 0) iconid = sd.GumpIconID;
                    if (smalliconid == 0) smalliconid = sd.GumpIconSmallID;
                    if (tithing == 0) tithing = sd.TithingCost;
                    if (manacost == 0) manacost = sd.ManaCost;
                    if (minskill == 0) minskill = sd.MinSkill;

                    if (!string.IsNullOrEmpty(sd.PowerWords) && sd.PowerWords != words)
                        WordToTargettype.Remove(sd.PowerWords);
                    if (!string.IsNullOrEmpty(sd.Name) && sd.Name != name)
                        WordToTargettype.Remove(sd.Name);
                }

                sd = new SpellDefinition(name, fullidx, iconid, smalliconid, words, manacost, minskill, tithing, target, regs);

                if (fullidx < 1032)
                    SpellsVystiaIceMagic.SetSpell(id, in sd);
                else if (fullidx < 1064)
                    SpellsVystiaNature.SetSpell(id, in sd);
                else if (fullidx < 1096)
                    SpellsVystiaHex.SetSpell(id, in sd);
                else if (fullidx < 1128)
                    SpellsVystiaElemental.SetSpell(id, in sd);
                else if (fullidx < 1160)
                    SpellsVystiaDark.SetSpell(id, in sd);
                else if (fullidx < 1192)
                    SpellsVystiaDivination.SetSpell(id, in sd);
                else if (fullidx < 1224)
                    SpellsVystiaNecromancy.SetSpell(id, in sd);
                else if (fullidx < 1256)
                    SpellsVystiaSummoning.SetSpell(id, in sd);
                else if (fullidx < 1288)
                    SpellsVystiaShamanic.SetSpell(id, in sd);
                else if (fullidx < 1320)
                    SpellsVystiaBardic.SetSpell(id, in sd);
                else if (fullidx < 1352)
                    SpellsVystiaEnchanting.SetSpell(id, in sd);
                else if (fullidx < 1384)
                    SpellsVystiaIllusion.SetSpell(id, in sd);
                else if (fullidx < 1416)
                    SpellsVystiaSongweaving.SetSpell(id, in sd);

                return;
            }

            if (fullidx < 1 || fullidx > 799)
            {
                return;
            }

            SpellDefinition sd2 = FullIndexGetSpell(fullidx);

            if (sd2.ID == fullidx) //we are not using an emptyspell spelldefinition
            {
                if (iconid == 0)
                {
                    iconid = sd2.GumpIconID;
                }

                if (smalliconid == 0)
                {
                    smalliconid = sd2.GumpIconSmallID;
                }

                if (tithing == 0)
                {
                    tithing = sd2.TithingCost;
                }

                if (manacost == 0)
                {
                    manacost = sd2.ManaCost;
                }

                if (minskill == 0)
                {
                    minskill = sd2.MinSkill;
                }

                if (!string.IsNullOrEmpty(sd2.PowerWords) && sd2.PowerWords != words)
                {
                    WordToTargettype.Remove(sd2.PowerWords);
                }

                if (!string.IsNullOrEmpty(sd2.Name) && sd2.Name != name)
                {
                    WordToTargettype.Remove(sd2.Name);
                }
            }

            sd2 = new SpellDefinition
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
                SpellsMagery.SetSpell(id, in sd2);
            }
            else if (fullidx < 200)
            {
                SpellsNecromancy.SetSpell(id, in sd2);
            }
            else if (fullidx < 300)
            {
                SpellsChivalry.SetSpell(id, in sd2);
            }
            else if (fullidx < 500)
            {
                SpellsBushido.SetSpell(id, in sd2);
            }
            else if (fullidx < 600)
            {
                SpellsNinjitsu.SetSpell(id, in sd2);
            }
            else if (fullidx < 678)
            {
                SpellsSpellweaving.SetSpell(id, in sd2);
            }
            else if (fullidx < 700)
            {
                SpellsMysticism.SetSpell(id - 77, in sd2);
            }
            else
            {
                SpellsMastery.SetSpell(id, in sd2);
            }
        }
    }
}
