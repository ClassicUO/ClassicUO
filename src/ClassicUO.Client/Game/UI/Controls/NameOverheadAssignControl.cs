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
using ClassicUO.Assets;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Resources;
using SDL2;

namespace ClassicUO.Game.UI.Controls
{
    internal class NameOverheadAssignControl : Control
    {
        private readonly HotkeyBox _hotkeyBox;
        private readonly Dictionary<NameOverheadOptions, Checkbox> checkboxDict = new();
        private readonly ScrollArea checkBoxScroll;

        private enum ButtonType
        {
            CheckAll,
            UncheckAll,
        }

        public NameOverheadAssignControl(NameOverheadOption option)
        {
            Option = option;

            CanMove = true;

            AddLabel("Set hotkey:", 0, 0);

            _hotkeyBox = new HotkeyBox
            {
                X = 80
            };

            _hotkeyBox.HotkeyChanged += BoxOnHotkeyChanged;
            _hotkeyBox.HotkeyCancelled += BoxOnHotkeyCancelled;

            Add(_hotkeyBox);

            Add
            (
                new NiceButton
                (
                    0, _hotkeyBox.Height + 3, 100, 25,
                    ButtonAction.Activate, "Uncheck all", 0, TEXT_ALIGN_TYPE.TS_LEFT
                ) { ButtonParameter = (int)ButtonType.UncheckAll, IsSelectable = false }
            );

            Add
            (
                new NiceButton
                (
                    120, _hotkeyBox.Height + 3, 100, 25,
                    ButtonAction.Activate, "Check all", 0, TEXT_ALIGN_TYPE.TS_LEFT
                ) { ButtonParameter = (int)ButtonType.CheckAll, IsSelectable = false }
            );

            Add(checkBoxScroll = new ScrollArea(0, 60, 300, 350, true));

            SetupOptionCheckboxes();

            UpdateCheckboxesByCurrentOptionFlags();
            UpdateValueInHotkeyBox();
        }

        private void SetupOptionCheckboxes()
        {
            var y = 0;
            AddLabel("Items", 75, y, true);
            y += 28;

            AddCheckbox("Containers", NameOverheadOptions.Containers, 0, y);
            AddCheckbox("Gold", NameOverheadOptions.Gold, 150, y);
            y += 22;
            AddCheckbox("Stackable", NameOverheadOptions.Stackable, 0, y);
            AddCheckbox("Locked down", NameOverheadOptions.LockedDown, 150, y);
            y += 22;
            AddCheckbox("Other items", NameOverheadOptions.Other, 0, y);
            y += 28;

            AddLabel("Corpses", 75, y, true);
            y += 28;

            AddCheckbox("Monster corpses", NameOverheadOptions.MonsterCorpses, 0, y);
            AddCheckbox("Humanoid corpses", NameOverheadOptions.HumanoidCorpses, 150, y);
            //y += 22;
            //AddCheckbox("Own corpses", NameOverheadOptions.OwnCorpses, 0, y);
            y += 28;

            AddLabel("Mobiles by type", 75, y, true);
            y += 28;

            AddCheckbox("Humanoid", NameOverheadOptions.Humanoid, 0, y);
            AddCheckbox("Monster", NameOverheadOptions.Monster, 150, y);
            y += 22;
            AddCheckbox("Your Followers", NameOverheadOptions.OwnFollowers, 0, y);
            AddCheckbox("Yourself", NameOverheadOptions.Self, 150, y);
            y += 22;
            AddCheckbox("Exclude yourself", NameOverheadOptions.ExcludeSelf, 0, y);
            y += 28;

            AddLabel("Mobiles by notoriety", 75, y, true);
            y += 28;

            AddCheckbox("Innocent (blue)", NameOverheadOptions.Innocent, 0, y);
            AddCheckbox("Allied (green)", NameOverheadOptions.Ally, 150, y);
            y += 22;
            AddCheckbox("Attackable (gray)", NameOverheadOptions.Gray, 0, y);
            AddCheckbox("Criminal (gray)", NameOverheadOptions.Criminal, 150, y);
            y += 22;
            AddCheckbox("Enemy (orange)", NameOverheadOptions.Enemy, 0, y);
            AddCheckbox("Murderer (red)", NameOverheadOptions.Murderer, 150, y);
            y += 22;
            AddCheckbox("Invulnerable (yellow)", NameOverheadOptions.Invulnerable, 0, y);
        }

        private void AddLabel(string name, int x, int y, bool scrollArea = false)
        {
            var label = new Label(name, true, 0xFFFF)
            {
                X = x,
                Y = y,
            };
            if (scrollArea)
            {
                checkBoxScroll.Add(label);
            }
            else
            {
                Add(label);
            }
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

                if (NameOverHeadManager.LastActiveNameOverheadOption == Option.Name)
                    NameOverHeadManager.ActiveOverheadOptions = (NameOverheadOptions)Option.NameOverheadOptionFlags;
            };

            checkboxDict.Add(optionFlag, checkbox);

            checkBoxScroll.Add(checkbox);
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

            NameOverheadOption option = NameOverHeadManager.FindOptionByHotkey(_hotkeyBox.Key, alt, ctrl, shift);

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
                case ButtonType.CheckAll:
                    Option.NameOverheadOptionFlags = int.MaxValue;
                    UpdateCheckboxesByCurrentOptionFlags();

                    break;

                case ButtonType.UncheckAll:
                    Option.NameOverheadOptionFlags = 0x0;
                    UpdateCheckboxesByCurrentOptionFlags();

                    break;
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
