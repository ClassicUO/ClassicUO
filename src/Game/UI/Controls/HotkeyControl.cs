#region license

// Copyright (c) 2021, andreakarasho
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. All advertising materials mentioning features or use of this software
//    must display the following acknowledgement:
//    This product includes software developed by andreakarasho - https://github.com/andreakarasho
// 4. Neither the name of the copyright holder nor the
//    names of its contributors may be used to endorse or promote products
//    derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS ''AS IS'' AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

using System.Collections.Generic;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Resources;

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

            Add
            (
                new Label
                (
                    text,
                    true,
                    0,
                    150,
                    1
                )
            );

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
                GameScene gs = Client.Game.GetScene<GameScene>();

                if (gs == null)
                {
                    return;
                }

                if (gs.Hotkeys.Bind(_key, box.Key, box.Mod))
                {
                }
                else // show a popup
                {
                    UIManager.Add(new MessageBoxGump(400, 200, ResGumps.KeyCombinationAlreadyExists, null));
                }
            };

            box.HotkeyCancelled += (sender, e) =>
            {
                GameScene gs = Client.Game.GetScene<GameScene>();

                if (gs == null)
                {
                    return;
                }

                gs.Hotkeys.UnBind(_key);
            };

            if (_hotkesBoxes.Count != 0)
            {
                box.Y = _hotkesBoxes[_hotkesBoxes.Count - 1].Bounds.Bottom;
            }


            _hotkesBoxes.Add(box);

            Add(box);
        }
    }
}