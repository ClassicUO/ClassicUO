﻿#region license

// Copyright (C) 2020 ClassicUO Development Community on Github
// 
// This project is an alternative client for the game Ultima Online.
// The goal of this is to develop a lightweight client considering
// new technologies.
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
using System.Linq;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using ClassicUO.Resources;
using ClassicUO.Utility.Platforms;
using SDL2;

namespace ClassicUO.Game.UI.Controls
{
    internal class MacroControl : Control
    {
        private static readonly string[] _allHotkeysNames = Enum.GetNames(typeof(MacroType));
        private static readonly string[] _allSubHotkeysNames = Enum.GetNames(typeof(MacroSubType));
        private readonly DataBox _databox;
        private readonly HotkeyBox _hotkeyBox;


        public MacroControl(string name)
        {
            CanMove = true;

            _hotkeyBox = new HotkeyBox();
            _hotkeyBox.HotkeyChanged += BoxOnHotkeyChanged;
            _hotkeyBox.HotkeyCancelled += BoxOnHotkeyCancelled;


            Add(_hotkeyBox);

            Add
            (
                new NiceButton
                (
                    0, _hotkeyBox.Height + 3, 170, 25, ButtonAction.Activate, ResGumps.CreateMacroButton, 0,
                    TEXT_ALIGN_TYPE.TS_LEFT
                ) { ButtonParameter = 2, IsSelectable = false }
            );

            Add
            (
                new NiceButton(0, _hotkeyBox.Height + 30, 50, 25, ButtonAction.Activate, ResGumps.Add)
                    { IsSelectable = false }
            );

            Add
            (
                new NiceButton(52, _hotkeyBox.Height + 30, 50, 25, ButtonAction.Activate, ResGumps.Remove)
                    { ButtonParameter = 1, IsSelectable = false }
            );


            ScrollArea area = new ScrollArea(10, _hotkeyBox.Bounds.Bottom + 80, 280, 280, true);
            Add(area);

            _databox = new DataBox(0, 0, 280, 280);
            _databox.WantUpdateSize = true;
            area.Add(_databox);


            Macro = Client.Game.GetScene<GameScene>().Macros.FindMacro(name) ?? Macro.CreateEmptyMacro(name);

            SetupKeyByDefault();
            SetupMacroUI();
        }


        public Macro Macro { get; }

        private void AddEmptyMacro()
        {
            MacroObject ob = (MacroObject) Macro.Items;

            if (ob.Code == MacroType.None)
            {
                return;
            }

            while (ob.Next != null)
            {
                MacroObject next = (MacroObject) ob.Next;

                if (next.Code == MacroType.None)
                {
                    return;
                }

                ob = next;
            }

            MacroObject obj = Macro.Create(MacroType.None);

            Macro.PushToBack(obj);

            _databox.Add(new MacroEntry(this, obj, _allHotkeysNames));
            _databox.WantUpdateSize = true;
            _databox.ReArrangeChildren();
        }

        private void RemoveLastCommand()
        {
            if (_databox.Children.Count != 0)
            {
                LinkedObject last = Macro.GetLast();

                Macro.Remove(last);

                _databox.Children[_databox.Children.Count - 1].Dispose();

                SetupMacroUI();
            }

            if (_databox.Children.Count == 0)
            {
                AddEmptyMacro();
            }
        }

        private void SetupMacroUI()
        {
            if (Macro == null)
            {
                return;
            }

            _databox.Clear();
            _databox.Children.Clear();

            if (Macro.Items == null)
            {
                Macro.Items = Macro.Create(MacroType.None);
            }

            MacroObject obj = (MacroObject) Macro.Items;

            while (obj != null)
            {
                _databox.Add(new MacroEntry(this, obj, _allHotkeysNames));

                if (obj.Next != null && obj.Code == MacroType.None)
                {
                    break;
                }

                obj = (MacroObject) obj.Next;
            }

            _databox.WantUpdateSize = true;
            _databox.ReArrangeChildren();
        }

        private void SetupKeyByDefault()
        {
            if (Macro == null || _hotkeyBox == null)
            {
                return;
            }

            if (Macro.Key != SDL.SDL_Keycode.SDLK_UNKNOWN)
            {
                SDL.SDL_Keymod mod = SDL.SDL_Keymod.KMOD_NONE;

                if (Macro.Alt)
                {
                    mod |= SDL.SDL_Keymod.KMOD_ALT;
                }

                if (Macro.Shift)
                {
                    mod |= SDL.SDL_Keymod.KMOD_SHIFT;
                }

                if (Macro.Ctrl)
                {
                    mod |= SDL.SDL_Keymod.KMOD_CTRL;
                }

                if (Macro.Cmd)
                {
                    mod |= SDL.SDL_Keymod.KMOD_GUI;
                }

                _hotkeyBox.SetKey(Macro.Key, mod);
            }
        }

