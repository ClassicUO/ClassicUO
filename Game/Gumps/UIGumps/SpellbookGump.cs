using System;
using System.Collections.Generic;
using System.Linq;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Gumps.Controls;
using ClassicUO.Input;
using ClassicUO.IO;
using ClassicUO.Utility;
using Microsoft.Xna.Framework.Input;

namespace ClassicUO.Game.Gumps.UIGumps
{
    internal class SpellbookGump : Gump
    {
        private readonly Item _spellBook;
        private readonly List<KeyValuePair<int, int>> _spellList = new List<KeyValuePair<int, int>>();

        private Label[] _indexes;

        private int _maxPage;

        private GumpPic _pageCornerLeft, _pageCornerRight;

        private SpellBookType _spellBookType;

        private readonly sbyte[] _spells = new sbyte[64];

        public SpellbookGump(Item item) : base(item.Serial, 0)
        {
            _spellBook = item;
            _spellBook.SetCallbacks(OnEntityUpdate, OnEntityDispose);

            CanMove = true;
            AcceptMouseInput = false;

            OnEntityUpdate(item);
        }

        private void CreateMagery()
        {
            Clear();

            AddChildren(new GumpPic(0, 0, 0x08AC, 0));
            AddChildren(_pageCornerLeft = new GumpPic(50, 8, 0x08BB, 0));
            _pageCornerLeft.LocalSerial = 0;
            _pageCornerLeft.Page = int.MaxValue;
            _pageCornerLeft.MouseClick += PageCornerOnMouseClick;
            _pageCornerLeft.MouseDoubleClick += PageCornerOnMouseDoubleClick;

            AddChildren(_pageCornerRight = new GumpPic(321, 8, 0x08BC, 0));
            _pageCornerRight.LocalSerial = 1;
            _pageCornerRight.Page = 1;
            _pageCornerRight.MouseClick += PageCornerOnMouseClick;
            _pageCornerRight.MouseDoubleClick += PageCornerOnMouseDoubleClick;


            for (int i = 0; i < 4; i++)
            {
                GumpPic circle = new GumpPic(60 + i * 35, 174, (ushort) (0x08B1 + i), 0) {LocalSerial = (uint) i};
                circle.MouseClick += CircleOnMouseClick;
                AddChildren(circle);
            }


            for (int i = 0; i < 4; i++)
            {
                GumpPic circle = new GumpPic(226 + i * 34, 174, (ushort) (0x08B5 + i), 0)
                    {LocalSerial = (uint) (i + 4)};
                circle.MouseClick += CircleOnMouseClick;
                AddChildren(circle);
            }

            for (int i = 0; i < 8; i++)
            {
                AddChildren(new Label("INDEX", false, 0x0288, font: 6)
                    {
                        X = 106 + i % 2 * 163,
                        Y = 10
                    }, 1 + i / 2);
            }

            for (int i = 0; i < 8; i++)
            {
                AddChildren(new Label(SpellsMagery.CircleNames[i], false, 0x0288, font: 6)
                    {
                        X = 62 + i % 2 * 161,
                        Y = 30
                    }, 1 + i / 2);
            }

            _indexes = new Label[64];
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    int index = i * 8 + j;
                    AddChildren(_indexes[index] = new Label(string.Empty, false, 0x0288, font: 9)
                        {
                            X = i % 2 == 0 ? 64 : 225,
                            Y = 52 + 15 * j,
                            AcceptMouseInput = true,
                            LocalSerial = (uint) (index + 1)
                        }, 1 + i / 2);
                }
            }

            _maxPage = 4;


            int totalSpells = 0;
            _spellList.Clear();

            for (int circle = 0; circle < 8; circle++)
            {
                for (int i = 1; i <= 8; i++)
                {
                    if (_spellBook.HasSpell(circle, i))
                    {
                        _spellList.Add(new KeyValuePair<int, int>(circle, i));
                        totalSpells++;
                    }
                }
            }

            _maxPage += (totalSpells + 1) / 2;


