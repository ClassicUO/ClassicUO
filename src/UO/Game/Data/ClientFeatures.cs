﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ClassicUO.IO;

namespace ClassicUO.Game.Data
{
    internal class ClientFeatures
    {
        public CharacterListFlag Flags { get; private set; }

        public void SetFlags(CharacterListFlag flags)
        {
            Flags = flags;

            OnePerson = (flags & CharacterListFlag.CLF_ONE_CHARACTER_SLOT) != 0;
            PopupEnabled = (flags & CharacterListFlag.CLF_CONTEXT_MENU) != 0;
            TooltipsEnabled = (flags & CharacterListFlag.CLF_PALADIN_NECROMANCER_TOOLTIPS) != 0 && FileManager.ClientVersion >= ClientVersions.CV_308Z;
            PaperdollBooks = (flags & CharacterListFlag.CLF_PALADIN_NECROMANCER_TOOLTIPS) != 0;
        }

        public bool TooltipsEnabled { get; private set; } = true;
        public bool PopupEnabled { get; private set; }
        public bool PaperdollBooks { get; private set; }
        public bool OnePerson { get; private set; }
    }
}
