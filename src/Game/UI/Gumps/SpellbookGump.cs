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

using System;
using System.IO;

using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;

namespace ClassicUO.Game.UI.Gumps
{
    internal class SpellbookGump : Gump
    {
        private readonly Item _spellBook;
        private readonly bool[] _spells = new bool[64];
        private int _maxPage;
        private GumpPic _pageCornerLeft, _pageCornerRight;
        private SpellBookType _spellBookType;
        private Control _lastPressed;
        private float _clickTiming = 0;

        public SpellbookGump(Item item) : this()
        {
            _spellBook = item;
            BuildGump();
        }

        public SpellbookGump() : base(0, 0)
        {
            CanMove = true;
            AcceptMouseInput = false;
            CanBeSaved = true;
        }

        public override void Save(BinaryWriter writer)
        {
            base.Save(writer);
            writer.Write(_spellBook.Serial);
        }

        public override void Restore(BinaryReader reader)
        {
            base.Restore(reader);
            Engine.SceneManager.GetScene<GameScene>().DoubleClickDelayed(reader.ReadUInt32());
            Dispose();
        }

        private void BuildGump()
        {
            LocalSerial = _spellBook.Serial;
            _spellBook.Items.Added += ItemsOnAdded;
            _spellBook.Items.Removed += ItemsOnRemoved;
            _spellBook.Disposed += SpellBookOnDisposed;

            OnEntityUpdate(_spellBook);

            Engine.SceneManager.CurrentScene.Audio.PlaySound(0x0055);
        }

        private void SpellBookOnDisposed(object sender, EventArgs e)
        {          
            Dispose();
        }

        public override void Dispose()
        {
            _spellBook.Items.Added -= ItemsOnAdded;
            _spellBook.Items.Removed -= ItemsOnRemoved;
            _spellBook.Disposed -= SpellBookOnDisposed;

            Engine.SceneManager.CurrentScene.Audio.PlaySound(0x0055);
            Engine.UI.SavePosition(LocalSerial, Location);
            base.Dispose();
        }

        private void ItemsOnRemoved(object sender, CollectionChangedEventArgs<Item> e)
        {
            OnEntityUpdate(_spellBook);
        }

        private void ItemsOnAdded(object sender, CollectionChangedEventArgs<Item> e)
        {
            OnEntityUpdate(_spellBook);
        }