            for (int page = 1; page <= _maxPage; page++)
            {
                int currentPage = page;
                int currentSpellCircle = currentPage * 2 - 2;
                int currentSpellInfoIndex = currentPage * 2 - 10;

                for (int currentCol = 0; currentCol < 2; currentCol++)
                {
                    bool isRightPage = currentCol + 1 == 2;
                    currentSpellInfoIndex += currentCol;

                    if (currentPage <= 4)
                    {
                        foreach (KeyValuePair<int, int> spell in _spellList)
                        {
                            if (spell.Key == currentSpellCircle)
                            {
                                int currentSpellInfoPage = _spellList.IndexOf(spell) / 2;
                                int spellIndex = currentSpellCircle * 8 + spell.Value;
                                _indexes[spellIndex - 1].Text = SpellsMagery.GetSpell(spellIndex).Name;
                                _indexes[spellIndex - 1].Tag = 5 + currentSpellInfoPage;

                                _indexes[spellIndex - 1].MouseClick += (sender, e) =>
                                {
                                    SetActivePage((int) _indexes[spellIndex - 1].Tag);
                                };

                                _indexes[spellIndex - 1].MouseDoubleClick += (sender, e) =>
                                {
                                    GumpControl control = (GumpControl) sender;
                                    if (FileManager.ClientVersion < ClientVersions.CV_308Z)
                                        GameActions.CastSpellFromBook((int) control.LocalSerial.Value,
                                            _spellBook.Serial);
                                    else
                                        GameActions.CastSpell((int) control.LocalSerial.Value);
                                };


                                //_indexes[spellIndex - 1].MouseEnter += (sender, e) =>
                                //{
                                //    Label control = (Label)sender;
                                //    control.
                                //};
                                //_indexes[spellIndex - 1].MouseLeft += (sender, e) =>
                                //{
                                //    Label control = (Label)sender;
                                //};
                            }
                        }

                        currentSpellCircle++;
                    }
                    else
                    {
                        if (currentSpellInfoIndex < _spellList.Count)
                            CreateSpellDetailsPage(page, isRightPage, _spellList[currentSpellInfoIndex].Key,
                                SpellsMagery.GetSpell(_spellList[currentSpellInfoIndex].Key * 8 +
                                                      _spellList[currentSpellInfoIndex].Value));
                    }
                }
            }


