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

using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Renderer;
using ClassicUO.Resources;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Xml;

namespace ClassicUO.Game.UI.Gumps
{
    internal class SpellbookGump : Gump
    {
        private DataBox _dataBox;
        private HitBox _hitBox;
        private bool _isMinimized;
        private int _maxPage;
        private GumpPic _pageCornerLeft,
            _pageCornerRight,
            _picBase;
        private SpellBookType _spellBookType;
        private readonly bool[] _spells = new bool[64];
        private int _enqueuePage = -1;

        public SpellbookGump(uint item) : this()
        {
            LocalSerial = item;

            BuildGump();
        }

        public SpellbookGump() : base(0, 0)
        {
            CanMove = true;
            AcceptMouseInput = false;
            CanCloseWithRightClick = true;
        }

        public bool IsMinimized
        {
            get => _isMinimized;
            set
            {
                if (_isMinimized != value)
                {
                    _isMinimized = value;

                    GetBookInfo(
                        _spellBookType,
                        out ushort bookGraphic,
                        out ushort minimizedGraphic,
                        out ushort iconStartGraphic,
                        out int maxSpellsCount,
                        out int spellsOnPage,
                        out int dictionaryPagesCount
                    );

                    _picBase.Graphic = value ? minimizedGraphic : bookGraphic;

                    foreach (Control c in Children)
                    {
                        c.IsVisible = !value;
                    }

                    _picBase.IsVisible = true;
                    WantUpdateSize = true;
                }
            }
        }

        public override GumpType GumpType => GumpType.SpellBook;

        public override void Save(XmlTextWriter writer)
        {
            base.Save(writer);
            writer.WriteAttributeString("isminimized", IsMinimized.ToString());
        }

        public override void Restore(XmlElement xml)
        {
            base.Restore(xml);

            Client.Game.GetScene<GameScene>().DoubleClickDelayed(LocalSerial);

            Dispose();
        }

        private void BuildGump()
        {
            Item item = World.Items.Get(LocalSerial);

            if (item == null)
            {
                Dispose();

                return;
            }

            AssignGraphic(item);

            GetBookInfo(
                _spellBookType,
                out ushort bookGraphic,
                out ushort minimizedGraphic,
                out ushort iconStartGraphic,
                out int maxSpellsCount,
                out int spellsOnPage,
                out int dictionaryPagesCount
            );

            Add(_picBase = new GumpPic(0, 0, bookGraphic, 0));
            _picBase.MouseDoubleClick += _picBase_MouseDoubleClick;

            _dataBox = new DataBox(0, 0, 0, 0)
            {
                CanMove = true,
                AcceptMouseInput = true,
                WantUpdateSize = true
            };

            Add(_dataBox);
            _hitBox = new HitBox(0, 98, 27, 23);
            Add(_hitBox);
            _hitBox.MouseUp += _hitBox_MouseUp;

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

            RequestUpdateContents();

            Client.Game.Audio.PlaySound(0x0055);
        }

        private void _picBase_MouseDoubleClick(object sender, MouseDoubleClickEventArgs e)
        {
            if (e.Button == MouseButtonType.Left && IsMinimized)
            {
                IsMinimized = false;
            }
        }