        private void CreateBook()
        {
            Clear();
            _pageCornerLeft = _pageCornerRight = null;
            GetBookInfo(_spellBookType, out Graphic bookGraphic, out Graphic minimizedGraphic, out Graphic iconStartGraphic, out int maxSpellsCount, out int spellIndexOffset, out int spellsOnPage, out int dictionaryPagesCount);
            Add(new GumpPic(0, 0, bookGraphic, 0));
            Add(_pageCornerLeft = new GumpPic(50, 8, 0x08BB, 0));
            _pageCornerLeft.LocalSerial = 0;
            _pageCornerLeft.Page = int.MaxValue;
            _pageCornerLeft.MouseClick += PageCornerOnMouseClick;
            _pageCornerLeft.MouseDoubleClick += PageCornerOnMouseDoubleClick;
            Add(_pageCornerRight = new GumpPic(321, 8, 0x08BC, 0));
            _pageCornerRight.LocalSerial = 1;
            _pageCornerRight.Page = 1;
            _pageCornerRight.MouseClick += PageCornerOnMouseClick;
            _pageCornerRight.MouseDoubleClick += PageCornerOnMouseDoubleClick;
            int totalSpells = 0;

            for (int circle = 0; circle < 8; circle++)
            {
                for (int i = 1; i <= 8; i++)
                {
                    if (_spellBook.HasSpell(circle, i))
                    {
                        _spells[circle * 8 + i - 1] = true;
                        totalSpells++;
                    }
                }
            }

            _maxPage = (dictionaryPagesCount >> 1) + ((totalSpells + 1) >> 1);

            int offs = 0;
            bool isMageSpellbook = false;

            if (_spellBookType == SpellBookType.Magery)
            {
                isMageSpellbook = true;

                Add(new Button((int)ButtonCircle.Circle_1_2, 0x08B1, 0x08B1)
                {
                    X = 58, Y = 175, ButtonAction = ButtonAction.Activate, ToPage = 1
                });

                Add(new Button((int)ButtonCircle.Circle_1_2, 0x08B2, 0x08B2)
                {
                    X = 93, Y = 175, ButtonAction = ButtonAction.Activate, ToPage = 1
                });

                Add(new Button((int)ButtonCircle.Circle_3_4, 0x08B3, 0x08B3)
                {
                    X = 130, Y = 175, ButtonAction = ButtonAction.Activate, ToPage = 2
                });

                Add(new Button((int)ButtonCircle.Circle_3_4, 0x08B4, 0x08B4)
                {
                    X = 164, Y = 175, ButtonAction = ButtonAction.Activate, ToPage = 2
                });

                Add(new Button((int)ButtonCircle.Circle_5_6, 0x08B5, 0x08B5)
                {
                    X = 227, Y = 175, ButtonAction = ButtonAction.Activate, ToPage = 3
                });

                Add(new Button((int)ButtonCircle.Circle_5_6, 0x08B6, 0x08B6)
                {
                    X = 260, Y = 175, ButtonAction = ButtonAction.Activate, ToPage = 3
                });

                Add(new Button((int)ButtonCircle.Circle_7_8, 0x08B7, 0x08B7)
                {
                    X = 297, Y = 175, ButtonAction = ButtonAction.Activate, ToPage = 4
                });

                Add(new Button((int)ButtonCircle.Circle_7_8, 0x08B8, 0x08B8)
                {
                    X = 332, Y = 175, ButtonAction = ButtonAction.Activate, ToPage = 4
                });
            }

            int spellDone = 0;
            for (int i = 1; i <= (dictionaryPagesCount >> 1); i++)
            {
                int page = i;

                for (int j = 0; j < 2; j++)
                {
                    if (page == 1 && _spellBookType == SpellBookType.Chivalry)
                    {
                        Label label = new Label("Tithing points\nAvailable: " + World.Player.TithingPoints, false, 0x0288, font: 6)
                        {
                            X = 62, Y = 162
                        };
                        Add(label, page);
                    }

                    int indexX = 106;
                    int dataX = 62;
                    int y = 0;
                    uint spellSerial = 100;

                    if (j % 2 != 0)
                    {
                        indexX = 269;
                        dataX = 225;
                        spellSerial = 1000;
                    }

                    Label text = new Label("INDEX", false, 0x0288, font: 6)
                    {
                        X = indexX, Y = 10
                    };
                    Add(text, page);
                    if (isMageSpellbook)
                    {
                        text = new Label(SpellsMagery.CircleNames[(i - 1) * 2 + j % 2], false, 0x0288, font: 6)
                        {
                            X = dataX, Y = 30
                        };
                        Add(text, page);
                    }

                    int topage = (dictionaryPagesCount >> 1) + ((spellDone + 1) >> 1);

                    for (int k = 0; k < spellsOnPage; k++)
                    {
                        if (_spells[offs])
                        {
                            GetSpellNames(offs, out string name, out string abbreviature, out string reagents);
                            if (spellDone % 2 == 0)
                            {
                                topage++;
                            }

                            spellDone++;

                            text = new HoveredLabel(name, false, 0x0288, 0x33, font: 9)
                            {
                                X = dataX, Y = 52 + y, LocalSerial = (uint)topage, AcceptMouseInput = true, Tag = offs + 1
                            };

                            text.MouseClick += OnClicked;
                            text.MouseDoubleClick += OnDoubleClicked;
                            Add(text, page);
                            y += 15;
                        }

                        offs++;
                    }
                }
            }

            int page1 = (dictionaryPagesCount >> 1) + 1;
            int topTextY = _spellBookType == SpellBookType.Magery ? 10 : 6;
            bool haveReagents = _spellBookType <= SpellBookType.Necromancy;
            bool haveAbbreviature = _spellBookType != SpellBookType.Bushido && _spellBookType != SpellBookType.Ninjitsu;

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
                        topTextX = 244 - 20;
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

                if (isMageSpellbook)
                {
                    Label text = new Label(SpellsMagery.CircleNames[i >> 3], false, 0x0288, font: 6)
                    {
                        X = topTextX, Y = topTextY
                    };
                    Add(text, page1);

                    text = new Label(name, false, 0x0288, 80, 6)
                    {
                        X = iconTextX, Y = 34
                    };
                    Add(text, page1);
                    int abbreviatureY = 26;

                    if (text.Height < 24)
                        abbreviatureY = 31;
                    abbreviatureY += text.Height;

                    text = new Label(abbreviature, false, 0x0288, font: 8)
                    {
                        X = iconTextX, Y = abbreviatureY
                    };
                    Add(text, page1);
                }
                else
                {
                    Label text = new Label(name, false, 0x0288, font: 6)
                    {
                        X = topTextX, Y = topTextY
                    };
                    Add(text, page1);

                    if (haveAbbreviature)
                    {
                        text = new Label(abbreviature, false, 0x0288, 80, 6)
                        {
                            X = iconTextX, Y = 34
                        };
                        Add(text, page1);
                    }
                }

                GumpPic icon = new GumpPic(iconX, 40, (Graphic)(iconStartGraphic + i), 0)
                {
                    X = iconX, Y = 40, LocalSerial = iconSerial
                };

                icon.MouseDoubleClick += (sender, e) => {
                    if (e.Button == MouseButton.Left)
                    {
                        SpellDefinition? def = GetSpellDefinition(sender);
                        if (def != null)
                            GameActions.CastSpell(def.Value.ID);
                    }
                };

                icon.DragBegin += (sender, e) =>
                {
                    SpellDefinition? def = GetSpellDefinition(sender);

                    UseSpellButtonGump gump = new UseSpellButtonGump(def.Value)
                    {
                        X = Mouse.Position.X - 22, Y = Mouse.Position.Y - 22
                    };

                    Engine.UI.Add(gump);
                    Engine.UI.AttemptDragControl(gump, Mouse.Position, true);
                };

                Add(icon, page1);

                if (haveReagents)
                {
                    Add(new GumpPicTiled(iconX, 88, 120, 5, 0x0835), page1);

                    Label text = new Label("Reagents:", false, 0x0288, font: 6)
                    {
                        X = iconX, Y = 92
                    };
                    Add(text, page1);

                    text = new Label(reagents, false, 0x0288, font: 9)
                    {
                        X = iconX, Y = 114
                    };
                    Add(text, page1);
                }

                if (!isMageSpellbook)
                {
                    GetSpellRequires(i, out int requiriesY, out string requires);

                    Label text = new Label(requires, false, 0x0288, font: 6)
                    {
                        X = iconX, Y = requiriesY
                    };
                    Add(text, page1);
                }
            }