            SetActivePage(1);
        }

        private void CircleOnMouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButton.Left && sender is GumpControl ctrl)
                SetActivePage((int) ctrl.LocalSerial.Value / 2 + 1);
        }

        private void PageCornerOnMouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButton.Left && sender is GumpControl ctrl)
            {
                SetActivePage(ctrl.LocalSerial == 0 ? ActivePage - 1 : ActivePage + 1);
            }
        }

        private void PageCornerOnMouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButton.Left && sender is GumpControl ctrl)
            {
                SetActivePage(ctrl.LocalSerial == 0 ? 1 : _maxPage);
            }
        }

        private void CreateNecromancy()
        {
            Clear();
            AddChildren(new GumpPic(0, 0, 0x2B00, 0));
            AddChildren(_pageCornerLeft = new GumpPic(50, 8, 0x08BB, 0));
            _pageCornerLeft.LocalSerial = 0;
            _pageCornerLeft.Page = int.MaxValue;
            _pageCornerLeft.MouseClick += PageCornerOnMouseClick;
            _pageCornerLeft.MouseDoubleClick += PageCornerOnMouseDoubleClick;

            AddChildren(_pageCornerRight = new GumpPic(321, 8, 0x08BC, 0));
            _pageCornerRight.LocalSerial = 1;
            _pageCornerRight.Page = 1;
            _pageCornerRight.MouseClick += PageCornerOnMouseClick;
            _pageCornerRight.MouseDoubleClick += PageCornerOnMouseDoubleClick;

            for (int i = 0; i < 8; i++)
            {
                AddChildren(new Label("INDEX", false, 0x0288, font: 6)
                    {
                        X = 106 + i % 2 * 163,
                        Y = 10
                    }, 1 + i / 2);
            }

            _indexes = new Label[17];
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    int index = i * 8 + j;
                    AddChildren(_indexes[index] = new Label(string.Empty, false, 0x0288, font: 9)
                        {
                            X = i % 2 == 0 ? 64 : 225,
                            Y = 52 + 15 * j,
                            AcceptMouseInput = true,
                            LocalSerial = (uint)(index + 1)
                        }, 1 + i / 2);
                }
            }
        }

        private void CreateChivalry()
        {
        }

        private void CreateBushido()
        {
        }

        private void CreateNinjitsu()
        {
        }

        private void CreateSpellweaving()
        {
        }

        private void CreateMysticism()
        {
        }


        private void CreateBook()
        {
            Clear();

            GetBookInfo(_spellBookType, out Graphic bookGraphic, out Graphic minimizedGraphic, out Graphic iconStartGraphic, out int maxSpellsCount, out int spellIndexOffset, out int spellsOnPage, out int dictionaryPagesCount);

            AddChildren(new GumpPic(0, 0, bookGraphic, 0));
            AddChildren(_pageCornerLeft = new GumpPic(50, 8, 0x08BB, 0));
            _pageCornerLeft.LocalSerial = 0;
            _pageCornerLeft.Page = int.MaxValue;
            _pageCornerLeft.MouseClick += PageCornerOnMouseClick;
            _pageCornerLeft.MouseDoubleClick += PageCornerOnMouseDoubleClick;

            AddChildren(_pageCornerRight = new GumpPic(321, 8, 0x08BC, 0));
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
                        _spells[circle * 8 + i - 1] = 1;
                        totalSpells++;
                    }
                }
            }

            _maxPage = 4 + (totalSpells + 1) / 2;

            int offs = 0;

            bool isMageSpellbook = false;

            if (_spellBookType == SpellBookType.Magery)
            {
                isMageSpellbook = true;

                AddChildren(new Button((int)ButtonCircle.Circle_1_2, 0x08B1, 0x08B1) { X = 58, Y = 175, ButtonAction = ButtonAction.Activate, ToPage = 1 });
                AddChildren(new Button((int)ButtonCircle.Circle_1_2, 0x08B2, 0x08B2) { X = 93, Y = 175, ButtonAction = ButtonAction.Activate, ToPage = 1 });
                AddChildren(new Button((int)ButtonCircle.Circle_3_4, 0x08B3, 0x08B3) { X = 130, Y = 175, ButtonAction = ButtonAction.Activate, ToPage = 2 });
                AddChildren(new Button((int)ButtonCircle.Circle_3_4, 0x08B4, 0x08B4) { X = 164, Y = 175, ButtonAction = ButtonAction.Activate, ToPage = 2 });

                AddChildren(new Button((int)ButtonCircle.Circle_5_6, 0x08B5, 0x08B5) { X = 227, Y = 175, ButtonAction = ButtonAction.Activate, ToPage = 3 });
                AddChildren(new Button((int)ButtonCircle.Circle_5_6, 0x08B6, 0x08B6) { X = 260, Y = 175, ButtonAction = ButtonAction.Activate, ToPage = 3 });
                AddChildren(new Button((int)ButtonCircle.Circle_7_8, 0x08B7, 0x08B7) { X = 297, Y = 175, ButtonAction = ButtonAction.Activate, ToPage = 4 });
                AddChildren(new Button((int)ButtonCircle.Circle_7_8, 0x08B8, 0x08B8) { X = 332, Y = 175, ButtonAction = ButtonAction.Activate, ToPage = 4 });
            }

            for (int i = 1; i <= dictionaryPagesCount / 2; i++)
            {
                int page = i;

                for (int j = 0; j < 2; j++)
                {
                    if (page == 0 && _spellBookType == SpellBookType.Chivalry)
                    {
                        Label label = new Label("Tithing points\nAvailable: " + World.Player.TithingPoints, false, 0x0288, font: 6) {X = 62, Y = 162};
                        AddChildren(label, page);
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

                    Label text = new Label("INDEX", false, 0x0288, font: 6) {X = indexX, Y = 10};
                    AddChildren(text, page);

                    if (isMageSpellbook)
                    {
                        text = new Label(SpellsMagery.CircleNames[(i - 1) * 2 + j % 2], false, 0x0288, font: 6) {X = dataX, Y = 30};
                        AddChildren(text, page);
                    }

                    for (int k = 0; k < spellsOnPage; k++)
                    {
                        if (_spells[i] > 0)
                        {
                            SpellDefinition spell = SpellsMagery.GetSpell(offs + 1);
                            text = new Label(spell.Name, false, 0x0288, font: 9)
                            {
                                X = dataX,
                                Y = 52 + y,
                                LocalSerial = (uint)(4 + (offs / 2) + 1),
                                AcceptMouseInput = true
                            };
                            text.MouseClick += (sender, e) =>
                            {
                                Label l = (Label) sender;
                                SetActivePage((int)l.LocalSerial.Value);
                            };

                            AddChildren(text, page);

                            y += 15;
                        }

                        offs++;
                    }
                }
            }

            int page1 = dictionaryPagesCount / 2;

            int topTextY = (_spellBookType == SpellBookType.Magery ? 10 : 6);
            bool haveReagents = (_spellBookType <= SpellBookType.Necromancy);
            bool haveAbbreviature = _spellBookType != SpellBookType.Bushido && _spellBookType != SpellBookType.Ninjitsu;

            for (int i = 0; i < maxSpellsCount; i++)
            {
                if (_spells[i] <= 0)
                    continue;

                int iconX = 62;
                int topTextX = 87;
                int iconTextX = 112;

                uint iconSerial = 100 + (uint) i;

                if (i % 2 != 0)
                {
                    iconX = 225;
                    topTextX = 244;
                    iconTextX = 275;
                    iconSerial = 1000 + (uint) i;
                }
                else
                {
                    page1++;
                }

                GetSpellNames(i, out string name, out string abbreviature, out string reagents);

                if (isMageSpellbook)
                {
                    Label text = new Label(SpellsMagery.CircleNames[i / 8], false, 0x0288, font: 6)
                    {
                        X = topTextX,
                        Y = topTextY
                    };
                    AddChildren(text, page1);

                    text = new Label(name, false, 0x0288, 80, 6)
                    {
                        X = iconTextX, Y = 34
                    };
                    AddChildren(text, page1);

                    int abbreviatureY = 26;

                    if (text.Height < 24)
                        abbreviatureY = 31;

                    abbreviatureY += text.Height;

                    text = new Label(abbreviature, false, 0x0288, font: 8)
                    {
                        X = iconTextX,
                        Y = abbreviatureY
                    };
                    AddChildren(text, page1);
                }
                else
                {
                    Label text = new Label(name, false, 0x0288, font: 6)
                    {
                        X = topTextX,
                        Y = topTextY
                    };
                    AddChildren(text, page1);

                    if (haveAbbreviature)
                    {
                        text = new Label(abbreviature, false, 0x0288, 80, 6)
                        {
                            X = iconTextX,
                            Y = 34
                        };
                        AddChildren(text, page1);
                    }
                }


                GumpPic icon = new GumpPic(iconX, 40, (Graphic)(iconStartGraphic + i), 0)
                {
                    X = iconX,
                    Y = 40,
                    LocalSerial = iconSerial
                };
                icon.DragBegin += (sender, e) =>
                {
                    GumpControl ctrl = (GumpControl)sender;
                    SpellDefinition def = SpellsMagery.GetSpell( (int)(ctrl.LocalSerial > 1000 ? ctrl.LocalSerial - 1000 : ctrl.LocalSerial - 100) + 1);
                    UseSpellButtonGump gump = new UseSpellButtonGump(def)
                    {
                        X = UIManager.InputManager.MousePosition.X - 22,
                        Y = UIManager.InputManager.MousePosition.Y - 22
                    };
                    UIManager.Add(gump);
                    UIManager.AttemptDragControl(gump, UIManager.InputManager.MousePosition, true);
                };
                AddChildren(icon, page1);

                if (haveReagents)
                {
                    AddChildren(new GumpPicTiled(iconX, 88, 120, 5, 0x0835), page1);

                    Label text = new Label("Reagents:", false, 0x0288, font: 6)
                    {
                        X = iconX,
                        Y = 92
                    };
                    AddChildren(text, page1);

                    text = new Label(reagents, false, 0x0288, font: 9)
                    {
                        X = iconX, 
                        Y = 114
                    };
                    AddChildren(text, page1);
                }

                if (!isMageSpellbook)
                {
                    GetSpellRequires(i, out int requiriesY, out string requires);

                    Label text = new Label(requires, false, 0x0288, font: 6)
                    {
                        X =iconX,
                        Y = requiriesY
                    };
                    AddChildren(text, page1);
                }
            }

            SetActivePage(1);
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
                    maxSpellsCount = 30;
                    bookGraphic = 0;
                    minimizedGraphic = 0;
                    iconStartGraphic = 0;
                    spellIndexOffset = 0;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }

            spellsOnPage = Math.Min(maxSpellsCount / 2, 8);
            dictionaryPagesCount = (int) Math.Ceiling(maxSpellsCount / 8.0f);

            if (dictionaryPagesCount % 2 != 0)
                dictionaryPagesCount++;
        }

        private void GetSpellNames(int offset, out string name, out string abbreviature, out string reagents)
        {
            switch (_spellBookType)
            {
                case SpellBookType.Magery:
                    SpellDefinition def = SpellsMagery.GetSpell(offset +1);
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
                //case SpellBookType.Chivalry:
                //    break;
                //case SpellBookType.Bushido:
                //    break;
                //case SpellBookType.Ninjitsu:
                //    break;
                //case SpellBookType.Spellweaving:
                //    break;
                //case SpellBookType.Mysticism:
                //    break;
                //case SpellBookType.Unknown:
                //    break;
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
                    int[] req = SpellsNecromancy.SpellsRequires[offset];
                    manaCost = req[0];
                    minSkill = req[1];
                    break;
                case SpellBookType.Chivalry:
                    break;
                case SpellBookType.Bushido:
                    break;
                case SpellBookType.Ninjitsu:
                    break;
                case SpellBookType.Spellweaving:
                    break;
            }

            text = $"Mana cost: {manaCost}\nMin. Skill: {minSkill}";
        }

        private void SetActivePage(int page)
        {
            if (page < 1)
                page = 1;
            else if (page > _maxPage)
                page = _maxPage;

            ActivePage = page;
            _pageCornerLeft.Page = ActivePage != 1 ? 0 : int.MaxValue;
            _pageCornerRight.Page = ActivePage != _maxPage ? 0 : int.MaxValue;
        }

        private void CreateSpellDetailsPage(int page, bool isright, int circle, SpellDefinition spell)
        {
            if (_spellBookType == SpellBookType.Magery)
                AddChildren(
                    new Label(SpellsMagery.CircleNames[circle], false, 0x0288, font: 6)
                        {X = isright ? 64 + 162 : 85, Y = 10}, page);

            GumpPic spellImage = new GumpPic(isright ? 225 : 62, 40, (Graphic) (spell.GumpIconID - 0x1298), 0)
            {
                LocalSerial = (uint)(Graphic)(spell.GumpIconID - 0x1298),
                Tag = spell.ID
            };

            spellImage.DragBegin += (sender, e) =>
            {
                GumpControl ctrl = (GumpControl)sender;
                SpellDefinition def = SpellsMagery.GetSpell((int) ctrl.Tag);
                UseSpellButtonGump gump = new UseSpellButtonGump(def)
                {
                    X = UIManager.InputManager.MousePosition.X - 22, 
                    Y = UIManager.InputManager.MousePosition.Y - 22
                };
                UIManager.Add(gump);
                UIManager.AttemptDragControl(gump, UIManager.InputManager.MousePosition, true);
            };
       
            AddChildren(spellImage, page);

            Label spellnameLabel = new Label(spell.Name, false, 0x0288, 80, 6) {X = isright ? 275 : 112, Y = 34};
            AddChildren(spellnameLabel, page);

            if (spell.Regs.Length > 0)
            {
                AddChildren(new GumpPicTiled(isright ? 225 : 62, 88, 120, 4, 0x0835), page);
                AddChildren(new Label("Reagents:", false, 0x0288, font: 6) {X = isright ? 225 : 62, Y = 92}, page);

                string reagList = spell.CreateReagentListString(",\n");

                if (_spellBookType == SpellBookType.Magery)
                {
                    int y = spellnameLabel.Height < 24 ? 31 : 24;
                    y += spellnameLabel.Height;
                    AddChildren(new Label(SpellsMagery.SpecialReagentsChars[spell.ID - 1][1], false, 0x0288, font: 8) { X = isright ? 275 : 112, Y = y }, page);
                }
                
                AddChildren(new Label(reagList, false, 0x0288, font: 9) {X = isright ? 225 : 62, Y = 114}, page);
            }
        }

        private void OnEntityUpdate(Entity entity)
        {
            switch (entity.Graphic)
            {
                default:
                case 0x0EFA:
                    _spellBookType = SpellBookType.Magery;
                    //CreateMagery();
                    break;
                case 0x2253:
                    _spellBookType = SpellBookType.Necromancy;
                    //CreateNecromancy();
                    break;
                case 0x2252:
                    _spellBookType = SpellBookType.Chivalry;
                    //CreateChivalry();
                    break;
                case 0x238C:
                    _spellBookType = SpellBookType.Bushido;
                    //CreateBushido();
                    break;
                case 0x23A0:
                    _spellBookType = SpellBookType.Ninjitsu;
                    //CreateNinjitsu();
                    break;
                case 0x2D50:
                    _spellBookType = SpellBookType.Spellweaving;
                    //CreateSpellweaving();
                    break;
            }

            CreateBook();
        }

        private void OnEntityDispose(Entity entity)
        {
            Dispose();
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

        enum ButtonCircle
        {
            Circle_1_2,
            Circle_3_4,
            Circle_5_6,
            Circle_7_8
        }
    }
}