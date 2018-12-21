#region license
//  Copyright (C) 2018 ClassicUO Development Community on Github
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
using System.Linq;

using ClassicUO.Configuration;
using ClassicUO.Game.Gumps.Controls;
using ClassicUO.Game.Scenes;
using ClassicUO.Input;
using ClassicUO.IO;
using ClassicUO.IO.Resources;

namespace ClassicUO.Game.Gumps.UIGumps.Login
{
    internal class CharacterSelectionGump : Gump
    {
        private const ushort SELECTED_COLOR = 0x0021;
        private const ushort NORMAL_COLOR = 0x034F;
        private uint _selectedCharacter;

        public CharacterSelectionGump() : base(0, 0)
        {
            bool testField = FileManager.ClientVersion >= ClientVersions.CV_305D;
            int posInList = 0;
            int yOffset = 150;
            int yBonus = 0;
            int listTitleY = 106;

            if (FileManager.ClientVersion >= ClientVersions.CV_6040)
            {
                listTitleY = 96;
                yOffset = 125;
                yBonus = 45;
            }

            LoginScene loginScene = Engine.SceneManager.GetScene<LoginScene>();
            var lastSelected = loginScene.Characters.FirstOrDefault(o => o == Service.Get<Settings>().LastCharacterName);

            if (lastSelected != null)
                _selectedCharacter = (uint) Array.IndexOf(loginScene.Characters, lastSelected);
            else if (loginScene.Characters.Length > 0)
                _selectedCharacter = 0;

            AddChildren(new ResizePic(0x0A28)
            {
                X = 160, Y = 70, Width = 408, Height = 343 + yBonus
            }, 1);

            AddChildren(new Label(FileManager.Cliloc.GetString(3000050), false, 0x0386, font: 2)
            {
                X = 267, Y = listTitleY
            }, 1);

            for (int i = 0; i < loginScene.Characters.Length; i++)
            {
                string character = loginScene.Characters[i];

                if (!string.IsNullOrEmpty(character))
                {
                    AddChildren(new CharacterEntryGump((uint)i, character, SelectCharacter, LoginCharacter)
                    {
                        X = 224,
                        Y = yOffset + posInList * 40,
                        Hue = posInList == _selectedCharacter ? SELECTED_COLOR : NORMAL_COLOR
                    }, 1);
                    posInList++;
                }
            }

            if (loginScene.Characters.Any(string.IsNullOrEmpty))
            {
                AddChildren(new Button((int) Buttons.New, 0x159D, 0x159F, 0x159E)
                {
                    X = 224, Y = 350 + yBonus, ButtonAction = ButtonAction.Activate
                }, 1);
            }

            AddChildren(new Button((int) Buttons.Delete, 0x159A, 0x159C, 0x159B)
            {
                X = 442, Y = 350 + yBonus, ButtonAction = ButtonAction.Activate
            }, 1);

            AddChildren(new Button((int) Buttons.Prev, 0x15A1, 0x15A3, 0x15A2)
            {
                X = 586, Y = 445, ButtonAction = ButtonAction.Activate
            }, 1);

            AddChildren(new Button((int) Buttons.Next, 0x15A4, 0x15A6, 0x15A5)
            {
                X = 610, Y = 445, ButtonAction = ButtonAction.Activate
            }, 1);
            ChangePage(1);
        }

        public override void OnButtonClick(int buttonID)
        {
            LoginScene loginScene = Engine.SceneManager.GetScene<LoginScene>();

            switch ((Buttons) buttonID)
            {
                case Buttons.Delete:
                    DeleteCharacter(loginScene);

                    break;
                case Buttons.New:
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
                    RemoveChildren(existing);
                var text = FileManager.Cliloc.GetString(1080033).Replace("~1_NAME~", charName);

                AddChildren(new LoadingGump(text, LoadingGump.Buttons.OK | LoadingGump.Buttons.Cancel, buttonID =>
                {
                    if (buttonID == (int) LoadingGump.Buttons.OK)
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

            foreach (CharacterEntryGump characterGump in FindControls<CharacterEntryGump>())
            {
                if (characterGump.CharacterIndex == index)
                    characterGump.Hue = SELECTED_COLOR;
                else
                    characterGump.Hue = NORMAL_COLOR;
            }
        }

        private void LoginCharacter(uint index)
        {
            LoginScene loginScene = Engine.SceneManager.GetScene<LoginScene>();

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
                AddChildren(new ResizePic(0x0BB8)
                {
                    X = 0, Y = 0, Width = 280, Height = 30
                });

                // Char Name
                AddChildren(_label = new Label(character, false, NORMAL_COLOR, 270, 5, align: TEXT_ALIGN_TYPE.TS_CENTER)
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

            protected override bool OnMouseDoubleClick(int x, int y, MouseButton button)
            {
                if (button == MouseButton.Left)
                {
                    _loginFn(CharacterIndex);

                    return true;
                }

                return false;
            }

            protected override void OnMouseClick(int x, int y, MouseButton button)
            {
                if (button == MouseButton.Left) _selectedFn(CharacterIndex);
            }
        }
    }
}