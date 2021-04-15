﻿#region license

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

using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.IO.Resources;
using ClassicUO.Network;
using ClassicUO.Resources;

namespace ClassicUO.Game.UI.Gumps
{
    internal class RacialAbilitiesBookGump : Gump
    {
        private static readonly string[] _humanNames = { "Strong Back", "Tough", "Workhorse", "Jack of All Trades" };
        private static readonly string[] _elfNames =
        {
            "Night Sight", "Infused with Magic",
            "Knowledge of Nature", "Difficult to Track",
            "Perception", "Wisdom"
        };
        private static readonly string[] _gargoyleNames = { "Flying", "Berserk", "Master Artisan", "Deadly Aim", "Mystic Insight" };
        private int _abilityCount = 4;
        private float _clickTiming;
        private int _dictionaryPagesCount = 1;
        private Control _lastPressed;
        private GumpPic _pageCornerLeft, _pageCornerRight;
        private int _pagesCount = 3;
        private int _tooltipOffset = 1112198;

        public RacialAbilitiesBookGump(int x, int y) : base(0, 0)
        {
            X = x;
            Y = y;
            CanMove = true;
            CanCloseWithRightClick = true;

            BuildGump();
        }

        private void BuildGump()
        {
            Add(new GumpPic(0, 0, 0x2B29, 0));

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

            int abilityOnPage = 0;
            ushort iconStartGraphic = 0;

            GetSummaryBookInfo(ref abilityOnPage, ref iconStartGraphic);

            _pagesCount = _dictionaryPagesCount + (_abilityCount >> 1);

            int offs = 0;

            for (int page = 1, topage = _dictionaryPagesCount - 1; page <= _dictionaryPagesCount; page++)
            {
                for (int j = 0; j < 2; j++)
                {
                    int indexX = 106;
                    int dataX = 62;
                    int y = 0;

                    if (j % 2 != 0)
                    {
                        indexX = 269;
                        dataX = 225;
                    }


                    Label text = new Label(ResGumps.Index, false, 0x0288, font: 6) { X = indexX, Y = 10 };
                    Add(text, page);

                    for (int i = 0; i < abilityOnPage; i++)
                    {
                        if (offs >= _abilityCount)
                        {
                            break;
                        }

                        if (offs % 2 == 0)
                        {
                            topage++;
                        }

                        bool passive = true;

                        text = new HoveredLabel
                        (
                            GetAbilityName(offs, ref passive),
                            false,
                            0x0288,
                            0x33,
                            0x0288,
                            font: 9
                        )
                        {
                            X = dataX,
                            Y = 52 + y,
                            AcceptMouseInput = true,
                            LocalSerial = (uint) topage
                        };

                        text.MouseUp += OnClicked;
                        Add(text, page);

                        y += 15;
                        offs++;
                    }
                }
            }

            int page1 = _dictionaryPagesCount - 1;

            for (int i = 0; i < _abilityCount; i++)
            {
                int iconX = 62;
                int iconTextX = 112;

                if (i > 0 && i % 2 != 0)
                {
                    iconX = 225;
                    iconTextX = 275;
                }
                else
                {
                    page1++;
                }


                bool passive = true;
                string spellName = GetAbilityName(i, ref passive);

                Label text = new Label
                (
                    spellName,
                    false,
                    0x0288,
                    100,
                    6
                ) { X = iconTextX, Y = 34 };

                Add(text, page1);

                if (passive)
                {
                    text = new Label(ResGumps.Passive, false, 0x0288, font: 6)
                    {
                        X = iconTextX,
                        Y = 64
                    };

                    Add(text, page1);
                }

                ushort graphic = (ushort) (iconStartGraphic + i);

                GumpPic pic = new GumpPic(iconX, 40, graphic, 0)
                {
                    LocalSerial = graphic
                };

                if (!passive)
                {
                    pic.DragBegin += (sender, e) =>
                    {
                        if (UIManager.IsDragging)
                        {
                            return;
                        }

                        RacialAbilityButton gump = new RacialAbilityButton((ushort) ((GumpPic) sender).LocalSerial)
                        {
                            X = Mouse.LClickPosition.X - 20,
                            Y = Mouse.LClickPosition.Y - 20
                        };

                        UIManager.Add(gump);
                        UIManager.AttemptDragControl(gump, true);
                    };

                    pic.MouseDoubleClick += (sender, e) =>
                    {
                        if ((ushort) ((GumpPic) sender).LocalSerial == 0x5DDA && World.Player.Race == RaceType.GARGOYLE)
                        {
                            NetClient.Socket.Send(new PToggleGargoyleFlying());
                            e.Result = true;
                        }
                    };
                }

                Add(pic, page1);
                pic.SetTooltip(ClilocLoader.Instance.GetString(_tooltipOffset + i), 150);

                Add
                (
                    new GumpPicTiled
                    (
                        iconX,
                        88,
                        120,
                        4,
                        0x0835
                    ),
                    page1
                );
            }
        }