        private void BoxOnHotkeyChanged(object sender, EventArgs e)
        {
            bool shift = (_hotkeyBox.Mod & SDL.SDL_Keymod.KMOD_SHIFT) != SDL.SDL_Keymod.KMOD_NONE;
            bool alt = (_hotkeyBox.Mod & SDL.SDL_Keymod.KMOD_ALT) != SDL.SDL_Keymod.KMOD_NONE;
            bool ctrl = (_hotkeyBox.Mod & SDL.SDL_Keymod.KMOD_CTRL) != SDL.SDL_Keymod.KMOD_NONE;
            bool cmd = (_hotkeyBox.Mod & SDL.SDL_Keymod.KMOD_GUI) != SDL.SDL_Keymod.KMOD_NONE && PlatformHelper.IsOSX;

            if (_hotkeyBox.Key != SDL.SDL_Keycode.SDLK_UNKNOWN)
            {
                Macro macro = Client.Game.GetScene<GameScene>().Macros.FindMacro(_hotkeyBox.Key, alt, ctrl, shift, cmd);

                if (macro != null)
                {
                    if (Macro == macro)
                    {
                        return;
                    }

                    SetupKeyByDefault();
                    UIManager.Add(new MessageBoxGump(250, 150, string.Format(ResGumps.ThisKeyCombinationAlreadyExists, macro.Name), null));

                    return;
                }
            }
            else
            {
                return;
            }

            Macro m = Macro;
            m.Key = _hotkeyBox.Key;
            m.Shift = shift;
            m.Alt = alt;
            m.Ctrl = ctrl;
            m.Cmd = cmd;
        }

        private void BoxOnHotkeyCancelled(object sender, EventArgs e)
        {
            Macro m = Macro;
            m.Alt = m.Ctrl = m.Shift = m.Cmd = false;
            m.Key = SDL.SDL_Keycode.SDLK_UNKNOWN;
        }

        public override void OnButtonClick(int buttonID)
        {
            if (buttonID == 0) // add
            {
                AddEmptyMacro();
            }
            else if (buttonID == 1) // remove
            {
                RemoveLastCommand();
            }
            else if (buttonID == 2) // add macro button
            {
                UIManager.Gumps.OfType<MacroButtonGump>().FirstOrDefault(s => s._macro == Macro)?.Dispose();

                MacroButtonGump macroButtonGump = new MacroButtonGump(Macro, Mouse.Position.X, Mouse.Position.Y);
                UIManager.Add(macroButtonGump);
            }
        }


        private class MacroEntry : Control
        {
            private readonly MacroControl _control;
            private readonly string[] _items;

            public MacroEntry(MacroControl control, MacroObject obj, string[] items)
            {
                _control = control;
                _items = items;

                Combobox mainBox = new Combobox(0, 0, 200, _items, (int) obj.Code)
                {
                    Tag = obj
                };

                mainBox.OnOptionSelected += BoxOnOnOptionSelected;

                Add(mainBox);

                Width = mainBox.Width;
                Height = mainBox.Height;

                AddSubMacro(obj);

                WantUpdateSize = true;
            }


            private void AddSubMacro(MacroObject obj)
            {
                if (obj == null || obj.Code == 0)
                {
                    return;
                }

                switch (obj.SubMenuType)
                {
                    case 1:
                        int count = 0;
                        int offset = 0;
                        Macro.GetBoundByCode(obj.Code, ref count, ref offset);

                        string[] names = new string[count];

                        for (int i = 0; i < count; i++)
                        {
                            names[i] = _allSubHotkeysNames[i + offset];
                        }

                        Combobox sub = new Combobox(20, Height, 180, names, (int) obj.SubCode - offset, 300);

                        sub.OnOptionSelected += (senderr, ee) =>
                        {
                            Macro.GetBoundByCode(obj.Code, ref count, ref offset);
                            MacroSubType subType = (MacroSubType) (offset + ee);
                            obj.SubCode = subType;
                        };

                        Add(sub);

                        Height += sub.Height;


                        break;

                    case 2:

                        ResizePic background = new ResizePic(0x0BB8)
                        {
                            X = 16,
                            Y = Height,
                            Width = 240,
                            Height = 60
                        };

                        Add(background);

                        StbTextBox textbox = new StbTextBox(0xFF, 80, 236, true, FontStyle.BlackBorder)
                        {
                            X = background.X + 4,
                            Y = background.Y + 4,
                            Width = background.Width - 4,
                            Height = background.Height - 4
                        };

                        textbox.SetText(obj.HasString() ? ((MacroObjectString) obj).Text : string.Empty);

                        textbox.TextChanged += (sss, eee) =>
                        {
                            if (obj.HasString())
                            {
                                ((MacroObjectString) obj).Text = ((StbTextBox) sss).Text;
                            }
                        };

                        Add(textbox);

                        WantUpdateSize = true;
                        Height += background.Height;

                        break;
                }

                _control._databox.ReArrangeChildren();
            }


            private void BoxOnOnOptionSelected(object sender, int e)
            {
                WantUpdateSize = true;

                Combobox box = (Combobox) sender;
                MacroObject m = (MacroObject) box.Tag;

                if (e == 0)
                {
                    _control.Macro.Remove(m);

                    box.Tag = null;

                    Dispose();

                    _control.SetupMacroUI();
                }
                else
                {
                    MacroObject newmacro = Macro.Create((MacroType) e);

                    _control.Macro.Remove(m);
                    _control.Macro.PushToBack(newmacro);

                    box.Tag = newmacro;


                    for (int i = 1; i < Children.Count; i++)
                    {
                        Children[i]?.Dispose();
                    }

                    Height = box.Height;

                    AddSubMacro(newmacro);
                }
            }
        }
    }
}