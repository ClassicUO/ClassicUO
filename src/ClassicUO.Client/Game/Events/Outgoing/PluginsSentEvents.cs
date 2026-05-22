// SPDX-License-Identifier: BSD-2-Clause

namespace ClassicUO.Game.Events.Outgoing
{
    /// <summary>Internal helper: notify plugins of all spells.</summary>
    internal readonly record struct ToPluginsAllSpellsSentArgs();

    /// <summary>Internal helper: notify plugins of all skills.</summary>
    internal readonly record struct ToPluginsAllSkillsSentArgs();
}