        private void GetSummaryBookInfo(ref int abilityOnPage, ref ushort iconStartGraphic)
        {
            _dictionaryPagesCount = 2;
            abilityOnPage = 3;

            switch (World.Player.Race)
            {
                case RaceType.HUMAN:
                    _abilityCount = 4;
                    iconStartGraphic = 0x5DD0;
                    _tooltipOffset = 1112198;

                    break;

                case RaceType.ELF:
                    _abilityCount = 6;
                    iconStartGraphic = 0x5DD4;
                    _tooltipOffset = 1112202;

                    break;

                case RaceType.GARGOYLE:
                    _abilityCount = 5;
                    iconStartGraphic = 0x5DDA;
                    _tooltipOffset = 1112208;

                    break;
            }
        }


        private string GetAbilityName(int offset, ref bool passive)
        {
            passive = true;

            switch (World.Player.Race)
            {
                case RaceType.HUMAN: return _humanNames[offset];
                case RaceType.ELF: return _elfNames[offset];

                case RaceType.GARGOYLE:

                    if (offset == 0)
                    {
                        passive = false;
                    }

                    return _gargoyleNames[offset];

                default: return string.Empty;
            }
        }

        private void OnClicked(object sender, MouseEventArgs e)
        {
            if (sender is HoveredLabel l && e.Button == MouseButtonType.Left)
            {
                _clickTiming += Mouse.MOUSE_DELAY_DOUBLE_CLICK;

                if (_clickTiming > 0)
                {
                    _lastPressed = l;
                }
            }
        }


        private void PageCornerOnMouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtonType.Left && sender is Control ctrl)
            {
                SetActivePage(ctrl.LocalSerial == 0 ? ActivePage - 1 : ActivePage + 1);
            }
        }

        private void PageCornerOnMouseDoubleClick(object sender, MouseDoubleClickEventArgs e)
        {
            if (e.Button == MouseButtonType.Left && sender is Control ctrl)
            {
                SetActivePage(ctrl.LocalSerial == 0 ? 1 : _pagesCount);
            }
        }


        private void SetActivePage(int page)
        {
            if (page < 1)
            {
                page = 1;
            }
            else if (page > _pagesCount)
            {
                page = _pagesCount;
            }

            ActivePage = page;
            _pageCornerLeft.Page = ActivePage != 1 ? 0 : int.MaxValue;
            _pageCornerRight.Page = ActivePage != _pagesCount ? 0 : int.MaxValue;

            Client.Game.Scene.Audio.PlaySound(0x0055);
        }

        public override void Update(double totalTime, double frameTime)
        {
            base.Update(totalTime, frameTime);

            if (IsDisposed)
            {
                return;
            }

            if (_lastPressed != null)
            {
                _clickTiming -= (float) frameTime;

                if (_clickTiming <= 0)
                {
                    _clickTiming = 0;
                    SetActivePage((int) _lastPressed.LocalSerial);
                    _lastPressed = null;
                }
            }
        }
    }
}