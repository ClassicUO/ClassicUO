#region license
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

using ClassicUO.Data;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.IO;
using ClassicUO.IO.Resources;
using ClassicUO.Utility;
using System;
using System.Collections.Generic;

namespace ClassicUO.Game.UI.Gumps
{
    internal class CombatBookGump : Gump
    {
        private readonly int _abilityCount = Constants.MAX_ABILITIES_COUNT;
        private int _dictionaryPagesCount = 3;
        private GumpPic _pageCornerLeft, _pageCornerRight, _primAbility, _secAbility;
        private float _clickTiming;
        private Control _lastPressed;


        public CombatBookGump(int x, int y) : base(0, 0)
        {
            X = x;
            Y = y;

            CanMove = true;
            CanCloseWithRightClick = true;

            if (Client.Version < ClientVersion.CV_7000)
            {
                if (Client.Version < ClientVersion.CV_500A)
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

            int maxPages = _dictionaryPagesCount + 1;

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

                        text = new HoveredLabel(AbilityData.Abilities[offs].Name, false, 0x0288, 0x33, 0x0288, font: 9)
                        {
                            X = dataX, Y = 42 + y, AcceptMouseInput = true,
                            LocalSerial = (uint) (maxPages++),
                            Tag = offs
                        };

                        text.MouseUp += (s, e) =>
                        {
                            if (s is HoveredLabel l && e.Button == MouseButtonType.Left)
                            {
                                _clickTiming += Mouse.MOUSE_DELAY_DOUBLE_CLICK;

                                if (_clickTiming > 0)
                                    _lastPressed = l;
                            }
                        };

                        Add(text, page);
                        text.SetTooltip(ClilocLoader.Instance.GetString(1061693 + offs), 150);

                        y += 15;
                        offs++;
                    }

                    if (spellsOnPage == 4)
                    {
                        byte bab1 = (byte) (((byte) World.Player.PrimaryAbility & 0x7F) - 1);

                        _primAbility = new GumpPic(215, 105, (ushort) (0x5200 + bab1), 0);
                        text = new Label("Primary Ability Icon", false, 0x0288, 80, 6) {X = 265, Y = 105};
                        Add(_primAbility, page);
                        Add(text, page);
                        _primAbility.SetTooltip(ClilocLoader.Instance.GetString(1028838 + bab1));
                        _primAbility.DragBegin += OnGumpicDragBeginPrimary;



                        byte bab2 = (byte) (((byte) World.Player.SecondaryAbility & 0x7F) - 1);

                        _secAbility = new GumpPic(215, 150, (ushort) (0x5200 + bab2), 0);
                        text = new Label("Secondary Ability Icon", false, 0x0288, 80, 6) {X = 265, Y = 150};
                        Add(_secAbility, page);
                        Add(text, page);
                        _secAbility.SetTooltip(ClilocLoader.Instance.GetString(1028838 + bab2));
                        _secAbility.DragBegin += OnGumpicDragBeginSecondary;
                    }
                }
            }


            int pageW = _dictionaryPagesCount + 1;

            _dictionaryPagesCount += _abilityCount;

