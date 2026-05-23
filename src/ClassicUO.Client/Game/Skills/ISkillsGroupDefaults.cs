// SPDX-License-Identifier: BSD-2-Clause

namespace ClassicUO.Game.Skills
{
    /// <summary>
    /// Produces the default <see cref="SkillsGroup"/> set used when no
    /// profile <c>skillsgroups.xml</c> file exists. First tries to seed the
    /// store from the client's <c>skillgrp.mul</c> via
    /// <see cref="ISkillsGroupMulLoader"/>; on failure it falls back to the
    /// seven hard-coded category builders (Miscellaneous, Combat, Trade
    /// Skills, Magic, Wilderness, Thieving, Bard).
    /// </summary>
    internal interface ISkillsGroupDefaults
    {
        /// <summary>Clear <paramref name="store"/>, repopulate it with default groups (MUL file or hard-coded), sort each group, then persist the result.</summary>
        void MakeDefault(ISkillsGroupStore store);
    }
}
