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

using System;

using ClassicUO.Game.GameObjects;
using ClassicUO.Game.UI.Gumps;

namespace ClassicUO.Game.Managers
{
    [Flags]
    internal enum NameOverheadTypeAllowed
    {
        All,
        Mobiles,
        Items,
        Corpses,
        MobilesCorpses = Mobiles | Corpses
    }

    internal static class NameOverHeadManager
    {
        private static NameOverHeadHandlerGump _gump;
        public static NameOverheadTypeAllowed TypeAllowed
        {
            get { return Engine.Profile.Current.NameOverheadTypeAllowed; }
            set { Engine.Profile.Current.NameOverheadTypeAllowed = value; }
        }

        public static bool IsToggled
        {
            get { return Engine.Profile.Current.NameOverheadToggled; }
            set { Engine.Profile.Current.NameOverheadToggled = value; }
        }

        public static bool IsAllowed(Entity serial)
        {
            if (serial == null)
                return false;

            if (TypeAllowed == NameOverheadTypeAllowed.All)
                return true;

            if (serial.Serial.IsItem && TypeAllowed == NameOverheadTypeAllowed.Items)
                return true;

            if (serial.Serial.IsMobile && TypeAllowed.HasFlag(NameOverheadTypeAllowed.Mobiles))
                return true;

            if (TypeAllowed.HasFlag(NameOverheadTypeAllowed.Corpses) && serial.Serial.IsItem && World.Items.Get(serial)?.IsCorpse == true)
                return true;

            return false;
        }

        public static void Open()
        {
            if (_gump != null)
                return;

            _gump = new NameOverHeadHandlerGump();
            Engine.UI.Add(_gump);
        }

        public static void Close()
        {
            if (_gump != null)
            {
                _gump.Dispose();
                _gump = null;
            }
        }

        public static void ToggleOverheads()
        {
            IsToggled = !IsToggled;
        }
    }
}