using ClassicUO.Game.GameObjects;

namespace ClassicUO.Game.Data
{
    public static class SpellbookData
    {
        public static SpellBookType GetTypeByGraphic(Graphic graphic)
        {
            SpellBookType bookType = SpellBookType.Unknown;

            switch (graphic)
            {
                case 0x0E3B: // spellbook
                case 0x0EFA:
                    bookType = SpellBookType.Magery;

                    break;
                case 0x2252: // paladin spellbook
                    bookType = SpellBookType.Chivalry;

                    break;
                case 0x2253: // necromancer book
                    bookType = SpellBookType.Necromancy;

                    break;
                case 0x238C: // book of bushido
                    bookType = SpellBookType.Bushido;

                    break;
                case 0x23A0: // book of ninjitsu
                    bookType = SpellBookType.Ninjitsu;

                    break;
                case 0x2D50: // spell weaving book
                    bookType = SpellBookType.Spellweaving;

                    break;
            }

            return bookType;
        }

        public static int GetOffsetFromSpellbookType(SpellBookType type)
        {
            switch (type)
            {
                case SpellBookType.Magery:

                    return 1;
                case SpellBookType.Necromancy:

                    return 101;
                case SpellBookType.Chivalry:

                    return 201;
                case SpellBookType.Bushido:

                    return 401;
                case SpellBookType.Ninjitsu:

                    return 501;
                case SpellBookType.Spellweaving:

                    return 601;
                default:

                    return 1;
            }
        }

        public static void GetData(Item spellbook, out ulong field, out SpellBookType type)
        {
            type = GetTypeByGraphic(spellbook.Graphic);
            field = 0;

            if (type == SpellBookType.Unknown)
                return;
            int offset = GetOffsetFromSpellbookType(type);

            foreach (Item item in spellbook.Items)
            {
                int index = (item.Amount - offset) & 0x0000003F;
                int circle = index / 8;
                index %= 8;
                index = (3 - circle % 4 + circle / 4 * 4) * 8 + index;
                field |= (ulong) 1 << index;
            }
        }
    }
}