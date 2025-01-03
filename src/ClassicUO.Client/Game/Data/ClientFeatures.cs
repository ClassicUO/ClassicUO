// SPDX-License-Identifier: BSD-2-Clause

using System;
using ClassicUO.Utility;

namespace ClassicUO.Game.Data
{
    [Flags]
    internal enum CharacterListFlags : uint
    {
        CLF_UNKNOWN = 0x01,
        CLF_OWERWRITE_CONFIGURATION_BUTTON = 0x02,
        CLF_ONE_CHARACTER_SLOT = 0x04,
        CLF_CONTEXT_MENU = 0x08,
        CLF_LIMIT_CHARACTER_SLOTS = 0x10,
        CLF_PALADIN_NECROMANCER_TOOLTIPS = 0x20,
        CLF_6_CHARACTER_SLOT = 0x40,
        CLF_SAMURAI_NINJA = 0x80,
        CLF_ELVEN_RACE = 0x100,
        CLF_UNKNOWN_1 = 0x200,
        CLF_UO3D = 0x400,
        CLF_UNKNOWN_2 = 0x800,
        CLF_7_CHARACTER_SLOT = 0x1000,
        CLF_UNKNOWN_3 = 0x2000,
        CLF_NEW_MOVEMENT_SYSTEM = 0x4000,
        CLF_UNLOCK_FELUCCA_AREAS = 0x8000
    }

    internal class ClientFeatures
    {
        public CharacterListFlags Flags { get; private set; }

        public bool TooltipsEnabled { get; private set; } = true;
        public bool PopupEnabled { get; private set; }
        public bool PaperdollBooks { get; private set; }
        public uint MaxChars { get; private set; } = 5;

        public void SetFlags(CharacterListFlags flags)
        {
            Flags = flags;

            if ((flags & CharacterListFlags.CLF_ONE_CHARACTER_SLOT) != 0)
            {
                MaxChars = 1;
            }
            else if ((flags & CharacterListFlags.CLF_7_CHARACTER_SLOT) != 0)
            {
                MaxChars = 7;
            }
            else if ((flags & CharacterListFlags.CLF_6_CHARACTER_SLOT) != 0)
            {
                MaxChars = 6;
            }

            PopupEnabled = (flags & CharacterListFlags.CLF_CONTEXT_MENU) != 0;

            TooltipsEnabled = (flags & CharacterListFlags.CLF_PALADIN_NECROMANCER_TOOLTIPS) != 0 && Client.Game.UO.Version >= ClientVersion.CV_308Z;

            PaperdollBooks = (flags & CharacterListFlags.CLF_PALADIN_NECROMANCER_TOOLTIPS) != 0;
        }
    }
}