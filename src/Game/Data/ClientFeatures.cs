#region license

//  Copyright (C) 2019 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
//      
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.

#endregion

using ClassicUO.IO;

namespace ClassicUO.Game.Data
{
    internal class ClientFeatures
    {
        public CharacterListFlags Flags { get; private set; }

        public bool TooltipsEnabled { get; private set; } = true;
        public bool PopupEnabled { get; private set; }
        public bool PaperdollBooks { get; private set; }
        public bool OnePerson { get; private set; }

        public void SetFlags(CharacterListFlags flags)
        {
            Flags = flags;

            OnePerson = (flags & CharacterListFlags.CLF_ONE_CHARACTER_SLOT) != 0;
            PopupEnabled = (flags & CharacterListFlags.CLF_CONTEXT_MENU) != 0;
            TooltipsEnabled = (flags & CharacterListFlags.CLF_PALADIN_NECROMANCER_TOOLTIPS) != 0 && FileManager.ClientVersion >= ClientVersions.CV_308Z;
            PaperdollBooks = (flags & CharacterListFlags.CLF_PALADIN_NECROMANCER_TOOLTIPS) != 0;
        }
    }
}