// SPDX-License-Identifier: BSD-2-Clause

using Microsoft.Xna.Framework;

namespace ClassicUO.Configuration
{
    // ECS per-character profile. Read by gump plugins via Res<Profile>. Field
    // names + defaults mirror the legacy Profile (src/ClassicUO.Client/
    // Configuration/Profile.cs) so a future profile.json load can deserialize
    // straight in. Until that load is wired, every field starts at the legacy
    // default — which is exactly what the gumps assume for v1.
    //
    // NOTE: UseUOPGumps is intentionally absent — in the legacy client it is a
    // property of the Gumps asset loader (FileManager.Gumps.UseUOPGumps), not a
    // profile flag. Status-gump layout reads it from the asset side, not here.
    internal sealed class Profile
    {
        // --- window geometry (pre-existing) ---
        public Point GameWindowPosition { get; set; } = new Point(10, 10);
        public Point GameWindowSize { get; set; } = new Point(600, 480);
        public bool GameWindowFullSize { get; set; }

        // --- status gump / health bar ---
        public bool UseOldStatusGump { get; set; }                       // false => modern 0x2A6C layout
        public bool StatusGumpBarMutuallyExclusive { get; set; } = true; // opening one disposes the other
        public int CloseHealthBarType { get; set; }                      // 0=none, 1=not-exists, 2=is-dead
        public bool CustomBarsToggled { get; set; }                      // false => classic sprite bars

        // --- buff gump ---
        public bool BuffBarTime { get; set; }                            // countdown text over buff icons

        // --- macro / counter bar ---
        public bool CastSpellsByOneClick { get; set; }                   // single-click cast vs double-click

        // --- counter bar ---
        public bool CounterBarHighlightOnChange { get; set; } = true;
        public bool CounterBarHighlightOnAmount { get; set; }
        public bool CounterBarDisplayAbbreviatedAmount { get; set; }
        public int CounterBarAbbreviatedAmount { get; set; } = 1000;
        public int CounterBarHighlightAmount { get; set; } = 5;
        public int CounterBarCellSize { get; set; } = 40;

        // --- party / shop / grid loot ---
        public bool PartyInviteGump { get; set; } = true;
        public int VendorGumpHeight { get; set; } = 60;
        public int GridLootType { get; set; }                            // 0=none, 1=grid only, 2=both
    }
}