        private void _hitBox_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtonType.Left && !IsMinimized)
            {
                IsMinimized = true;
            }
        }

        public override void Dispose()
        {
            Client.Game.Audio.PlaySound(0x0055);
            UIManager.SavePosition(LocalSerial, Location);
            base.Dispose();
        }

        private void CreateBook()
        {
            _dataBox.Clear();
            _dataBox.WantUpdateSize = true;

            GetBookInfo(
                _spellBookType,
                out ushort bookGraphic,
                out ushort minimizedGraphic,
                out ushort iconStartGraphic,
                out int maxSpellsCount,
                out int spellsOnPage,
                out int dictionaryPagesCount
            );

            int totalSpells = 0;

            Item item = World.Items.Get(LocalSerial);

            if (item == null)
            {
                Dispose();

                return;
            }

            for (LinkedObject i = item.Items; i != null; i = i.Next)
            {
                Item spell = (Item)i;
                int currentCount = spell.Amount;

                if (currentCount > 0 && currentCount <= maxSpellsCount)
                {
                    _spells[currentCount - 1] = true;
                    totalSpells++;
                }
            }

            int pagesToFill =
                _spellBookType == SpellBookType.Mastery
                    ? dictionaryPagesCount
                    : dictionaryPagesCount >> 1;

            _maxPage = pagesToFill + ((totalSpells + 1) >> 1);

            int currentSpellIndex = 0;

            if (_spellBookType == SpellBookType.Magery)
            {
                _dataBox.Add(
                    new Button((int)ButtonCircle.Circle_1_2, 0x08B1, 0x08B1)
                    {
                        X = 58,
                        Y = 175,
                        ButtonAction = ButtonAction.Activate,
                        ToPage = 1
                    }
                );

                _dataBox.Add(
                    new Button((int)ButtonCircle.Circle_1_2, 0x08B2, 0x08B2)
                    {
                        X = 93,
                        Y = 175,
                        ButtonAction = ButtonAction.Activate,
                        ToPage = 1
                    }
                );

                _dataBox.Add(
                    new Button((int)ButtonCircle.Circle_3_4, 0x08B3, 0x08B3)
                    {
                        X = 130,
                        Y = 175,
                        ButtonAction = ButtonAction.Activate,
                        ToPage = 2
                    }
                );

                _dataBox.Add(
                    new Button((int)ButtonCircle.Circle_3_4, 0x08B4, 0x08B4)
                    {
                        X = 164,
                        Y = 175,
                        ButtonAction = ButtonAction.Activate,
                        ToPage = 2
                    }
                );

                _dataBox.Add(
                    new Button((int)ButtonCircle.Circle_5_6, 0x08B5, 0x08B5)
                    {
                        X = 227,
                        Y = 175,
                        ButtonAction = ButtonAction.Activate,
                        ToPage = 3
                    }
                );

                _dataBox.Add(
                    new Button((int)ButtonCircle.Circle_5_6, 0x08B6, 0x08B6)
                    {
                        X = 260,
                        Y = 175,
                        ButtonAction = ButtonAction.Activate,
                        ToPage = 3
                    }
                );

                _dataBox.Add(
                    new Button((int)ButtonCircle.Circle_7_8, 0x08B7, 0x08B7)
                    {
                        X = 297,
                        Y = 175,
                        ButtonAction = ButtonAction.Activate,
                        ToPage = 4
                    }
                );

                _dataBox.Add(
                    new Button((int)ButtonCircle.Circle_7_8, 0x08B8, 0x08B8)
                    {
                        X = 332,
                        Y = 175,
                        ButtonAction = ButtonAction.Activate,
                        ToPage = 4
                    }
                );
            }

            int spellDone = 0;

            for (int page = 1; page <= pagesToFill; page++)
            {
                for (int j = 0; j < 2; j++)
                {
                    if (page == 1 && _spellBookType == SpellBookType.Chivalry)
                    {
                        Label label = new Label(
                            ResGumps.TithingPointsAvailable + World.Player.TithingPoints,
                            false,
                            0x0288,
                            font: 6
                        )
                        {
                            X = 62,
                            Y = 162
                        };

                        _dataBox.Add(label, page);
                    }

                    int indexX = 106;
                    int dataX = 62;
                    int y = 0;

                    if (j % 2 != 0)
                    {
                        indexX = 269;
                        dataX = 225;
                    }

                    Label text = new Label(ResGumps.Index, false, 0x0288, font: 6)
                    {
                        X = indexX,
                        Y = 10
                    };

                    _dataBox.Add(text, page);

                    if (_spellBookType == SpellBookType.Mastery && j >= 1)
                    {
                        text = new Label(ResGumps.Abilities, false, 0x0288, font: 6)
                        {
                            X = dataX,
                            Y = 30
                        };

                        _dataBox.Add(text, page);

                        if (
                            World.OPL.TryGetNameAndData(
                                LocalSerial,
                                out string name,
                                out string data
                            )
                        )
                        {
                            data = data.ToLower();
                            string[] buff = data.Split(
                                new[] { '\n' },
                                StringSplitOptions.RemoveEmptyEntries
                            );

                            for (int i = 0; i < buff.Length; i++)
                            {
                                if (buff[i] != null)
                                {
                                    int index = buff[i].IndexOf(
                                        "mastery",
                                        StringComparison.InvariantCulture
                                    );

                                    if (--index < 0)
                                    {
                                        continue;
                                    }

                                    string skillName = buff[i].Substring(0, index);

                                    if (!string.IsNullOrEmpty(skillName))
                                    {
                                        List<int> activedSpells =
                                            SpellsMastery.GetSpellListByGroupName(skillName);

                                        for (int k = 0; k < activedSpells.Count; k++)
                                        {
                                            int id = activedSpells[k];

                                            SpellDefinition spell = SpellsMastery.GetSpell(id);

                                            if (spell != null)
                                            {
                                                ushort iconGraphic = (ushort)spell.GumpIconID;
                                                int toolTipCliloc =
                                                    id >= 0 && id < 6 ? 1115689 : 1155938 - 6;

                                                int iconMY = 55 + 44 * k;

                                                GumpPic icon = new GumpPic(
                                                    225,
                                                    iconMY,
                                                    iconGraphic,
                                                    0
                                                )
                                                {
                                                    LocalSerial = (uint)(id - 1)
                                                };

                                                _dataBox.Add(icon, page);
                                                icon.MouseDoubleClick += OnIconDoubleClick;
                                                icon.DragBegin += OnIconDragBegin;

                                                text = new Label(spell.Name, false, 0x0288, 80, 6)
                                                {
                                                    X = 225 + 44 + 4,
                                                    Y = iconMY + 2
                                                };

                                                _dataBox.Add(text, page);

                                                if (toolTipCliloc > 0)
                                                {
                                                    string tooltip =
                                                        ClilocLoader.Instance.GetString(
                                                            toolTipCliloc + id
                                                        );

                                                    icon.SetTooltip(tooltip, 250);
                                                }
                                            }
                                        }
                                    }

                                    break;
                                }
                            }
                        }

                        break;
                    }

                    if (_spellBookType == SpellBookType.Magery)
                    {
                        text = new Label(
                            SpellsMagery.CircleNames[(page - 1) * 2 + j % 2],
                            false,
                            0x0288,
                            font: 6
                        )
                        {
                            X = dataX,
                            Y = 30
                        };

                        _dataBox.Add(text, page);
                    }
                    else if (_spellBookType == SpellBookType.Mastery)
                    {
                        text = new Label(
                            page == pagesToFill ? ResGumps.Passive : ResGumps.Activated,
                            false,
                            0x0288,
                            font: 6
                        )
                        {
                            X = dataX,
                            Y = 30
                        };

                        _dataBox.Add(text, page);
                    }

                    int topage = pagesToFill + ((spellDone + 1) >> 1);

                    if (_spellBookType == SpellBookType.Mastery)
                    {
                        int length = SpellsMastery.SpellbookIndices[page - 1].Length;

                        for (int k = 0; k < length; k++)
                        {
                            currentSpellIndex = SpellsMastery.SpellbookIndices[page - 1][k] - 1;

                            if (_spells[currentSpellIndex])
                            {
                                GetSpellNames(
                                    currentSpellIndex,
                                    out string name,
                                    out string abbreviature,
                                    out string reagents
                                );

                                if (spellDone % 2 == 0)
                                {
                                    topage++;
                                }

                                spellDone++;

                                text = new HoveredLabel(
                                    name,
                                    false,
                                    0x0288,
                                    0x33,
                                    0x0288,
                                    font: 9,
                                    maxwidth: 130,
                                    style: FontStyle.Cropped
                                )
                                {
                                    X = dataX,
                                    Y = 52 + y,
                                    LocalSerial = (uint)(pagesToFill + currentSpellIndex / 2 + 1),
                                    AcceptMouseInput = true,
                                    Tag = currentSpellIndex + 1,
                                    CanMove = true
                                };

                                text.MouseUp += OnLabelMouseUp;
                                text.MouseDoubleClick += OnLabelMouseDoubleClick;
                                _dataBox.Add(text, page);

                                y += 15;
                            }
                        }
                    }
                    else
                    {
                        for (int k = 0; k < spellsOnPage; k++, currentSpellIndex++)
                        {
                            if (_spells[currentSpellIndex])
                            {
                                GetSpellNames(
                                    currentSpellIndex,
                                    out string name,
                                    out string abbreviature,
                                    out string reagents
                                );

                                if (spellDone % 2 == 0)
                                {
                                    topage++;
                                }

                                spellDone++;

                                text = new HoveredLabel(
                                    name,
                                    false,
                                    0x0288,
                                    0x33,
                                    0x0288,
                                    font: 9,
                                    maxwidth: 130,
                                    style: FontStyle.Cropped
                                )
                                {
                                    X = dataX,
                                    Y = 52 + y,
                                    LocalSerial = (uint)topage,
                                    AcceptMouseInput = true,
                                    Tag = currentSpellIndex + 1,
                                    CanMove = true
                                };

                                text.MouseUp += OnLabelMouseUp;
                                text.MouseDoubleClick += OnLabelMouseDoubleClick;
                                _dataBox.Add(text, page);

                                y += 15;
                            }
                        }
                    }
                }
            }

            int page1 = pagesToFill + 1;
            int topTextY = 6;

            for (int i = 0, spellsDone = 0; i < maxSpellsCount; i++)
            {
                if (!_spells[i])
                {
                    continue;
                }

                int iconX = 62;
                int topTextX = 87;
                int iconTextX = 112;
                uint iconSerial = 100 + (uint)i;

                if (spellsDone > 0)
                {
                    if (spellsDone % 2 != 0)
                    {
                        iconX = 225;
                        topTextX = 224;
                        iconTextX = 275;
                        iconSerial = 1000 + (uint)i;
                    }
                    else
                    {
                        page1++;
                    }
                }

                spellsDone++;

                GetSpellNames(i, out string name, out string abbreviature, out string reagents);

                switch (_spellBookType)
                {
                    case SpellBookType.Magery:
                        {
                            Label text = new Label(
                                SpellsMagery.CircleNames[i >> 3],
                                false,
                                0x0288,
                                font: 6
                            )
                            {
                                X = topTextX,
                                Y = topTextY + 4
                            };

                            _dataBox.Add(text, page1);

                            text = new Label(name, false, 0x0288, 80, 6) { X = iconTextX, Y = 34 };

                            _dataBox.Add(text, page1);
                            int abbreviatureY = 26;

                            if (text.Height < 24)
                            {
                                abbreviatureY = 31;
                            }

                            abbreviatureY += text.Height;

                            text = new Label(abbreviature, false, 0x0288, font: 8)
                            {
                                X = iconTextX,
                                Y = abbreviatureY
                            };

                            _dataBox.Add(text, page1);

                            break;
                        }

                    case SpellBookType.Mastery:
                        {
                            Label text = new Label(
                                SpellsMastery.GetMasteryGroupByID(i + 1),
                                false,
                                0x0288,
                                font: 6
                            )
                            {
                                X = topTextX,
                                Y = topTextY + 4
                            };

                            _dataBox.Add(text, page1);

                            text = new Label(name, false, 0x0288, 80, 6) { X = iconTextX, Y = 34 };

                            _dataBox.Add(text, page1);

                            if (!string.IsNullOrEmpty(abbreviature))
                            {
                                int abbreviatureY = 26;

                                if (text.Height < 24)
                                {
                                    abbreviatureY = 31;
                                }

                                abbreviatureY += text.Height;

                                text = new Label(abbreviature, false, 0x0288, 80, 6)
                                {
                                    X = iconTextX,
                                    Y = abbreviatureY
                                };

                                _dataBox.Add(text, page1);
                            }

                            break;
                        }

                    default:
                        {
                            Label text = new Label(name, false, 0x0288, font: 6)
                            {
                                X = topTextX,
                                Y = topTextY
                            };

                            _dataBox.Add(text, page1);

                            if (!string.IsNullOrEmpty(abbreviature))
                            {
                                text = new Label(abbreviature, false, 0x0288, 80, 6)
                                {
                                    X = iconTextX,
                                    Y = 34
                                };

                                _dataBox.Add(text, page1);
                            }

                            break;
                        }
                }

                ushort iconGraphic;
                int toolTipCliloc;

                var spellDef = GetSpellDefinition(iconSerial);
                if (_spellBookType == SpellBookType.Mastery)
                {
                    iconGraphic = (ushort)SpellsMastery.GetSpell(i + 1).GumpIconID;

                    toolTipCliloc = i >= 0 && i < 6 ? 1115689 : 1155938 - 6;
                }
                else
                {
                    iconGraphic = (ushort)spellDef.GumpIconSmallID;
                    GetSpellToolTip(out toolTipCliloc);
                }

                HueGumpPic icon = new HueGumpPic(
                    iconX,
                    40,
                    iconGraphic,
                    0,
                    (ushort)spellDef.ID,
                    spellDef.Name
                )
                {
                    X = iconX,
                    Y = 40,
                    LocalSerial = iconSerial
                };

                if (toolTipCliloc > 0)
                {
                    string tooltip = ClilocLoader.Instance.GetString(toolTipCliloc + i);
                    icon.SetTooltip(tooltip, 250);
                }

                icon.MouseDoubleClick += OnIconDoubleClick;
                icon.DragBegin += OnIconDragBegin;

                _dataBox.Add(icon, page1);

                if (!string.IsNullOrEmpty(reagents))
                {
                    if (_spellBookType != SpellBookType.Mastery)
                    {
                        _dataBox.Add(new GumpPicTiled(iconX, 88, 120, 5, 0x0835), page1);
                    }

                    Label text = new Label(ResGumps.Reagents, false, 0x0288, font: 6)
                    {
                        X = iconX,
                        Y = 92
                    };

                    _dataBox.Add(text, page1);

                    text = new Label(reagents, false, 0x0288, font: 9) { X = iconX, Y = 114 };

                    _dataBox.Add(text, page1);
                }

                if (_spellBookType != SpellBookType.Magery)
                {
                    GetSpellRequires(i, out int requiriesY, out string requires);

                    Label text = new Label(requires, false, 0x0288, font: 6)
                    {
                        X = iconX,
                        Y = requiriesY
                    };

                    _dataBox.Add(text, page1);
                }
            }

            SetActivePage(1);
        }

        protected override void UpdateContents()
        {
            Item item = World.Items.Get(LocalSerial);

            if (item == null)
            {
                Dispose();

                return;
            }

            AssignGraphic(item);

            CreateBook();
        }

        private void OnIconDoubleClick(object sender, MouseDoubleClickEventArgs e)
        {
            if (e.Button == MouseButtonType.Left)
            {
                SpellDefinition def = GetSpellDefinition((sender as Control).LocalSerial);

                if (def != null)
                {
                    GameActions.CastSpell(def.ID);
                }
            }
        }

        private void OnIconDragBegin(object sender, EventArgs e)
        {
            if (UIManager.DraggingControl != this || UIManager.MouseOverControl != sender)
            {
                return;
            }

            SpellDefinition def = GetSpellDefinition((sender as Control).LocalSerial);

            if (def == null)
            {
                return;
            }

            GetSpellFloatingButton(def.ID)?.Dispose();

            UseSpellButtonGump gump = new UseSpellButtonGump(def)
            {
                X = Mouse.LClickPosition.X - 22,
                Y = Mouse.LClickPosition.Y - 22
            };

            UIManager.Add(gump);
            UIManager.AttemptDragControl(gump, true);
        }

        private static UseSpellButtonGump GetSpellFloatingButton(int id)
        {
            for (LinkedListNode<Gump> i = UIManager.Gumps.Last; i != null; i = i.Previous)
            {
                if (i.Value is UseSpellButtonGump g && g.SpellID == id)
                {
                    return g;
                }
            }

            return null;
        }

        private SpellDefinition GetSpellDefinition(uint serial)
        {
            int idx =
                (int)(
                    serial > 1000
                        ? serial - 1000
                        : serial >= 100
                            ? serial - 100
                            : serial
                ) + 1;

            return GetSpellDefinition(idx);
        }

        private SpellDefinition GetSpellDefinition(int idx)
        {
            SpellDefinition def = null;

            switch (_spellBookType)
            {
                case SpellBookType.Magery:
                    def = SpellsMagery.GetSpell(idx);

                    break;

                case SpellBookType.Necromancy:
                    def = SpellsNecromancy.GetSpell(idx);

                    break;

                case SpellBookType.Chivalry:
                    def = SpellsChivalry.GetSpell(idx);

                    break;

                case SpellBookType.Bushido:
                    def = SpellsBushido.GetSpell(idx);

                    break;

                case SpellBookType.Ninjitsu:
                    def = SpellsNinjitsu.GetSpell(idx);

                    break;

                case SpellBookType.Spellweaving:
                    def = SpellsSpellweaving.GetSpell(idx);

                    break;

                case SpellBookType.Mysticism:
                    def = SpellsMysticism.GetSpell(idx);

                    break;

                case SpellBookType.Mastery:
                    def = SpellsMastery.GetSpell(idx);

                    break;
            }

            return def;
        }

        private static void GetBookInfo(
            SpellBookType type,
            out ushort bookGraphic,
            out ushort minimizedGraphic,
            out ushort iconStartGraphic,
            out int maxSpellsCount,
            out int spellsOnPage,
            out int dictionaryPagesCount
        )
        {
            switch (type)
            {
                default:
                case SpellBookType.Magery:
                    maxSpellsCount = SpellsMagery.MaxSpellCount;
                    bookGraphic = 0x08AC;
                    minimizedGraphic = 0x08BA;
                    iconStartGraphic = 0x08C0;

                    break;

                case SpellBookType.Necromancy:
                    maxSpellsCount = SpellsNecromancy.MaxSpellCount;
                    bookGraphic = 0x2B00;
                    minimizedGraphic = 0x2B03;
                    iconStartGraphic = 0x5000;

                    break;

                case SpellBookType.Chivalry:
                    maxSpellsCount = SpellsChivalry.MaxSpellCount;
                    bookGraphic = 0x2B01;
                    minimizedGraphic = 0x2B04;
                    iconStartGraphic = 0x5100;

                    break;

                case SpellBookType.Bushido:
                    maxSpellsCount = SpellsBushido.MaxSpellCount;
                    bookGraphic = 0x2B07;
                    minimizedGraphic = 0x2B09;
                    iconStartGraphic = 0x5400;

                    break;

                case SpellBookType.Ninjitsu:
                    maxSpellsCount = SpellsNinjitsu.MaxSpellCount;
                    bookGraphic = 0x2B06;
                    minimizedGraphic = 0x2B08;
                    iconStartGraphic = 0x5300;

                    break;

                case SpellBookType.Spellweaving:
                    maxSpellsCount = SpellsSpellweaving.MaxSpellCount;
                    bookGraphic = 0x2B2F;
                    minimizedGraphic = 0x2B2D;
                    iconStartGraphic = 0x59D8;

                    break;

                case SpellBookType.Mysticism:
                    maxSpellsCount = SpellsMysticism.MaxSpellCount;
                    bookGraphic = 0x2B32;
                    minimizedGraphic = 0x2B30;
                    iconStartGraphic = 0x5DC0;

                    break;

                case SpellBookType.Mastery:
                    maxSpellsCount = SpellsMastery.MaxSpellCount;
                    bookGraphic = 0x8AC;
                    minimizedGraphic = 0x08BA;
                    iconStartGraphic = 0x945;

                    break;
            }

            spellsOnPage = Math.Min(maxSpellsCount >> 1, 8);
            dictionaryPagesCount = (int)Math.Ceiling(maxSpellsCount / 8.0f);

            if (dictionaryPagesCount % 2 != 0)
            {
                dictionaryPagesCount++;
            }
        }

        private void GetSpellToolTip(out int offset)
        {
            switch (_spellBookType)
            {
                case SpellBookType.Magery:
                    offset = 1061290;

                    break;

                case SpellBookType.Necromancy:
                    offset = 1061390;

                    break;

                case SpellBookType.Chivalry:
                    offset = 1061490;

                    break;

                case SpellBookType.Bushido:
                    offset = 1063263;

                    break;

                case SpellBookType.Ninjitsu:
                    offset = 1063279;

                    break;

                case SpellBookType.Spellweaving:
                    offset = 1072042;

                    break;

                case SpellBookType.Mysticism:
                    offset = 1095193;

                    break;

                case SpellBookType.Mastery:
                    offset = 0;

                    break;

                default:
                    offset = 0;

                    break;
            }
        }

        private void GetSpellNames(
            int offset,
            out string name,
            out string abbreviature,
            out string reagents
        )
        {
            switch (_spellBookType)
            {
                default:
                case SpellBookType.Magery:
                    SpellDefinition def = SpellsMagery.GetSpell(offset + 1);
                    name = def.Name;
                    abbreviature = SpellsMagery.SpecialReagentsChars[offset];
                    reagents = def.CreateReagentListString("\n");

                    break;

                case SpellBookType.Necromancy:
                    def = SpellsNecromancy.GetSpell(offset + 1);
                    name = def.Name;
                    abbreviature = def.PowerWords;
                    reagents = def.CreateReagentListString("\n");

                    break;

                case SpellBookType.Chivalry:
                    def = SpellsChivalry.GetSpell(offset + 1);
                    name = def.Name;
                    abbreviature = def.PowerWords;
                    reagents = string.Empty;

                    break;

                case SpellBookType.Bushido:
                    def = SpellsBushido.GetSpell(offset + 1);
                    name = def.Name;
                    abbreviature = def.PowerWords;
                    reagents = string.Empty;

                    break;

                case SpellBookType.Ninjitsu:
                    def = SpellsNinjitsu.GetSpell(offset + 1);
                    name = def.Name;
                    abbreviature = def.PowerWords;
                    reagents = string.Empty;

                    break;

                case SpellBookType.Spellweaving:
                    def = SpellsSpellweaving.GetSpell(offset + 1);
                    name = def.Name;
                    abbreviature = def.PowerWords;
                    reagents = string.Empty;

                    break;

                case SpellBookType.Mysticism:
                    def = SpellsMysticism.GetSpell(offset + 1);
                    name = def.Name;
                    abbreviature = def.PowerWords;
                    reagents = def.CreateReagentListString("\n");

                    break;

                case SpellBookType.Mastery:
                    def = SpellsMastery.GetSpell(offset + 1);
                    name = def.Name;
                    abbreviature = def.PowerWords;
                    reagents = def.CreateReagentListString("\n");

                    break;
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

        private void GetSpellRequires(int offset, out int y, out string text)
        {
            y = 162;
            int manaCost = 0;
            int minSkill = 0;

            switch (_spellBookType)
            {
                case SpellBookType.Necromancy:
                    SpellDefinition def = SpellsNecromancy.GetSpell(offset + 1);
                    manaCost = def.ManaCost;
                    minSkill = def.MinSkill;

                    break;

                case SpellBookType.Chivalry:
                    def = SpellsChivalry.GetSpell(offset + 1);
                    manaCost = def.ManaCost;
                    minSkill = def.MinSkill;

                    break;

                case SpellBookType.Bushido:
                    def = SpellsBushido.GetSpell(offset + 1);
                    manaCost = def.ManaCost;
                    minSkill = def.MinSkill;

                    break;

                case SpellBookType.Ninjitsu:
                    def = SpellsNinjitsu.GetSpell(offset + 1);
                    manaCost = def.ManaCost;
                    minSkill = def.MinSkill;

                    break;

                case SpellBookType.Spellweaving:
                    def = SpellsSpellweaving.GetSpell(offset + 1);
                    manaCost = def.ManaCost;
                    minSkill = def.MinSkill;

                    break;

                case SpellBookType.Mysticism:
                    def = SpellsMysticism.GetSpell(offset + 1);
                    manaCost = def.ManaCost;
                    minSkill = def.MinSkill;

                    break;

                case SpellBookType.Mastery:
                    def = SpellsMastery.GetSpell(offset + 1);
                    manaCost = def.ManaCost;
                    minSkill = def.MinSkill;

                    if (def.TithingCost > 0)
                    {
                        y = 148;
                        text = string.Format(
                            ResGumps.Upkeep0Mana1MinSkill2,
                            def.TithingCost,
                            manaCost,
                            minSkill
                        );
                    }
                    else
                    {
                        text = string.Format(ResGumps.ManaCost0MinSkill1, manaCost, minSkill);
                    }

                    return;
            }

            text = string.Format(ResGumps.ManaCost0MinSkill1, manaCost, minSkill);
        }

        private void SetActivePage(int page)
        {
            if (page == _dataBox.ActivePage)
            {
                return;
            }

            if (page < 1)
            {
                page = 1;
            }
            else if (page > _maxPage)
            {
                page = _maxPage;
            }

            _dataBox.ActivePage = page;
            _pageCornerLeft.Page = _dataBox.ActivePage != 1 ? 0 : int.MaxValue;
            _pageCornerRight.Page = _dataBox.ActivePage != _maxPage ? 0 : int.MaxValue;

            Client.Game.Audio.PlaySound(0x0055);
        }

        private void OnLabelMouseUp(object sender, MouseEventArgs e)
        {
            if (
                e.Button == MouseButtonType.Left
                && Mouse.LDragOffset == Point.Zero
                && sender is HoveredLabel l
            )
            {
                _enqueuePage = (int)l.LocalSerial;
            }
        }

        private void OnLabelMouseDoubleClick(object sender, MouseDoubleClickEventArgs e)
        {
            if (e.Button == MouseButtonType.Left && sender is HoveredLabel l)
            {
                SpellDefinition def = GetSpellDefinition((int)l.Tag);

                if (def != null)
                {
                    GameActions.CastSpell(def.ID);
                }

                _enqueuePage = -1;
                e.Result = true;
            }
        }

        public override void Update()
        {
            base.Update();

            Item item = World.Items.Get(LocalSerial);

            if (item == null)
            {
                Dispose();
            }

            if (IsDisposed)
            {
                return;
            }

            if (
                _enqueuePage >= 0
                && Time.Ticks - Mouse.LastLeftButtonClickTime >= Mouse.MOUSE_DELAY_DOUBLE_CLICK
            )
            {
                SetActivePage(_enqueuePage);
                _enqueuePage = -1;
            }
        }

        private void AssignGraphic(Item item)
        {
            switch (item.Graphic)
            {
                default:
                case 0x0EFA:
                    _spellBookType = SpellBookType.Magery;

                    break;

                case 0x2253:
                    _spellBookType = SpellBookType.Necromancy;

                    break;

                case 0x2252:
                    _spellBookType = SpellBookType.Chivalry;

                    break;

                case 0x238C:

                    if ((World.ClientFeatures.Flags & CharacterListFlags.CLF_SAMURAI_NINJA) != 0)
                    {
                        _spellBookType = SpellBookType.Bushido;
                    }

                    break;

                case 0x23A0:

                    if ((World.ClientFeatures.Flags & CharacterListFlags.CLF_SAMURAI_NINJA) != 0)
                    {
                        _spellBookType = SpellBookType.Ninjitsu;
                    }

                    break;

                case 0x2D50:
                    _spellBookType = SpellBookType.Spellweaving;

                    break;

                case 0x2D9D:
                    _spellBookType = SpellBookType.Mysticism;

                    break;

                case 0x225A:
                case 0x225B:
                    _spellBookType = SpellBookType.Mastery;

                    break;
            }
        }

        private void PageCornerOnMouseClick(object sender, MouseEventArgs e)
        {
            if (
                e.Button == MouseButtonType.Left
                && Mouse.LDragOffset == Point.Zero
                && sender is Control ctrl
            )
            {
                SetActivePage(
                    ctrl.LocalSerial == 0 ? _dataBox.ActivePage - 1 : _dataBox.ActivePage + 1
                );
            }
        }

        private void PageCornerOnMouseDoubleClick(object sender, MouseDoubleClickEventArgs e)
        {
            if (e.Button == MouseButtonType.Left && sender is Control ctrl)
            {
                SetActivePage(ctrl.LocalSerial == 0 ? 1 : _maxPage);
            }
        }

        public override void OnButtonClick(int buttonID)
        {
            switch ((ButtonCircle)buttonID)
            {
                case ButtonCircle.Circle_1_2:
                    SetActivePage(1);

                    break;

                case ButtonCircle.Circle_3_4:
                    SetActivePage(2);

                    break;

                case ButtonCircle.Circle_5_6:
                    SetActivePage(3);

                    break;

                case ButtonCircle.Circle_7_8:
                    SetActivePage(4);

                    break;
            }
        }

        private enum ButtonCircle
        {
            Circle_1_2,
            Circle_3_4,
            Circle_5_6,
            Circle_7_8
        }

        private class HueGumpPic : GumpPic
        {
            private readonly MacroManager _mm;
            private readonly ushort _spellID;
            private readonly string _spellName;

            /// <summary>
            /// ShowEdit button when user pressing ctrl + alt
            /// </summary>
            private bool ShowEdit =>
                Keyboard.Ctrl && Keyboard.Alt && ProfileManager.CurrentProfile.FastSpellsAssign;

            public HueGumpPic(
                int x,
                int y,
                ushort graphic,
                ushort hue,
                ushort spellID,
                string spellName
            ) : base(x, y, graphic, hue)
            {
                _spellID = spellID;
                _spellName = spellName;

                _mm = Client.Game.GetScene<GameScene>().Macros;
            }

            public override void Update()
            {
                base.Update();

                if (World.ActiveSpellIcons.IsActive(_spellID))
                {
                    Hue = 38;
                }
                else if (Hue != 0)
                {
                    Hue = 0;
                }
            }

            /// <summary>
            /// Overide Draw method to include + icon when ShowEdit is true
            /// </summary>
            /// <param name="batcher"></param>
            /// <param name="x"></param>
            /// <param name="y"></param>
            /// <returns></returns>
            public override bool Draw(UltimaBatcher2D batcher, int x, int y)
            {
                base.Draw(batcher, x, y);

                if (ShowEdit)
                {
                    Vector3 hueVector = ShaderHueTranslator.GetHueVector(0);

                    ref readonly var gumpInfo = ref Client.Game.Gumps.GetGump(0x09CF);

                    if (gumpInfo.Texture != null)
                    {
                        if (
                            UIManager.MouseOverControl != null
                            && (
                                UIManager.MouseOverControl == this
                                || UIManager.MouseOverControl.RootParent == this
                            )
                        )
                        {
                            hueVector.X = 34;
                            hueVector.Y = 1;
                        }
                        else
                        {
                            hueVector.X = 0x44;
                            hueVector.Y = 1;
                        }

                        batcher.Draw(
                            gumpInfo.Texture,
                            new Vector2(x + (Width - gumpInfo.UV.Width), y),
                            gumpInfo.UV,
                            hueVector
                        );
                    }
                }

                return true;
            }

            /// <summary>
            /// On User Click and ShowEdit true we should show them macro editor
            /// </summary>
            /// <param name="x"></param>
            /// <param name="y"></param>
            /// <param name="button"></param>
            protected override void OnMouseUp(int x, int y, MouseButtonType button)
            {
                if (button == MouseButtonType.Left && ShowEdit)
                {
                    Macro mCast = Macro.CreateFastMacro(
                        _spellName,
                        MacroType.CastSpell,
                        (MacroSubType)GetSpellsId() + SpellBookDefinition.GetSpellsGroup(_spellID)
                    );
                    if (_mm.FindMacro(_spellName) == null)
                    {
                        _mm.MoveToBack(mCast);
                    }
                    GameActions.OpenMacroGump(_spellName);
                }
            }

            /// <summary>
            /// Get Spell Id
            /// </summary>
            /// <returns></returns>
            private int GetSpellsId()
            {
                return _spellID % 100;
            }
        }
    }
}
