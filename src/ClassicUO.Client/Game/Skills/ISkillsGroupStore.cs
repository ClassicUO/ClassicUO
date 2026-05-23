// SPDX-License-Identifier: BSD-2-Clause

using System.Collections.Generic;

namespace ClassicUO.Game.Skills
{
    /// <summary>
    /// Owns the in-memory <see cref="SkillsGroup"/> collection used by a
    /// <see cref="SkillsGroupManager"/> and the XML persistence layer in
    /// <c>skillsgroups.xml</c>. Responsible for add / remove semantics
    /// (including the "cannot delete first group" message box) and for
    /// reading / writing the XML file in the active profile directory.
    /// </summary>
    internal interface ISkillsGroupStore
    {
        /// <summary>Backing list of groups. The first entry is treated as the default catch-all group when other groups are removed.</summary>
        List<SkillsGroup> Groups { get; }

        /// <summary>Append a group to the end of the list.</summary>
        void Add(SkillsGroup g);

        /// <summary>Remove a group from the list, transferring its skills to the first group. Refuses to remove the first group and shows a message box instead.</summary>
        bool Remove(SkillsGroup g);

        /// <summary>Load <c>skillsgroups.xml</c> from the active profile. Falls back to <paramref name="defaults"/> when the file is missing or malformed.</summary>
        void Load(ISkillsGroupDefaults defaults);

        /// <summary>Serialise the current group list to <c>skillsgroups.xml</c> in the active profile.</summary>
        void Save();
    }
}
