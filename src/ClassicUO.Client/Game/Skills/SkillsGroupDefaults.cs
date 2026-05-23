// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Resources;

namespace ClassicUO.Game.Skills
{
    /// <summary>
    /// Concrete <see cref="ISkillsGroupDefaults"/>. Seeds an
    /// <see cref="ISkillsGroupStore"/> from <c>skillgrp.mul</c> via the
    /// injected <see cref="ISkillsGroupMulLoader"/> when available and
    /// falls back to seven hard-coded category builders otherwise. Always
    /// sorts each produced group and persists the result through the store.
    /// </summary>
    internal sealed class SkillsGroupDefaults : ISkillsGroupDefaults
    {
        private readonly ISkillsGroupMulLoader _mulLoader;

        public SkillsGroupDefaults(ISkillsGroupMulLoader mulLoader)
        {
            _mulLoader = mulLoader;
        }

        public void MakeDefault(ISkillsGroupStore store)
        {
            store.Groups.Clear();

            if (!_mulLoader.LoadMULFile(Client.Game.UO.FileManager.GetUOFilePath("skillgrp.mul"), store))
            {
                MakeDefaultMiscellaneous(store);
                MakeDefaultCombat(store);
                MakeDefaultTradeSkills(store);
                MakeDefaultMagic(store);
                MakeDefaultWilderness(store);
                MakeDefaultThieving(store);
                MakeDefaultBard(store);
            }

            foreach (SkillsGroup g in store.Groups)
            {
                g.Sort();
            }

            store.Save();
        }

        private static void MakeDefaultMiscellaneous(ISkillsGroupStore store)
        {
            SkillsGroup g = new SkillsGroup();
            g.Name = ResGeneral.Miscellaneous;
            g.Add(4);
            g.Add(6);
            g.Add(10);
            g.Add(12);
            g.Add(19);
            g.Add(3);
            g.Add(36);

            store.Add(g);
        }

        private static void MakeDefaultCombat(ISkillsGroupStore store)
        {
            int count = Client.Game.UO.FileManager.Skills.SkillsCount;

            SkillsGroup g = new SkillsGroup();
            g.Name = ResGeneral.Combat;
            g.Add(1);
            g.Add(31);
            g.Add(42);
            g.Add(17);
            g.Add(41);
            g.Add(5);
            g.Add(40);
            g.Add(27);

            if (count > 57)
            {
                g.Add(57);
            }

            g.Add(43);

            if (count > 50)
            {
                g.Add(50);
            }

            if (count > 51)
            {
                g.Add(51);
            }

            if (count > 52)
            {
                g.Add(52);
            }

            if (count > 53)
            {
                g.Add(53);
            }

            store.Add(g);
        }

        private static void MakeDefaultTradeSkills(ISkillsGroupStore store)
        {
            SkillsGroup g = new SkillsGroup();
            g.Name = ResGeneral.TradeSkills;
            g.Add(0);
            g.Add(7);
            g.Add(8);
            g.Add(11);
            g.Add(13);
            g.Add(23);
            g.Add(44);
            g.Add(45);
            g.Add(34);
            g.Add(37);

            store.Add(g);
        }

        private static void MakeDefaultMagic(ISkillsGroupStore store)
        {
            int count = Client.Game.UO.FileManager.Skills.SkillsCount;

            SkillsGroup g = new SkillsGroup();
            g.Name = ResGeneral.Magic;
            g.Add(16);

            if (count > 56)
            {
                g.Add(56);
            }

            g.Add(25);
            g.Add(46);

            if (count > 55)
            {
                g.Add(55);
            }

            g.Add(26);

            if (count > 54)
            {
                g.Add(54);
            }

            g.Add(32);

            if (count > 49)
            {
                g.Add(49);
            }

            store.Add(g);
        }

        private static void MakeDefaultWilderness(ISkillsGroupStore store)
        {
            SkillsGroup g = new SkillsGroup();
            g.Name = ResGeneral.Wilderness;
            g.Add(2);
            g.Add(35);
            g.Add(18);
            g.Add(20);
            g.Add(38);
            g.Add(39);

            store.Add(g);
        }

        private static void MakeDefaultThieving(ISkillsGroupStore store)
        {
            SkillsGroup g = new SkillsGroup();
            g.Name = ResGeneral.Thieving;
            g.Add(14);
            g.Add(21);
            g.Add(24);
            g.Add(30);
            g.Add(48);
            g.Add(28);
            g.Add(33);
            g.Add(47);

            store.Add(g);
        }

        private static void MakeDefaultBard(ISkillsGroupStore store)
        {
            SkillsGroup g = new SkillsGroup();
            g.Name = ResGeneral.Bard;
            g.Add(15);
            g.Add(29);
            g.Add(9);
            g.Add(22);

            store.Add(g);
        }
    }
}
