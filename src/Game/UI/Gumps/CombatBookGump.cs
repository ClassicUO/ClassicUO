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

using ClassicUO.Game.Data;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.IO;

namespace ClassicUO.Game.UI.Gumps
{
    internal class CombatBookGump : Gump
    {
        private readonly int _abilityCount = Constants.MAX_ABILITIES_COUNT;
        private readonly int _dictionaryPagesCount = 3;
        private GumpPic _pageCornerLeft, _pageCornerRight, _primAbility, _secAbility;

        public CombatBookGump(int x, int y) : base(0, 0)
        {
            X = x;
            Y = y;

            CanMove = true;
            CanCloseWithRightClick = true;

            if (FileManager.ClientVersion < ClientVersions.CV_7000)
            {
                if (FileManager.ClientVersion < ClientVersions.CV_500A)
                    _abilityCount = 29;
                else
                {
                    _abilityCount = 13;
                    _dictionaryPagesCount = 1;
                }
            }

            BuildGump();
        }

        private void BuildGump()
        {
            Add(new GumpPic(0, 0, 0x2B02, 0));


            Add(_pageCornerLeft = new GumpPic(50, 8, 0x08BB, 0));
            _pageCornerLeft.LocalSerial = 0;
            _pageCornerLeft.Page = int.MaxValue;
            _pageCornerLeft.MouseUp += PageCornerOnMouseClick;
            _pageCornerLeft.MouseDoubleClick += PageCornerOnMouseDoubleClick;
            Add(_pageCornerRight = new GumpPic(321, 8, 0x08BC, 0));
            _pageCornerRight.LocalSerial = 1;
            _pageCornerRight.Page = 1;
            _pageCornerRight.MouseUp += PageCornerOnMouseClick;
            _pageCornerRight.MouseDoubleClick += PageCornerOnMouseDoubleClick;

            int offs = 0;

            for (int page = 1; page <= _dictionaryPagesCount; page++)
            {
                for (int j = 0; j < 2; j++)
                {
                    int indexX = 96;
                    int dataX = 52;
                    int y = 0;
                    int spellsOnPage = 9;

                    if (j % 2 != 0)
                    {
                        indexX = 259;
                        dataX = 215;
                        spellsOnPage = 4;
                    }

                    Label text = new Label("INDEX", false, 0x0288, font: 6) {X = indexX, Y = 6};
                    Add(text, page);

                    for (int i = 0; i < spellsOnPage; i++)
                    {
                        if (offs >= _abilityCount)
                            break;

                        text = new HoveredLabel(AbilityData.Abilities[offs].Name, false, 0x0288, 0x33, font: 9)
                        {
                            X = dataX, Y = 42 + y, AcceptMouseInput = true
                        };

                        Add(text, page);

                        y += 15;
                        offs++;
                    }

                    if (spellsOnPage == 4)
                    {
                        _primAbility = new GumpPic(215, 105, (ushort) (0x5200 + ((byte) World.Player.PrimaryAbility & 0x7F) - 1), 0);
                        text = new Label("Primary Ability Icon", false, 0x0288, 80, 6) {X = 265, Y = 105};
                        Add(_primAbility);
                        Add(text, page);

                        _primAbility.DragBegin += (sender, e) =>
                        {
                            if (Engine.UI.IsDragging)
                                return;

                            ref readonly AbilityDefinition def = ref AbilityData.Abilities[((byte) World.Player.PrimaryAbility & 0x7F) - 1];

                            UseAbilityButtonGump gump = new UseAbilityButtonGump(def, true)
                            {
                                X = Mouse.Position.X - 22, Y = Mouse.Position.Y - 22
                            };
                            Engine.UI.Add(gump);
                            Engine.UI.AttemptDragControl(gump, Mouse.Position, true);
                        };

                        _secAbility = new GumpPic(215, 150, (ushort) (0x5200 + ((byte) World.Player.SecondaryAbility & 0x7F) - 1), 0);
                        text = new Label("Secondary Ability Icon", false, 0x0288, 80, 6) {X = 265, Y = 150};
                        Add(_secAbility);
                        Add(text, page);

                        _secAbility.DragBegin += (sender, e) =>
                        {
                            if (Engine.UI.IsDragging)
                                return;

                            ref readonly AbilityDefinition def = ref AbilityData.Abilities[((byte) World.Player.SecondaryAbility & 0x7F) - 1];

                            UseAbilityButtonGump gump = new UseAbilityButtonGump(def, false)
                            {
                                X = Mouse.Position.X - 22,
                                Y = Mouse.Position.Y - 22
                            };
                            Engine.UI.Add(gump);
                            Engine.UI.AttemptDragControl(gump, Mouse.Position, true);
                        };
                    }
                }
            }
        }

        public override void Update(double totalMS, double frameMS)
        {
            base.Update(totalMS, frameMS);

            if (IsDisposed)
                return;

            for (int i = 0; i < 2; i++)
            {
                int index = ((byte) (i == 0 ? World.Player.PrimaryAbility : World.Player.SecondaryAbility) & 0x7F) - 1;

                ref readonly AbilityDefinition def = ref AbilityData.Abilities[index];

                if (i == 0)
                {
                    if (_primAbility.Graphic != def.Icon)
                        _primAbility.Graphic = def.Icon;
                }
                else
                {
                    if (_secAbility.Graphic != def.Icon)
                        _secAbility.Graphic = def.Icon;
                }
            }
        }

        private void PageCornerOnMouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButton.Left && sender is Control ctrl) SetActivePage(ctrl.LocalSerial == 0 ? ActivePage - 1 : ActivePage + 1);
        }

        private void PageCornerOnMouseDoubleClick(object sender, MouseDoubleClickEventArgs e)
        {
            if (e.Button == MouseButton.Left && sender is Control ctrl) SetActivePage(ctrl.LocalSerial == 0 ? 1 : _dictionaryPagesCount);
        }


        private void SetActivePage(int page)
        {
            if (page < 1)
                page = 1;
            else if (page > _dictionaryPagesCount)
                page = _dictionaryPagesCount;
            ActivePage = page;
            _pageCornerLeft.Page = ActivePage != 1 ? 0 : int.MaxValue;
            _pageCornerRight.Page = ActivePage != _dictionaryPagesCount ? 0 : int.MaxValue;

            Engine.SceneManager.CurrentScene.Audio.PlaySound(0x0055);
        }
    }
}