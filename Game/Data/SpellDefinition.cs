using System.Text;
using ClassicUO.IO.Resources;

namespace ClassicUO.Game.Data
{
    public struct SpellDefinition
    {
        public static SpellDefinition EmptySpell = new SpellDefinition();

        public readonly string Name;
        public readonly int ID;
        public readonly int GumpIconID;
        public readonly int GumpIconSmallID;
        public readonly Reagents[] Regs;
        public readonly string PowerWords;
        public readonly int ManaCost;
        public readonly int MinSkill;
        public readonly int TithingCost;

        public SpellDefinition(string name, int index, int gumpIconID, string powerwords, int manacost, int minkill,
            int tithingcost, params Reagents[] regs)
        {
            Name = name;
            ID = index;
            GumpIconID = gumpIconID;
            GumpIconSmallID = gumpIconID;
            Regs = regs;
            ManaCost = manacost;
            MinSkill = minkill;
            PowerWords = powerwords;
            TithingCost = tithingcost;
        }


        public SpellDefinition(string name, int index, int gumpIconID, string powerwords, int manacost, int minkill,
            params Reagents[] regs)
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

        public SpellDefinition(string name, int index, int gumpIconID, params Reagents[] regs)
        {
            Name = name;
            ID = index;
            GumpIconID = gumpIconID;
            GumpIconSmallID = gumpIconID - 0x1298;
            Regs = regs;
            ManaCost = 0;
            MinSkill = 0;
            TithingCost = 0;
            PowerWords = string.Empty;
        }

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
                        sb.Append("Spiders' Silk");
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
                        sb.Append("Unknown reagent");
                        break;
                }

                if (i < Regs.Length - 1)
                    sb.Append(separator);
            }

            return sb.ToString();
        }
    }
}