            for (int i = 0; i < _abilityCount; i++, pageW++)
            {
                if (i >= AbilityData.Abilities.Length)
                    break;

                var icon = new GumpPic(62, 40, (ushort) (0x5200 + i), 0);
                Add(icon, pageW);
                icon.SetTooltip(ClilocLoader.Instance.GetString(1061693 + i), 150);

                Label text = new Label(StringHelper.CapitalizeAllWords(AbilityData.Abilities[i].Name), false, 0x0288, 80, 6)
                {
                    X = 110,
                    Y = 34
                };

                Add(text, pageW);

                Add(new GumpPicTiled(0x0835)
                {
                    X = 62,
                    Y = 88,
                    Width = 128
                }, pageW);


                var list = GetItemsList((byte) i);
                int maxStaticCount = TileDataLoader.Instance.StaticData.Length;

                int textX = 62;
                int textY = 98;


                for (int j = 0; j < list.Count; j++)
                {
                    if (j == 6)
                    {
                        textX = 215;
                        textY = 34;
                    }

                    ushort id = list[j];

                    if (id >= maxStaticCount)
                        continue;

                    text = new Label(StringHelper.CapitalizeAllWords(TileDataLoader.Instance.StaticData[id].Name), false, 0x0288, font: 9)
                    {
                        X = textX,
                        Y = textY
                    };

                    Add(text, pageW);

                    textY += 16;
                }
            }
        }


        private void OnGumpicDragBeginPrimary(object sender, EventArgs e)
        {
            if (UIManager.IsDragging)
                return;

            ref readonly AbilityDefinition def = ref AbilityData.Abilities[((byte) World.Player.PrimaryAbility & 0x7F) - 1];

            UseAbilityButtonGump gump = new UseAbilityButtonGump(def, true)
            {
                X = Mouse.Position.X - 22,
                Y = Mouse.Position.Y - 22
            };
            UIManager.Add(gump);
            UIManager.AttemptDragControl(gump, Mouse.Position, true);
        }

        private void OnGumpicDragBeginSecondary(object sender, EventArgs e)
        {
            if (UIManager.IsDragging)
                return;

            ref readonly AbilityDefinition def = ref AbilityData.Abilities[((byte) World.Player.SecondaryAbility & 0x7F) - 1];

            UseAbilityButtonGump gump = new UseAbilityButtonGump(def, false)
            {
                X = Mouse.Position.X - 22,
                Y = Mouse.Position.Y - 22
            };
            UIManager.Add(gump);
            UIManager.AttemptDragControl(gump, Mouse.Position, true);
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

            if (IsDisposed)
                return;

            if (_lastPressed != null)
            {
                _clickTiming -= (float) frameMS;

                if (_clickTiming <= 0)
                {
                    _clickTiming = 0;
                    SetActivePage((int) _lastPressed.LocalSerial);
                    _lastPressed = null;
                }
            }
        }

        private void PageCornerOnMouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtonType.Left && sender is Control ctrl) SetActivePage(ctrl.LocalSerial == 0 ? ActivePage - 1 : ActivePage + 1);
        }

        private void PageCornerOnMouseDoubleClick(object sender, MouseDoubleClickEventArgs e)
        {
            if (e.Button == MouseButtonType.Left && sender is Control ctrl) SetActivePage(ctrl.LocalSerial == 0 ? 1 : _dictionaryPagesCount);
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

            Client.Game.Scene.Audio.PlaySound(0x0055);
        }


        private List<ushort> GetItemsList(byte index)
        {
            List<ushort> list = new List<ushort>();


            switch (index)
            {
                case 0:
                {
                    list.Add(3908);
                    list.Add(5048);
                    list.Add(3935);
                    list.Add(5119);
                    list.Add(9927);
                    list.Add(5181);
                    list.Add(5040);
                    list.Add(5121);
                    list.Add(3939);
                    list.Add(9932);
                    list.Add(11554);
                    list.Add(16497);
                    list.Add(16502);
                    list.Add(16494);
                    list.Add(16491);
                    break;
                }
                case 1:
                {
                    list.Add(3779);
                    list.Add(5115);
                    list.Add(3912);
                    list.Add(3910);
                    list.Add(5185);
                    list.Add(9924);
                    list.Add(5127);
                    list.Add(5040);
                    list.Add(3720);
                    list.Add(5125);
                    list.Add(11552);
                    list.Add(16499);
                    list.Add(16498);
                    break;
                }
                case 2:
                {
                    list.Add(5048);
                    list.Add(3912);
                    list.Add(5183);
                    list.Add(5179);
                    list.Add(3933);
                    list.Add(5113);
                    list.Add(3722);
                    list.Add(9930);
                    list.Add(3920);
                    list.Add(11556);
                    list.Add(16487);
                    list.Add(16500);
                    break;
                }
                case 3:
                {
                    list.Add(5050);
                    list.Add(3914);
                    list.Add(3935);
                    list.Add(3714);
                    list.Add(5092);
                    list.Add(5179);
                    list.Add(5127);
                    list.Add(5177);
                    list.Add(9926);
                    list.Add(4021);
                    list.Add(10146);
                    list.Add(11556);
                    list.Add(11560);
                    list.Add(5109);
                    list.Add(16500);
                    list.Add(16495);
                    break;
                }
                case 4:
                {
                    list.Add(5111);
                    list.Add(3718);
                    list.Add(3781);
                    list.Add(3908);
                    list.Add(3573);
                    list.Add(3714);
                    list.Add(3933);
                    list.Add(5125);
                    list.Add(11558);
                    list.Add(11560);
                    list.Add(5109);
                    list.Add(9934);
                    list.Add(16493);
                    list.Add(16494);
                    break;
                }
                case 5:
                {
                    list.Add(3918);
                    list.Add(3914);
                    list.Add(9927);
                    list.Add(3573);
                    list.Add(5044);
                    list.Add(3720);
                    list.Add(9930);
                    list.Add(5117);
                    list.Add(16501);
                    list.Add(16495);
                    break;
                }
                case 6:
                {
                    list.Add(3718);
                    list.Add(5187);
                    list.Add(3916);
                    list.Add(5046);
                    list.Add(5119);
                    list.Add(9931);
                    list.Add(3722);
                    list.Add(9929);
                    list.Add(9933);
                    list.Add(10148);
                    list.Add(10153);
                    list.Add(16488);
                    list.Add(16493);
                    list.Add(16496);
                    break;
                }
                case 7:
                {
                    list.Add(5111);
                    list.Add(3779);
                    list.Add(3922);
                    list.Add(9928);
                    list.Add(5121);
                    list.Add(9929);
                    list.Add(11553);
                    list.Add(16490);
                    list.Add(16488);
                    break;
                }
                case 8:
                {
                    list.Add(3910);
                    list.Add(9925);
                    list.Add(9931);
                    list.Add(5181);
                    list.Add(9926);
                    list.Add(5123);
                    list.Add(3920);
                    list.Add(5042);
                    list.Add(16499);
                    list.Add(16502);
                    list.Add(16496);
                    list.Add(16491);
                    break;
                }
                case 9:
                {
                    list.Add(5117);
                    list.Add(9932);
                    list.Add(9933);
                    list.Add(16492);
                    break;
                }
                case 10:
                {
                    list.Add(5050);
                    list.Add(3918);
                    list.Add(5046);
                    list.Add(9924);
                    list.Add(9925);
                    list.Add(5113);
                    list.Add(3569);
                    list.Add(9928);
                    list.Add(3939);
                    list.Add(5042);
                    list.Add(16497);
                    list.Add(16498);
                    break;
                }
                case 11:
                {
                    list.Add(3781);
                    list.Add(5187);
                    list.Add(5185);
                    list.Add(5092);
                    list.Add(5044);
                    list.Add(3922);
                    list.Add(5123);
                    list.Add(4021);
                    list.Add(11553);
                    list.Add(16490);
                    break;
                }
                case 12:
                {
                    list.Add(5115);
                    list.Add(5183);
                    list.Add(3916);
                    list.Add(5177);
                    list.Add(3569);
                    list.Add(10157);
                    list.Add(11559);
                    list.Add(9934);
                    list.Add(16501);
                    break;
                }
                case 13:
                {
                    list.Add(10146);
                    break;
                }
                case 14:
                {
                    list.Add(10148);
                    list.Add(10150);
                    list.Add(10151);
                    break;
                }
                case 15:
                {
                    list.Add(10147);
                    list.Add(10158);
                    list.Add(10159);
                    list.Add(11557);
                    break;
                }
                case 16:
                {
                    list.Add(10151);
                    list.Add(10157);
                    list.Add(11561);
                    break;
                }
                case 17:
                {
                    list.Add(10152);
                    break;
                }
                case 18:
                case 20:
                {
                    list.Add(10155);
                    break;
                }
                case 19:
                {
                    list.Add(10152);
                    list.Add(10153);
                    list.Add(10158);
                    list.Add(11554);
                    break;
                }
                case 21:
                {
                    list.Add(10149);
                    break;
                }
                case 22:
                {
                    list.Add(10149);
                    list.Add(10159);
                    break;
                }
                case 23:
                {
                    list.Add(11555);
                    list.Add(11558);
                    list.Add(11559);
                    list.Add(11561);
                    break;
                }
                case 24:
                case 27:
                {
                    list.Add(11550);
                    break;
                }
                case 25:
                {
                    list.Add(11551);
                    break;
                }
                case 26:
                {
                    list.Add(11551);
                    list.Add(11552);
                    break;
                }
                case 28:
                {
                    list.Add(11557);
                    break;
                }
                case 29:
                {
                    list.Add(16492);
                    break;
                }
                case 30:
                {
                    list.Add(16487);
                    break;
                }
            }

            return list;
        }
    }
}