            SetActivePage(1);
        }

        private SpellDefinition? GetSpellDefinition(object sender)
        {
            Control ctrl = (Control)sender;
            int idx = (int)(ctrl.LocalSerial > 1000 ? ctrl.LocalSerial - 1000 : ctrl.LocalSerial - 100) + 1;
            SpellDefinition? def = null;

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

                default:
                    throw new ArgumentOutOfRangeException();
            }
            return def;
        }

        private void GetBookInfo(SpellBookType type, out Graphic bookGraphic, out Graphic minimizedGraphic, out Graphic iconStartGraphic, out int maxSpellsCount, out int spellIndexOffset, out int spellsOnPage, out int dictionaryPagesCount)
        {
            switch (type)
            {
                case SpellBookType.Magery:
                    maxSpellsCount = 64;
                    bookGraphic = 0x08AC;
                    minimizedGraphic = 0x08BA;
                    iconStartGraphic = 0x08C0;
                    spellIndexOffset = 0;
                    break;

                case SpellBookType.Necromancy:
                    maxSpellsCount = 17;
                    bookGraphic = 0x2B00;
                    minimizedGraphic = 0x2B03;
                    iconStartGraphic = 0x5000;
                    spellIndexOffset = 64;
                    break;

                case SpellBookType.Chivalry:
                    maxSpellsCount = 10;
                    bookGraphic = 0x2B01;
                    minimizedGraphic = 0x2B04;
                    iconStartGraphic = 0x5100;
                    spellIndexOffset = 81;
                    break;

                case SpellBookType.Bushido:
                    maxSpellsCount = 6;
                    bookGraphic = 0x2B07;
                    minimizedGraphic = 0x2B09;
                    iconStartGraphic = 0x5400;
                    spellIndexOffset = 91;
                    break;

                case SpellBookType.Ninjitsu:
                    maxSpellsCount = 8;
                    bookGraphic = 0x2B06;
                    minimizedGraphic = 0x2B08;
                    iconStartGraphic = 0x5300;
                    spellIndexOffset = 97;
                    break;

                case SpellBookType.Spellweaving:
                    maxSpellsCount = 16;
                    bookGraphic = 0x2B2F;
                    minimizedGraphic = 0x2B2D;
                    iconStartGraphic = 0x59D8;
                    spellIndexOffset = 105;
                    break;

                case SpellBookType.Mysticism:
                    maxSpellsCount = 16;
                    bookGraphic = 0x2B32;
                    minimizedGraphic = 0; // TODO: i dunno
                    iconStartGraphic = 0x5DC0;
                    spellIndexOffset = 121;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }

            spellsOnPage = Math.Min(maxSpellsCount >> 1, 8);
            dictionaryPagesCount = (int) Math.Ceiling(maxSpellsCount / 8.0f);

            if (dictionaryPagesCount % 2 != 0)
                dictionaryPagesCount++;
        }

        private void GetSpellNames(int offset, out string name, out string abbreviature, out string reagents)
        {
            switch (_spellBookType)
            {
                case SpellBookType.Magery:
                    SpellDefinition def = SpellsMagery.GetSpell(offset + 1);
                    name = def.Name;
                    abbreviature = SpellsMagery.SpecialReagentsChars[offset][1];
                    reagents = def.CreateReagentListString("\n");
                    break;

                case SpellBookType.Necromancy:
                    def = SpellsNecromancy.GetSpell(offset + 1);
                    name = def.Name;
                    abbreviature = SpellsNecromancy.SpellsSpecialsName[offset][1];
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

                default:
                    throw new ArgumentOutOfRangeException();
            }
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
            }

            text = $"Mana cost: {manaCost}\nMin. Skill: {minSkill}";
        }

        private void SetActivePage(int page)
        {
            if (page == ActivePage)
                return;

            if (page < 1)
                page = 1;
            else if (page > _maxPage)
                page = _maxPage;

            ActivePage = page;
            _pageCornerLeft.Page = ActivePage != 1 ? 0 : int.MaxValue;
            _pageCornerRight.Page = ActivePage != _maxPage ? 0 : int.MaxValue;

            Engine.SceneManager.CurrentScene.Audio.PlaySound(0x0055);
        }

        private void CreateSpellDetailsPage(int page, bool isright, int circle, SpellDefinition spell)
        {
            if (_spellBookType == SpellBookType.Magery)
            {
                Add(new Label(SpellsMagery.CircleNames[circle], false, 0x0288, font: 6)
                {
                    X = isright ? 64 + 162 : 85, Y = 10
                }, page);
            }

            GumpPic spellImage = new GumpPic(isright ? 225 : 62, 40, (Graphic) (spell.GumpIconID - 0x1298), 0)
            {
                LocalSerial = (uint) (Graphic) (spell.GumpIconID - 0x1298), Tag = spell.ID
            };

            spellImage.DragBegin += (sender, e) =>
            {
                Control ctrl = (Control) sender;
                SpellDefinition def = SpellsMagery.GetSpell((int) ctrl.Tag);

                UseSpellButtonGump gump = new UseSpellButtonGump(def)
                {
                    X = Mouse.Position.X - 22, Y = Mouse.Position.Y - 22
                };
                Engine.UI.Add(gump);
                Engine.UI.AttemptDragControl(gump, Mouse.Position, true);
            };
            Add(spellImage, page);

            Label spellnameLabel = new Label(spell.Name, false, 0x0288, 80, 6)
            {
                X = isright ? 275 : 112, Y = 34
            };
            Add(spellnameLabel, page);

            if (spell.Regs.Length > 0)
            {
                Add(new GumpPicTiled(isright ? 225 : 62, 88, 120, 4, 0x0835), page);

                Add(new Label("Reagents:", false, 0x0288, font: 6)
                {
                    X = isright ? 225 : 62, Y = 92
                }, page);
                string reagList = spell.CreateReagentListString(",\n");

                if (_spellBookType == SpellBookType.Magery)
                {
                    int y = spellnameLabel.Height < 24 ? 31 : 24;
                    y += spellnameLabel.Height;

                    Add(new Label(SpellsMagery.SpecialReagentsChars[spell.ID - 1][1], false, 0x0288, font: 8)
                    {
                        X = isright ? 275 : 112, Y = y
                    }, page);
                }

                Add(new Label(reagList, false, 0x0288, font: 9)
                {
                    X = isright ? 225 : 62, Y = 114
                }, page);
            }
        }

        private void OnEntityUpdate(Entity entity)
        {
            switch (entity.Graphic)
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
                    _spellBookType = SpellBookType.Bushido;
                    break;

                case 0x23A0:
                    _spellBookType = SpellBookType.Ninjitsu;
                    break;

                case 0x2D50:
                    _spellBookType = SpellBookType.Spellweaving;
                    break;

                case 0x2D9D:
                    _spellBookType = SpellBookType.Mysticism;
                    break;
            }

            CreateBook();
        }

        private void OnClicked(object sender, MouseEventArgs e)
        {
            if(sender is HoveredLabel l && e.Button == MouseButton.Left)
            {
                _clickTiming += Mouse.MOUSE_DELAY_DOUBLE_CLICK;
                if (_clickTiming > 0)
                    _lastPressed = l;
            }
        }

        private void OnDoubleClicked(object sender, MouseDoubleClickEventArgs e)
        {
            if(_lastPressed != null && e.Button == MouseButton.Left)
            {
                _clickTiming = -Mouse.MOUSE_DELAY_DOUBLE_CLICK;
                GameActions.CastSpell((int)_lastPressed.Tag);
                _lastPressed = null;
            }
        }

        public override void Update(double totalMS, double frameMS)
        {
            base.Update(totalMS, frameMS);
            if(_lastPressed!=null)
            {
                _clickTiming -= (float)frameMS;
                if(_clickTiming <= 0)
                {
                    _clickTiming = 0;
                    SetActivePage((int)_lastPressed.LocalSerial.Value);
                    _lastPressed = null;
                }
            }
        }

        public void Update()
        {
            OnEntityUpdate(_spellBook);
        }

        private void PageCornerOnMouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButton.Left && sender is Control ctrl) SetActivePage(ctrl.LocalSerial == 0 ? ActivePage - 1 : ActivePage + 1);
        }

        private void PageCornerOnMouseDoubleClick(object sender, MouseDoubleClickEventArgs e)
        {
            if (e.Button == MouseButton.Left && sender is Control ctrl) SetActivePage(ctrl.LocalSerial == 0 ? 1 : _maxPage);
        }

        public override void OnButtonClick(int buttonID)
        {
            switch ((ButtonCircle) buttonID)
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
    }
}