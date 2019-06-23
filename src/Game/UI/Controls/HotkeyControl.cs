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

using System.Collections.Generic;

using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Gumps;

namespace ClassicUO.Game.UI.Controls
{
    internal class HotkeyControl : Control
    {
        private readonly List<HotkeyBox> _hotkesBoxes = new List<HotkeyBox>();
        private readonly HotkeyAction _key;

        public HotkeyControl(string text, HotkeyAction key)
        {
            _key = key;
            CanMove = true;
            AcceptMouseInput = true;

            Add(new Label(text, true, 0, 150, 1));

            AddNew(key);
        }


        public void AddNew(HotkeyAction action)
        {
            HotkeyBox box = new HotkeyBox
            {
                X = 150
            };

            box.HotkeyChanged += (sender, e) =>
            {
                GameScene gs = Engine.SceneManager.GetScene<GameScene>();

                if (gs == null)
                    return;

                if (gs.Hotkeys.Bind(_key, box.Key, box.Mod))
                {
                }
                else // show a popup
                    Engine.UI.Add(new MessageBoxGump(400, 200, "Key combination already exists.", null));
            };

            box.HotkeyCancelled += (sender, e) =>
            {
                GameScene gs = Engine.SceneManager.GetScene<GameScene>();

                if (gs == null)
                    return;

                gs.Hotkeys.UnBind(_key);
            };

            if (_hotkesBoxes.Count != 0) box.Y = _hotkesBoxes[_hotkesBoxes.Count - 1].Bounds.Bottom;


            _hotkesBoxes.Add(box);

            Add(box);
        }
    }
}