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
using System.Linq;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Assets;
using ClassicUO.Resources;
using ClassicUO.Utility;
using SDL2;
using ClassicUO.Renderer.Arts;
using ClassicUO.Game.GameObjects;
using System.IO;
using System.Collections.Generic;
using System.Text.Json;
using static ClassicUO.Game.UI.Controls.PaperDollInteractable;
using FontStashSharp.RichText;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps.Login
{
    internal class CharacterSelectionGump : Gump
    {


        //private const ushort SELECTED_COLOR = 0xAAF;
        private static readonly ushort SELECTED_COLOR = 0x4E9;
        private static readonly ushort NORMAL_COLOR = 0x4EB;
        private uint _selectedCharacter;
        private GothicStyleButton button;
        private GothicStyleButton buttonnew;
        private CharacterEntryGump _characterEntryGump;
        private CharacterEntryGump _lastSelectedGumpPic;
        public CharacterSelectionGump() : base(0, 0)
        {
            X = LoginLayoutHelper.ContentOffsetX;
            Y = LoginLayoutHelper.ContentOffsetY;
            CanCloseWithRightClick = false;

            int posInList = 0;
            int yOffset = 290;
            int yBonus = 0;
            int listTitleY = 106;

            LoginScene loginScene = Client.Game.GetScene<LoginScene>();

            string lastCharName = LastCharacterManager.GetLastCharacter(LoginScene.Account, World.ServerName);
            string lastSelected = loginScene.Characters.FirstOrDefault(o => o == lastCharName);

            LockedFeatureFlags f = World.ClientLockedFeatures.Flags;
            CharacterListFlags ff = World.ClientFeatures.Flags;

            if (Client.Version >= ClientVersion.CV_6040 || Client.Version >= ClientVersion.CV_5020 && loginScene.Characters.Length > 5)
            {
                listTitleY = 96;
                yOffset = 290;
                yBonus = 45;
            }

            if (!string.IsNullOrEmpty(lastSelected))
            {
                _selectedCharacter = (uint)Array.IndexOf(loginScene.Characters, lastSelected);
            }
            else if (loginScene.Characters.Length > 0)
            {
                _selectedCharacter = 0;
            }


            bool isAsianLang = string.Compare(Settings.GlobalSettings.Language, "CHT", StringComparison.InvariantCultureIgnoreCase) == 0 ||
                string.Compare(Settings.GlobalSettings.Language, "KOR", StringComparison.InvariantCultureIgnoreCase) == 0 ||
                string.Compare(Settings.GlobalSettings.Language, "JPN", StringComparison.InvariantCultureIgnoreCase) == 0;

            bool unicode = isAsianLang;
            byte font = (byte)(isAsianLang ? 1 : 2);
            ushort hue = (ushort)(isAsianLang ? 0 : 0);

            Add(new TextBox(ClilocLoader.Instance.GetString(3000050, "Character Selection"), TrueTypeLoader.EMBEDDED_FONT, 30, 300, Color.DarkRed, strokeEffect: true) { X = LoginLayoutHelper.CenterOffsetX(300), Y = LoginLayoutHelper.Y(listTitleY), AcceptMouseInput = true });


            for (int i = 0, valid = 0; i < loginScene.Characters.Length; i++)
            {
                string character = loginScene.Characters[i];
                uint bodyId = loginScene.GetCharacterBodyID(i);

                if (!string.IsNullOrEmpty(character))
                {
                    valid++;

                    if (valid > World.ClientFeatures.MaxChars)
                    {
                        break;
                    }

                    if (World.ClientLockedFeatures.Flags != 0 && !World.ClientLockedFeatures.Flags.HasFlag(LockedFeatureFlags.SeventhCharacterSlot))
                    {
                        if (valid == 6 && !World.ClientLockedFeatures.Flags.HasFlag(LockedFeatureFlags.SixthCharacterSlot))
                        {
                            break;
                        }
                    }

                    Add(
                        _characterEntryGump = new CharacterEntryGump((uint)i, character, bodyId, SelectCharacter, LoginCharacter, SelectCharacterHover)
                        {
                            X = LoginLayoutHelper.X(5 + posInList * 140),
                            Y = LoginLayoutHelper.Y(yOffset + posInList * 3)
                        },
                        1
                    );

                    _characterEntryGump.buttonDelete.OnClick += () =>
                    {

                        OnButtonClick(1);
                    };



                    posInList++;
                }
            }

            if (CanCreateChar(loginScene))
            {
                Add(buttonnew = new GothicStyleButton(
                    LoginLayoutHelper.X(30),
                    LoginLayoutHelper.Y(180 + yBonus),
                    120,
                    40,
                    "NEW",
                    null,
                    16
                ));

      
           

                buttonnew.OnClick += () =>
                {
                    OnButtonClick(0);
                };


            } 

           

            Add(button = new GothicStyleButton(
                LoginLayoutHelper.X(30),
                LoginLayoutHelper.Y(680),
                120,
                40,
                "BACK",
                null,
                16
            ));

            button.OnClick += () =>
            {
                OnButtonClick(3);
            };

            Add(button = new GothicStyleButton(
                LoginLayoutHelper.ContentWidth - 30 - 120,
                LoginLayoutHelper.Y(680),
                120,
                40,
                "NEXT",
                null,
                16
            ));

            button.OnClick += () =>
            {
                OnButtonClick(2);
            };


            AcceptKeyboardInput = true;
            ChangePage(1);
            SelectCharacter(_selectedCharacter);
        }

        private void OnGumpPicMouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtonType.Left)
            {
                // Se existir um GumpPic previamente selecionado, restaura sua opacidade
                if (_lastSelectedGumpPic != null)
                {
                    _lastSelectedGumpPic.Alpha = 1.0f; // Restaura opacidade total
                }

                // Define o novo GumpPic clicado como selecionado
                _characterEntryGump.Alpha = 0.5f; // Define opacidade reduzida (50%)
                _lastSelectedGumpPic = _characterEntryGump; // Atualiza o GumpPic selecionado globalmente
            }
        }

        private void OnGumpPicMouseEnter(object sender, EventArgs e)
        {
           // _characterEntryGump.Alpha = 0.5f; // Set opacity to 50% on hover
        }

        private void OnGumpPicMouseExit(object sender, EventArgs e)
        {
            //_characterEntryGump.Alpha = 1.0f; // Reset opacity to 100% when not hovered
        }

        private bool CanCreateChar(LoginScene scene)
        {
            if (scene.Characters != null)
            {
                int empty = scene.Characters.Count(string.IsNullOrEmpty);

                if (empty >= 0 && scene.Characters.Length - empty < World.ClientFeatures.MaxChars)
                {
                    return true;
                }
            }

            return false;
        }

        public override void Update()
        {
            if (!IsDisposed)
            {
                X = LoginLayoutHelper.ContentOffsetX;
                Y = LoginLayoutHelper.ContentOffsetY;
            }
            base.Update();
        }

        protected override void OnControllerButtonUp(SDL.SDL_GameControllerButton button)
        {
            base.OnControllerButtonUp(button);

            LoginScene loginScene = Client.Game.GetScene<LoginScene>();
            int count = loginScene?.Characters?.Length ?? 0;
            if (count == 0)
                return;

            switch (button)
            {
                case SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_UP:
                    uint prev = _selectedCharacter == 0 ? (uint)(count - 1) : _selectedCharacter - 1;
                    for (int i = 0; i < count && string.IsNullOrEmpty(loginScene.Characters[prev]); i++)
                        prev = prev == 0 ? (uint)(count - 1) : prev - 1;
                    SelectCharacter(prev);
                    return;
                case SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_DOWN:
                    uint next = (uint)((_selectedCharacter + 1) % count);
                    for (int i = 0; i < count && string.IsNullOrEmpty(loginScene.Characters[next]); i++)
                        next = (uint)((next + 1) % count);
                    SelectCharacter(next);
                    return;
                case SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_A:
                    LoginCharacter(_selectedCharacter);
                    break;
            }
        }

        protected override void OnKeyDown(SDL.SDL_Keycode key, SDL.SDL_Keymod mod)
        {
            if (key == SDL.SDL_Keycode.SDLK_RETURN || key == SDL.SDL_Keycode.SDLK_KP_ENTER)
            {
                LoginCharacter(_selectedCharacter);
            }
        }

        public override void OnButtonClick(int buttonID)
        {
            LoginScene loginScene = Client.Game.GetScene<LoginScene>();

            switch ((Buttons)buttonID)
            {
                case Buttons.Delete:
                    DeleteCharacter(loginScene);

                    break;

                case Buttons.New when CanCreateChar(loginScene):
                    loginScene.StartCharCreation();

                    break;

                case Buttons.Next:
                    UIManager.GetGump<LoginBackground>()?.Dispose();
                    UIManager.GetGump<CharacterSelectionBackground>()?.Dispose();
                    UIManager.GetGump<SelectServerBackground>()?.Dispose();
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
            string charName = loginScene.Characters[_selectedCharacter];

            if (!string.IsNullOrEmpty(charName))
            {
                LoadingGump existing = Children.OfType<LoadingGump>().FirstOrDefault();

                if (existing != null)
                {
                    Remove(existing);
                }

                Add
                (
                    new LoadingGump
                    (
                        string.Format(ResGumps.PermanentlyDelete0, charName),
                        LoginButtons.OK | LoginButtons.Cancel,
                        buttonID =>
                        {
                            if (buttonID == (int)LoginButtons.OK)
                            {
                                loginScene.DeleteCharacter(_selectedCharacter);
                            }
                            else
                            {
                                ChangePage(1);
                            }
                        }
                    ),
                    2
                );

                ChangePage(2);
            }
        }

        private CharacterEntryGump _lastSelectedGump;
        private uint _lastHoveredIndex = uint.MaxValue;

        private void SelectCharacter(uint index)
        {
            _selectedCharacter = index;

            foreach (CharacterEntryGump characterGump in FindControls<CharacterEntryGump>())
            {
                if (characterGump.CharacterIndex == index)
                {
                    characterGump.Hue = SELECTED_COLOR;
                    characterGump.Alpha = 0.1f; // Mantém a opacidade reduzida no item selecionado
                    _lastSelectedGump = characterGump;
                    characterGump.buttonDelete.IsVisible = true;
                    characterGump.visibleTn = true;
                    characterGump.fullBlendControl.Alpha = 0.2f;
                    characterGump.fullBlendControl.IsVisible = true;
                }
                else
                {
                    characterGump.Hue = NORMAL_COLOR;
                    characterGump.Alpha = 0.1f; // Restaura a opacidade dos demais itens
                    characterGump.buttonDelete.IsVisible = false;
                    characterGump.visibleTn = false;
                    characterGump.fullBlendControl.Alpha = 0.0f;
                    characterGump.fullBlendControl.IsVisible = false;

                }
            }
        }

        private void SelectCharacterHover(uint index)
        {
            uint effectiveIndex = index == uint.MaxValue ? _selectedCharacter : index;
            if (_lastHoveredIndex == effectiveIndex)
            {
                return;
            }
            _lastHoveredIndex = effectiveIndex;
            _selectedCharacter = effectiveIndex;

            foreach (CharacterEntryGump characterGump in FindControls<CharacterEntryGump>())
            {
                characterGump.Hue = characterGump._indexCharacter == effectiveIndex ? SELECTED_COLOR : NORMAL_COLOR;
                characterGump._label.Hue = characterGump._indexCharacter == effectiveIndex ? SELECTED_COLOR : NORMAL_COLOR;
                characterGump.fullBlendControl.Alpha = characterGump._indexCharacter == effectiveIndex ? 0.2f : 0.0f;
                characterGump.fullBlendControl.IsVisible = characterGump._indexCharacter == effectiveIndex;

            }
        }


        private void LoginCharacter(uint index)
        {
            LoginScene loginScene = Client.Game.GetScene<LoginScene>();

            if (loginScene.Characters != null && loginScene.Characters.Length > index && !string.IsNullOrEmpty(loginScene.Characters[index]))
            {
                UIManager.GetGump<LoginBackground>()?.Dispose();
                UIManager.GetGump<CharacterSelectionBackground>()?.Dispose();
                UIManager.GetGump<SelectServerBackground>()?.Dispose();
                loginScene.SelectCharacter(index);
            }
        }

        private enum Buttons
        {
            New,
            Delete,
            Next,
            Prev
        }

        public class CharacterEntryGump : Control
        {
            public TextBox _label;
            private readonly Action<uint> _loginFn;
            private readonly Action<uint> _selectedFn;
            private readonly Action<uint> _hoverFn;
            private readonly ushort _bodyID;
            private static Art art { get; set; }
            private static PlayerMobile _character;
            private PaperDollInteractable _paperDoll;
            private readonly string savePath;
            public uint _indexCharacter;
            public GothicStyleButton buttonDelete;
            public FullBlendControl fullBlendControl;
            public bool visibleTn { get; set; }

            public PaperdollSaveDataDto Load()
            {
                if (!File.Exists(savePath))
                {
                    return null;
                }
                try
                {
                    string json = File.ReadAllText(savePath);
                    return JsonSerializer.Deserialize<PaperdollSaveDataDto>(json);
                }
                catch
                {
                    return null;
                }
            }

            internal class PaperdollSaveDataDto
            {
                public ushort BodyId { get; set; }
                public bool IsFemale { get; set; }
                public byte Race { get; set; }
                public Dictionary<string, PaperdollItem> Items { get; set; }
            }

            private static ushort GetBodyIdFromRaceAndGender(RaceType race, bool isFemale)
            {
                return (race, isFemale) switch
                {
                    (RaceType.HUMAN, false) => 0x0190,
                    (RaceType.HUMAN, true) => 0x0191,
                    (RaceType.ELF, false) => 0x025D,
                    (RaceType.ELF, true) => 0x025E,
                    (RaceType.GARGOYLE, false) => 0x029A,
                    (RaceType.GARGOYLE, true) => 0x029B,
                    _ => 0x0190
                };
            }

            public CharacterEntryGump(uint index, string character, uint bodyID, Action<uint> selectedFn, Action<uint> loginFn, Action<uint> hoverFn)
            {
                CharacterIndex = index;
                _indexCharacter = index;
                savePath = Path.Combine(CUOEnviroment.ExecutablePath, "Data", "Profiles", Settings.GlobalSettings.Username, World.ServerName, character, "paperdollSelectCharManager.json");
                var saveData = Load();
                ushort resolvedBodyId;
                bool isFemale;
                if (saveData != null)
                {
                    isFemale = saveData.IsFemale;
                    resolvedBodyId = saveData.BodyId != 0
                        ? saveData.BodyId
                        : GetBodyIdFromRaceAndGender((RaceType)(saveData.Race < (byte)RaceType.HUMAN ? (byte)RaceType.HUMAN : saveData.Race > (byte)RaceType.GARGOYLE ? (byte)RaceType.GARGOYLE : saveData.Race), saveData.IsFemale);
                }
                else
                {
                    resolvedBodyId = (ushort)bodyID;
                    isFemale = false;
                }
                _bodyID = resolvedBodyId;
                var items = saveData?.Items;

                _selectedFn = selectedFn;
                _hoverFn = hoverFn;
                _loginFn = loginFn;

                Add(new GumpPic(0, 0, 0x000C, 0) { IsPartialHue = true }.ScaleWidthAndHeight(Scale).SetInternalScale(Scale));

                if (items != null && items.Count > 0)
                {
                    var customLayerOrder = new Dictionary<Layer, int>
                    {
                    { Layer.Helmet, 7 },
                    { Layer.Robe, 6},
                    { Layer.Hair, 5 },
                    { Layer.Beard, 4 },
                    { Layer.Waist, 3 },
                    { Layer.OneHanded, 2},
                    { Layer.TwoHanded, 2 },
                    { Layer.Talisman, 1 }
                    };

                    foreach (var item in items.Values
                        .OrderBy(i => customLayerOrder.ContainsKey(i.Layer) ? customLayerOrder[i.Layer] : 0)
                        .ThenBy(i => i.Layer))
                    {
                        if (item.Graphic > 0 && item.Layer != Layer.Bracelet && item.Layer != Layer.Earrings && item.Layer != Layer.Ring && item.Layer != Layer.Backpack) 
                        {
                            ushort id = PaperDollInteractable.GetAnimID(_bodyID, item.AnimID, isFemale);

                            Add(new GumpPicEquipment(
                               item.Serial,
                               0,
                               0,
                               id,
                               (ushort)(item.Hue & 0xFFFF),
                                item.Layer
                            )
                            {
                                AcceptMouseInput = true,

                                IsPartialHue = item.IsPartialHue,
                                CanLift = World.InGame
                                   && !World.Player.IsDead
                                   && LocalSerial == World.Player,
                            }.ScaleWidthAndHeight(Scale).SetInternalScale(InternalScale));
                        }
                    }
                }
                else
                {
                    Add(new GumpPic(1, 1, 0xC4E9, 0));
                    Add(new GumpPic(1, 1, 0xC502, 0));
                    Add(new GumpPic(1, 1, 0xC4FE, 0));
                    Add(new GumpPic(1, 1, 0xC530, 0));
                }

                // Char Name
                Add
               (
                   _label = new TextBox(character, TrueTypeLoader.EMBEDDED_FONT, 16, 190, Color.DarkRed, align: TextHorizontalAlignment.Center, strokeEffect: true) { AcceptMouseInput = true }

               );

                Add
                  (
                     fullBlendControl = new FullBlendControl
                     {
                         X = 40,
                         Y = 230,
                         Width = 120,
                         Height = 2,
                         Hue = 0x801
                     }
                  );

                fullBlendControl.Alpha = 0.0f;
                fullBlendControl.IsVisible = false;


                Add
               (
                   new Button((int)Buttons.Delete, 0x159A, 0x159C, 0x159B)
                   {
                       X = 16,
                       Y = 190,
                       ButtonAction = ButtonAction.Activate
                   },
                   1
               );

                Add(buttonDelete = new GothicStyleButton(
                      x: 75,
                      y: 245,
                      width: 20,
                      height: 20,
                      text: "X",
                      fontPath: null, // Usar fonte padrão
                      fontSize: 22
                  ));
                visibleTn = true;
                buttonDelete.IsVisible = visibleTn;
                buttonDelete.Alpha = 0.0f;

                AcceptMouseInput = true;
            }

            public uint CharacterIndex { get; }

            public ushort Hue
            {
                get => (ushort)_label.Hue;
                set => _label.Hue = (ushort)value;
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
                if (button == MouseButtonType.Left)
                {
                    _selectedFn(CharacterIndex);
                    buttonDelete.IsVisible = true;
                    buttonDelete.Alpha = 1.0f;
                    fullBlendControl.Alpha = 0.2f;
                    fullBlendControl.IsVisible = true;
                }
            }

            protected override void OnMouseOver(int x, int y)
            {
                _hoverFn(CharacterIndex);
                Alpha = 0.7f;
            }

            protected override void OnMouseExit(int x, int y)
            {
                _hoverFn(uint.MaxValue);
                Alpha = 1.0f;
            }
        }
    }
}