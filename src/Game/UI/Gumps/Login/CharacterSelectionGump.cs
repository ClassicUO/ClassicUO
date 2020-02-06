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

using ClassicUO.Configuration;
using ClassicUO.Data;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.IO.Resources;

using SDL2;

namespace ClassicUO.Game.UI.Gumps.Login
{
    internal class CharacterSelectionGump : Gump
    {
        private const ushort SELECTED_COLOR = 0x0021;
        private const ushort NORMAL_COLOR = 0x034F;
        private uint _selectedCharacter;

        public CharacterSelectionGump() : base(0, 0)
        {
            CanCloseWithRightClick = false;

            int posInList = 0;
            int yOffset = 150;
            int yBonus = 0;
            int listTitleY = 106;

            LoginScene loginScene = Client.Game.GetScene<LoginScene>();
            var lastSelected = loginScene.Characters.FirstOrDefault(o => o == Settings.GlobalSettings.LastCharacterName);

            var f = World.ClientLockedFeatures.Flags;
            var ff = World.ClientFeatures.Flags;

            if ((Client.Version >= ClientVersion.CV_6040) ||
                (Client.Version >= ClientVersion.CV_5020 && loginScene.Characters.Length > 5))
            {
                listTitleY = 96;
                yOffset = 125;
                yBonus = 45;
            }

            if (!string.IsNullOrEmpty(lastSelected))
                _selectedCharacter = (uint) Array.IndexOf(loginScene.Characters, lastSelected);
            else if (loginScene.Characters.Length > 0)
                _selectedCharacter = 0;

            Add(new ResizePic(0x0A28)
            {
                X = 160, Y = 70, Width = 408, Height = 343 + yBonus
            }, 1);

            Add(new Label(ClilocLoader.Instance.GetString(3000050), false, 0x0386, font: 2)
            {
                X = 267, Y = listTitleY
            }, 1);

            for (int i = 0, valid = 0; i < loginScene.Characters.Length; i++)
            {
                string character = loginScene.Characters[i];
              
                if (!string.IsNullOrEmpty(character))
                {
                    valid++;

                    if (valid > World.ClientFeatures.MaxChars)
                        break;

                    if(World.ClientLockedFeatures.Flags != 0 && !World.ClientLockedFeatures.CharSlots7)
                    {
                        if (valid == 6 && !World.ClientLockedFeatures.CharSlots6)
                            break;
                    }

                    Add(new CharacterEntryGump((uint) i, character, SelectCharacter, LoginCharacter)
                    {
                        X = 224,
                        Y = yOffset + posInList * 40,
                        Hue = posInList == _selectedCharacter ? SELECTED_COLOR : NORMAL_COLOR
                    }, 1);
                    posInList++;
                }
            }

            if(CanCreateChar(loginScene))
            {
                Add(new Button((int) Buttons.New, 0x159D, 0x159F, 0x159E)
                {
                    X = 224, Y = 350 + yBonus, ButtonAction = ButtonAction.Activate
                }, 1);
            }

            Add(new Button((int)Buttons.Delete, 0x159A, 0x159C, 0x159B)
            {
                X = 442,
                Y = 350 + yBonus,
                ButtonAction = ButtonAction.Activate
            }, 1);

            Add(new Button((int) Buttons.Prev, 0x15A1, 0x15A3, 0x15A2)
            {
                X = 586, Y = 445, ButtonAction = ButtonAction.Activate
            }, 1);

            Add(new Button((int) Buttons.Next, 0x15A4, 0x15A6, 0x15A5)
            {
                X = 610, Y = 445, ButtonAction = ButtonAction.Activate
            }, 1);

            AcceptKeyboardInput = true;
            ChangePage(1);
        }

        private bool CanCreateChar(LoginScene scene)
        {
            if (scene.Characters != null)
            {
                int empty = scene.Characters.Count(string.IsNullOrEmpty);
                if (empty > 0 && (scene.Characters.Length - empty) < World.ClientFeatures.MaxChars)
                    return true;
            }
            return false;
        }

        protected override void OnKeyDown(SDL.SDL_Keycode key, SDL.SDL_Keymod mod)
        {
            if (key == SDL.SDL_Keycode.SDLK_RETURN || key == SDL.SDL_Keycode.SDLK_KP_ENTER)
                LoginCharacter(_selectedCharacter);
        }

        public override void OnButtonClick(int buttonID)
        {
            LoginScene loginScene = Client.Game.GetScene<LoginScene>();

            switch ((Buttons) buttonID)
            {
                case Buttons.Delete:
                    DeleteCharacter(loginScene);

                    break;

                case Buttons.New when CanCreateChar(loginScene):
                    loginScene.StartCharCreation();

                    break;

                case Buttons.Next:
                    LoginCharacter(_selectedCharacter);

                    break;

                case Buttons.Prev:
                    loginScene.StepBack();

                    break;
            }

            base.OnButtonClick(buttonID);
        }

        private void DeleteCharacter(LoginScene loginScene)
        {
            var charName = loginScene.Characters[_selectedCharacter];

            if (!string.IsNullOrEmpty(charName))
            {
                var existing = Children.OfType<LoadingGump>().FirstOrDefault();

                if (existing != null)
                    Remove(existing);

                Add(new LoadingGump($"Permanently delete {charName}", LoginButtons.OK | LoginButtons.Cancel, buttonID =>
                {
                    if (buttonID == (int) LoginButtons.OK)
                        loginScene.DeleteCharacter(_selectedCharacter);
                    else
                        ChangePage(1);
                }), 2);
                ChangePage(2);
            }
        }

        private void SelectCharacter(uint index)
        {
            _selectedCharacter = index;

            foreach (CharacterEntryGump characterGump in FindControls<CharacterEntryGump>()) characterGump.Hue = characterGump.CharacterIndex == index ? SELECTED_COLOR : NORMAL_COLOR;
        }

        private void LoginCharacter(uint index)
        {
            LoginScene loginScene = Client.Game.GetScene<LoginScene>();

            if (loginScene.Characters.Length > index && !string.IsNullOrEmpty(loginScene.Characters[index]))
                loginScene.SelectCharacter(index);
        }

        private enum Buttons
        {
            New,
            Delete,
            Next,
            Prev
        }

        private class CharacterEntryGump : Control
        {
            private readonly Label _label;
            private readonly Action<uint> _loginFn;
            private readonly Action<uint> _selectedFn;

            public CharacterEntryGump(uint index, string character, Action<uint> selectedFn, Action<uint> loginFn)
            {
                CharacterIndex = index;
                _selectedFn = selectedFn;
                _loginFn = loginFn;

                // Bg
                Add(new ResizePic(0x0BB8)
                {
                    X = 0, Y = 0, Width = 280, Height = 30
                });

                // Char Name
                Add(_label = new Label(character, false, NORMAL_COLOR, 270, 5, align: TEXT_ALIGN_TYPE.TS_CENTER)
                {
                    X = 0
                });
                AcceptMouseInput = true;
            }

            public uint CharacterIndex { get; }

            public ushort Hue
            {
                get => _label.Hue;
                set => _label.Hue = value;
            }

            protected override bool OnMouseDoubleClick(int x, int y, MouseButtonType button)
            {
                if (button == MouseButtonType.Left)
                {
                    _loginFn(CharacterIndex);

                    return true;
                }

                return false;
            }


            protected override void OnMouseUp(int x, int y, MouseButtonType button)
            {
                if (button == MouseButtonType.Left) _selectedFn(CharacterIndex);
            }
        }
    }
}