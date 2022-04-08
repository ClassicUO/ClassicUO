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

using System;
using System.Collections.Generic;
using System.Linq;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.IO.Resources;
using ClassicUO.Resources;
using SDL2;

namespace ClassicUO.Game.UI.Controls
{
    internal class NameOverheadAssignControl : Control
    {
        private readonly HotkeyBox _hotkeyBox;
        private readonly Dictionary<NameOverheadOptions, Checkbox> checkboxDict = new();

        private enum ButtonType
        {
            CheckAll,
            UncheckAll,
        }

        public NameOverheadAssignControl(NameOverheadOption option)
        {
            Option = option;

            CanMove = true;

            _hotkeyBox = new HotkeyBox();
            _hotkeyBox.HotkeyChanged += BoxOnHotkeyChanged;
            _hotkeyBox.HotkeyCancelled += BoxOnHotkeyCancelled;


            Add(_hotkeyBox);

            Add
            (
                new NiceButton
                (
                    0, _hotkeyBox.Height + 3, 170, 25,
                    ButtonAction.Activate, "Uncheck all", 0, TEXT_ALIGN_TYPE.TS_LEFT
                ) { ButtonParameter = (int)ButtonType.UncheckAll, IsSelectable = false }
            );

            Add
            (
                new NiceButton
                (
                    150, _hotkeyBox.Height + 3, 170, 25,
                    ButtonAction.Activate, "Check all", 0, TEXT_ALIGN_TYPE.TS_LEFT
                ) { ButtonParameter = (int)ButtonType.CheckAll, IsSelectable = false }
            );

            SetupOptionCheckboxes();

            UpdateCheckboxesByCurrentOptionFlags();
            UpdateValueInHotkeyBox();
        }

        private void SetupOptionCheckboxes()
        {
            var i = 0;
            AddCheckbox("Containers", NameOverheadOptions.Containers, 0, 60 + 20 * i++);
            AddCheckbox("Gold", NameOverheadOptions.Gold, 0, 60 + 20 * i++);
            AddCheckbox("Stackable", NameOverheadOptions.Stackable, 0, 60 + 20 * i++);
            AddCheckbox("Other items", NameOverheadOptions.Other, 0, 60 + 20 * i++);

            AddCheckbox("Monster corpses", NameOverheadOptions.MonsterCorpses, 0, 60 + 20 * i++);
            AddCheckbox("Humanoid corpses", NameOverheadOptions.HumanoidCorpses, 0, 60 + 20 * i++);
            AddCheckbox("Own corpses", NameOverheadOptions.OwnCorpses, 0, 60 + 20 * i++);
            // Items
            // Containers = 1 << 0,
            // Gold = 1 << 1,
            // Stackable = 1 << 2,
            // Other = 1 << 3,
            //
            // // Corpses
            // MonsterCorpses = 1 << 4,
            // HumanoidCorpses = 1 << 5,
            // OwnCorpses = 1 << 6,
            //
            // // Mobiles (type)
            // Humanoid = 1 << 7,
            // Monster = 1 << 8,
            // OwnFollowers = 1 << 9,
            //
            // // Mobiles (notoriety)
            // Innocent = 1 << 10,
            // Ally = 1 << 11,
            // Gray = 1 << 12,
            // Criminal = 1 << 13,
            // Enemy = 1 << 14,
            // Murderer = 1 << 15,
            // Invulnerable = 1 << 16,
        }

        private void AddCheckbox(string checkboxName, NameOverheadOptions optionFlag, int x, int y)
        {
            var checkbox = new Checkbox
            (
                0x00D2, 0x00D3, checkboxName, 0xFF,
                0xFFFF
            )
            {
                IsChecked = true,
                X = x,
                Y = y
            };

            checkbox.ValueChanged += (sender, args) =>
            {
                var isChecked = ((Checkbox)sender).IsChecked;

                if (isChecked)
                    Option.NameOverheadOptionFlags |= (int)optionFlag;
                else
                    Option.NameOverheadOptionFlags &= ~(int)optionFlag;
            };

            checkboxDict.Add(optionFlag, checkbox);

            Add(checkbox);
        }

        public NameOverheadOption Option { get; }

        private void UpdateValueInHotkeyBox()
        {
            if (Option == null || _hotkeyBox == null)
            {
                return;
            }

            if (Option.Key != SDL.SDL_Keycode.SDLK_UNKNOWN)
            {
                SDL.SDL_Keymod mod = SDL.SDL_Keymod.KMOD_NONE;

                if (Option.Alt)
                {
                    mod |= SDL.SDL_Keymod.KMOD_ALT;
                }

                if (Option.Shift)
                {
                    mod |= SDL.SDL_Keymod.KMOD_SHIFT;
                }

                if (Option.Ctrl)
                {
                    mod |= SDL.SDL_Keymod.KMOD_CTRL;
                }

                _hotkeyBox.SetKey(Option.Key, mod);
            }
        }

        private void BoxOnHotkeyChanged(object sender, EventArgs e)
        {
            bool shift = (_hotkeyBox.Mod & SDL.SDL_Keymod.KMOD_SHIFT) != SDL.SDL_Keymod.KMOD_NONE;
            bool alt = (_hotkeyBox.Mod & SDL.SDL_Keymod.KMOD_ALT) != SDL.SDL_Keymod.KMOD_NONE;
            bool ctrl = (_hotkeyBox.Mod & SDL.SDL_Keymod.KMOD_CTRL) != SDL.SDL_Keymod.KMOD_NONE;

            if (_hotkeyBox.Key == SDL.SDL_Keycode.SDLK_UNKNOWN)
                return;

            NameOverheadOption option = NameOverHeadManager.Options.FirstOrDefault(o => o.Key == _hotkeyBox.Key && o.Alt == alt && o.Ctrl == ctrl && o.Shift == shift);

            if (option == null)
            {
                Option.Key = _hotkeyBox.Key;
                Option.Shift = shift;
                Option.Alt = alt;
                Option.Ctrl = ctrl;

                return;
            }

            if (Option == option)
                return;

            UpdateValueInHotkeyBox();
            UIManager.Add(new MessageBoxGump(250, 150, string.Format(ResGumps.ThisKeyCombinationAlreadyExists, option.Name), null));
        }

        private void BoxOnHotkeyCancelled(object sender, EventArgs e)
        {
            Option.Alt = Option.Ctrl = Option.Shift = false;
            Option.Key = SDL.SDL_Keycode.SDLK_UNKNOWN;
        }

        public override void OnButtonClick(int buttonID)
        {
            switch ((ButtonType)buttonID)
            {
                case ButtonType.CheckAll: break;
                case ButtonType.UncheckAll: break;
            }
        }

        private void UpdateCheckboxesByCurrentOptionFlags()
        {
            foreach (var kvp in checkboxDict)
            {
                var flag = kvp.Key;
                var checkbox = kvp.Value;

                checkbox.IsChecked = ((NameOverheadOptions)Option.NameOverheadOptionFlags).HasFlag(flag);
            }
        }
    }
}
