// SPDX-License-Identifier: BSD-2-Clause

namespace ClassicUO.Game.Skills
{
    /// <summary>
    /// Parses the legacy UO <c>skillgrp.mul</c> file (both 8-bit and unicode
    /// variants) into <see cref="SkillsGroup"/> entries appended to a
    /// target <see cref="ISkillsGroupStore"/>. Used by
    /// <see cref="ISkillsGroupDefaults"/> as the preferred source for the
    /// initial group layout when the player has no <c>skillsgroups.xml</c>.
    /// </summary>
    internal interface ISkillsGroupMulLoader
    {
        /// <summary>Parse <paramref name="path"/> and append any decoded groups to <paramref name="store"/>. Returns <c>true</c> when at least one group was added.</summary>
        bool LoadMULFile(string path, ISkillsGroupStore store);
    }
}
