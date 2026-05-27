// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Assets;
using ClassicUO.Network;
using ClassicUO.Resources;
using Microsoft.Xna.Framework;

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
        private int _dictionaryPagesCount = 1;
        private GumpPic _pageCornerLeft, _pageCornerRight;
        private int _pagesCount = 3;
        private int _tooltipOffset = 1112198;
        private int _enqueuePage = -1;

        public RacialAbilitiesBookGump(World world, int x, int y) : base(world, 0, 0)
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
                        if (UIManager.DraggingControl != this || UIManager.MouseOverControl != sender)
                        {
                            return;
                        }

                        RacialAbilityButton gump = new RacialAbilityButton(World, (ushort) ((GumpPic) sender).LocalSerial)
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
                            NetClient.Socket.Send_ToggleGargoyleFlying();
                            e.Result = true;
                        }
                    };
                }

                Add(pic, page1);
                pic.SetTooltip(Client.Game.UO.FileManager.Clilocs.GetString(_tooltipOffset + i), 150);

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

        protected override void OnDragBegin(int x, int y)
        {
            if (UIManager.MouseOverControl?.RootParent == this)
            {
                UIManager.MouseOverControl.InvokeDragBegin(new Point(x, y));
            }

            base.OnDragBegin(x, y);
        }

        protected override void OnDragEnd(int x, int y)
        {
            if (UIManager.MouseOverControl?.RootParent == this)
            {
                UIManager.MouseOverControl.InvokeDragEnd(new Point(x, y));
            }

            base.OnDragEnd(x, y);
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
                _enqueuePage = (int)l.LocalSerial;
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

            Client.Game.Audio.PlaySound(0x0055);
        }

        public override void Update()
        {
            base.Update();

            if (IsDisposed)
            {
                return;
            }

            if (_enqueuePage >= 0 && Time.Ticks - Mouse.LastLeftButtonClickTime >= Mouse.MOUSE_DELAY_DOUBLE_CLICK)
            {
                SetActivePage(_enqueuePage);
                _enqueuePage = -1;
            }
        }
    }
}