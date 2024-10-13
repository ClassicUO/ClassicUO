#region license

// Copyright (c) 2024, andreakarasho
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
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ClassicUO.Game.Managers;
using ClassicUO.Resources;
using ClassicUO.Utility;


namespace ClassicUO.Game.Data
{
    internal class SpellDefinition : IEquatable<SpellDefinition>
    {
        public static World world = new World();
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

        public static bool TryGetSpellFromName(string spellName, out SpellDefinition spell, bool partialMatch = true)
        {
            foreach (var entry in SpellsMagery.GetAllSpells)
            {
                if (partialMatch)
                {
                    if (entry.Value.Name.ToLower().Contains(spellName.ToLower()))
                    {
                        spell = entry.Value;
                        return true;
                    }
                }
                else if (entry.Value.Name.Equals(spellName, StringComparison.InvariantCultureIgnoreCase))
                {
                    spell = entry.Value;
                    return true;
                }
            }

            foreach (var entry in SpellsNecromancy.GetAllSpells)
            {
                if (partialMatch)
                {
                    if (entry.Value.Name.ToLower().Contains(spellName.ToLower()))
                    {
                        spell = entry.Value;
                        return true;
                    }
                }
                else if (entry.Value.Name.Equals(spellName, StringComparison.InvariantCultureIgnoreCase))
                {
                    spell = entry.Value;
                    return true;
                }
            }

            foreach (var entry in SpellsChivalry.GetAllSpells)
            {
                if (partialMatch)
                {
                    if (entry.Value.Name.ToLower().Contains(spellName.ToLower()))
                    {
                        spell = entry.Value;
                        return true;
                    }
                }
                else if (entry.Value.Name.Equals(spellName, StringComparison.InvariantCultureIgnoreCase))
                {
                    spell = entry.Value;
                    return true;
                }
            }

            foreach (var entry in SpellsBushido.GetAllSpells)
            {
                if (partialMatch)
                {
                    if (entry.Value.Name.ToLower().Contains(spellName.ToLower()))
                    {
                        spell = entry.Value;
                        return true;
                    }
                }
                else if (entry.Value.Name.Equals(spellName, StringComparison.InvariantCultureIgnoreCase))
                {
                    spell = entry.Value;
                    return true;
                }
            }

            foreach (var entry in SpellsNinjitsu.GetAllSpells)
            {
                if (partialMatch)
                {
                    if (entry.Value.Name.ToLower().Contains(spellName.ToLower()))
                    {
                        spell = entry.Value;
                        return true;
                    }
                }
                else if (entry.Value.Name.Equals(spellName, StringComparison.InvariantCultureIgnoreCase))
                {
                    spell = entry.Value;
                    return true;
                }
            }

            foreach (var entry in SpellsSpellweaving.GetAllSpells)
            {
                if (partialMatch)
                {
                    if (entry.Value.Name.ToLower().Contains(spellName.ToLower()))
                    {
                        spell = entry.Value;
                        return true;
                    }
                }
                else if (entry.Value.Name.Equals(spellName, StringComparison.InvariantCultureIgnoreCase))
                {
                    spell = entry.Value;
                    return true;
                }
            }

            foreach (var entry in SpellsMysticism.GetAllSpells)
            {
                if (partialMatch)
                {
                    if (entry.Value.Name.ToLower().Contains(spellName.ToLower()))
                    {
                        spell = entry.Value;
                        return true;
                    }
                }
                else if (entry.Value.Name.Equals(spellName, StringComparison.InvariantCultureIgnoreCase))
                {
                    spell = entry.Value;
                    return true;
                }
            }

            foreach (var entry in SpellsMastery.GetAllSpells)
            {
                if (partialMatch)
                {
                    if (entry.Value.Name.ToLower().Contains(spellName.ToLower()))
                    {
                        spell = entry.Value;
                        return true;
                    }
                }
                else if (entry.Value.Name.Equals(spellName, StringComparison.InvariantCultureIgnoreCase))
                {
                    spell = entry.Value;
                    return true;
                }
            }

            spell = null;
            return false;
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

        public static SpellDefinition[] GetAllSpells()
        {
            return
            [
                .. SpellsMagery.GetAllSpells.Values,
                .. SpellsNecromancy.GetAllSpells.Values,
                .. SpellsChivalry.GetAllSpells.Values,
                .. SpellsBushido.GetAllSpells.Values,
                .. SpellsNinjitsu.GetAllSpells.Values,
                .. SpellsSpellweaving.GetAllSpells.Values,
                .. SpellsMysticism.GetAllSpells.Values,
                .. SpellsMastery.GetAllSpells.Values,
            ];
        }

        public static void SaveAllSpellsToJson()
        {
            List<SpellJson> list = new List<SpellJson>();

            foreach (SpellDefinition spell in GetAllSpells())
            {
                if (spell.ID < 1 || spell.ID > 799)
                {
                    continue;
                }

                SpellJson spellJson = new SpellJson()
                {
                    SpellName = spell.Name,
                    PowerWords = spell.PowerWords,
                    GumpIcon = spell.GumpIconID,
                    SmallGumpIcon = spell.GumpIconSmallID,
                    ManaCost = spell.ManaCost,
                    MinSkill = spell.MinSkill,
                    TithingCost = spell.TithingCost,
                    TargetType = spell.TargetType,
                    AllReagents = spell.Regs

                };

                if (spell.ID < 100)
                {
                    spellJson.School = "Magery";
                    spellJson.SpellID = spell.ID;
                }
                else if (spell.ID < 200)
                {
                    spellJson.School = "Necromancy";
                    spellJson.SpellID = spell.ID - 100;
                    spellJson.SpellOffset = 100;

                }
                else if (spell.ID < 300)
                {
                    spellJson.School = "Chivalry";
                    spellJson.SpellID = spell.ID - 200;
                    spellJson.SpellOffset = 200;
                }
                else if (spell.ID < 500)
                {
                    spellJson.School = "Bushido";
                    spellJson.SpellID = spell.ID - 400;
                    spellJson.SpellOffset = 400;
                }
                else if (spell.ID < 600)
                {
                    spellJson.School = "Ninjitsu";
                    spellJson.SpellID = spell.ID - 500;
                    spellJson.SpellOffset = 500;
                }
                else if (spell.ID < 678)
                {
                    spellJson.School = "Spellweaving";
                    spellJson.SpellID = spell.ID - 600;
                    spellJson.SpellOffset = 600;
                }
                else if (spell.ID < 700)
                {
                    spellJson.School = "Mysticism";
                    spellJson.SpellID = spell.ID - 600;
                    spellJson.SpellOffset = 600;
                }
                else if (spell.ID < 800)
                {
                    spellJson.School = "Mastery";
                    spellJson.SpellID = spell.ID - 700;
                    spellJson.SpellOffset = 700;
                }

                list.Add(spellJson);
            }

            if (!SaveJsonFile(list, Path.Combine(CUOEnviroment.ExecutablePath, "Data", "spelldef.json")))
            {
                GameActions.Print(world, "Failed to save all spells as a json file!", 32);
            }
            else
            {
                GameActions.Print(world, $"Saved all spells as a json file at {Path.Combine(CUOEnviroment.ExecutablePath, "Data", "spelldef.json")}");
            }
        }

        public static bool SaveJsonFile<T>(T obj, string path, bool prettified = true)
        {
            try
            {
                string output = JsonSerializer.Serialize(obj, new JsonSerializerOptions() { WriteIndented = prettified });
                File.WriteAllText(path, output);
                return true;
            }
            catch (Exception e) { Console.WriteLine(e.ToString()); }

            return false;
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


    public class SpellJson
    {
        public string School { get; set; } = "Magery";

        public int SpellID { get; set; } = 0;
        public int SpellOffset { get; set; } = 0;
        public string SpellName { get; set; } = "";
        public string PowerWords { get; set; } = "";
        public int GumpIcon { get; set; } = 0x5000;
        public int SmallGumpIcon { get; set; } = 0x5000;
        public int ManaCost { get; set; } = 0;
        public int MinSkill { get; set; } = 0;
        public int TithingCost { get; set; } = 0;
        public TargetType TargetType { get; set; } = TargetType.Neutral;
        internal Reagents[] AllReagents { get; set; } = { };

        [JsonIgnore]
        public int SpellIndex => SpellID + SpellOffset;
    }
}