// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Assets;
using ClassicUO.Renderer;
using ClassicUO.Resources;
using Microsoft.Xna.Framework;

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

        public SpellbookGump(World world, uint item) : this(world)
        {
            LocalSerial = item;

            BuildGump();
        }

        public SpellbookGump(World world) : base(world, 0, 0)
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

            // Determine spell ID offset for custom spellbooks
            int spellIdOffset = GetSpellIdOffset(_spellBookType);

            for (LinkedObject i = item.Items; i != null; i = i.Next)
            {
                Item spell = (Item)i;
                int currentCount = spell.Amount;

                // Normalize spell ID by subtracting offset (e.g., 1000-1031 becomes 0-31)
                int normalizedId = currentCount - spellIdOffset;

                if (normalizedId >= 0 && normalizedId < maxSpellsCount)
                {
                    _spells[normalizedId] = true;
                    totalSpells++;
                }
            }

            int pagesToFill =
                _spellBookType == SpellBookType.Mastery
                    ? dictionaryPagesCount
                    : dictionaryPagesCount >> 1;

            _maxPage = pagesToFill + ((totalSpells + 1) >> 1);

            int currentSpellIndex = 0;

            if (_spellBookType == SpellBookType.Magery || IsVystiaSpellbook())
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
                                                        Client.Game.UO.FileManager.Clilocs.GetString(
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

                    if (_spellBookType == SpellBookType.Magery || IsVystiaSpellbook())
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

                // Calculate iconSerial based on spellbook type
                // For Vystia spells, iconSerial = spellOffset + normalizedIndex
                // Example: First spell (i=0), spellOffset=1000 → iconSerial=1000 (Frost Touch)
                int spellOffset = GetSpellIdOffset(_spellBookType);
                uint iconSerial = (uint)(spellOffset + i);
                Console.WriteLine($"[SPELLBOOK] Creating icon - i: {i}, spellOffset: {spellOffset}, iconSerial: {iconSerial}");

                if (spellsDone > 0)
                {
                    if (spellsDone % 2 != 0)
                    {
                        iconX = 225;
                        topTextX = 224;
                        iconTextX = 275;
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

                if (_spellBookType == SpellBookType.Mastery)
                {
                    iconGraphic = (ushort)SpellsMastery.GetSpell(i + 1).GumpIconID;

                    toolTipCliloc = i >= 0 && i < 6 ? 1115689 : 1155938 - 6;
                }
                else
                {
                    iconGraphic = (ushort)(iconStartGraphic + i);
                    GetSpellToolTip(out toolTipCliloc);
                }

                var spellDef = GetSpellDefinition(iconSerial);
                HueGumpPic icon = new HueGumpPic(
                    this,
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
                    string tooltip = Client.Game.UO.FileManager.Clilocs.GetString(toolTipCliloc + i);
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
                uint iconSerial = (sender as Control).LocalSerial;
                SpellDefinition def = GetSpellDefinition(iconSerial);

                Console.WriteLine($"[SPELLBOOK] OnIconDoubleClick - IconSerial: {iconSerial}, SpellbookType: {_spellBookType}, SpellbookSerial: {LocalSerial}");

                if (def != null)
                {
                    Console.WriteLine($"[SPELLBOOK] Spell found - ID: {def.ID}, Name: {def.Name}");
                    Console.WriteLine($"[SPELLBOOK] Calling CastSpellFromBook(spellID: {def.ID}, bookSerial: {LocalSerial})");
                    GameActions.CastSpellFromBook(def.ID, LocalSerial);
                }
                else
                {
                    Console.WriteLine($"[SPELLBOOK] ERROR: GetSpellDefinition returned null for iconSerial {iconSerial}");
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

            UseSpellButtonGump gump = new UseSpellButtonGump(World, def)
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
            int idx;

            // For Vystia spells (1000+), icon serial IS the server spell ID
            // We need to convert it to dictionary key (1-based) by subtracting baseOffset and adding 1
            if (serial >= 1000)
            {
                int baseOffset = GetSpellIdOffset(_spellBookType);
                // serial is the server spell ID (e.g., 1000, 1001, 1002...)
                // Dictionary keys are 1-based (1, 2, 3...)
                // So: idx = (serverID - baseOffset) + 1
                // Example: serial=1000, baseOffset=1000 → idx = (1000-1000)+1 = 1 ✓
                idx = (int)(serial - baseOffset) + 1;
            }
            else if (serial >= 100)
            {
                idx = (int)(serial - 100) + 1;
            }
            else
            {
                idx = (int)serial + 1;
            }

            Console.WriteLine($"[SPELLBOOK] GetSpellDefinition(serial: {serial}) -> idx: {idx}, type: {_spellBookType}");
            SpellDefinition def = GetSpellDefinition(idx);
            Console.WriteLine($"[SPELLBOOK] GetSpellDefinition(idx: {idx}) returned: {(def != null ? $"{def.Name} (ID: {def.ID})" : "NULL")}");
            return def;
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

                case SpellBookType.VystiaIceMagic:
                    def = SpellsVystiaIceMagic.GetSpell(idx);

                    break;

                case SpellBookType.VystiaDruid:
                    def = SpellsVystiaNature.GetSpell(idx);

                    break;

                case SpellBookType.VystiaWitch:
                    def = SpellsVystiaHex.GetSpell(idx);

                    break;

                case SpellBookType.VystiaSorcerer:
                    def = SpellsVystiaElemental.GetSpell(idx);

                    break;

                case SpellBookType.VystiaWarlock:
                    def = SpellsVystiaDark.GetSpell(idx);

                    break;

                case SpellBookType.VystiaOracle:
                    def = SpellsVystiaDivination.GetSpell(idx);

                    break;

                case SpellBookType.VystiaNecromancer:
                    def = SpellsVystiaNecromancy.GetSpell(idx);

                    break;

                case SpellBookType.VystiaSummoner:
                    def = SpellsVystiaSummoning.GetSpell(idx);

                    break;

                case SpellBookType.VystiaShaman:
                    def = SpellsVystiaShamanic.GetSpell(idx);

                    break;

                case SpellBookType.VystiaBard:
                    def = SpellsVystiaBardic.GetSpell(idx);

                    break;

                case SpellBookType.VystiaSongweaving:
                    def = SpellsVystiaSongweaving.GetSpell(idx);

                    break;

                case SpellBookType.VystiaEnchanter:
                    def = SpellsVystiaEnchanting.GetSpell(idx);

                    break;

                case SpellBookType.VystiaIllusionist:
                    def = SpellsVystiaIllusion.GetSpell(idx);

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

                // Vystia Ice Magic
                case SpellBookType.VystiaIceMagic:
                    maxSpellsCount = SpellsVystiaIceMagic.MaxSpellCount;
                    bookGraphic = 0x08AC; // Reuse Magery book graphic for display
                    minimizedGraphic = 0x08BA;
                    iconStartGraphic = 0x08C0; // Reuse Magery icons for now

                    break;

                case SpellBookType.VystiaDruid:
                    maxSpellsCount = SpellsVystiaNature.MaxSpellCount;
                    bookGraphic = 0x08AC;
                    minimizedGraphic = 0x08BA;
                    iconStartGraphic = 0x08C0;

                    break;

                case SpellBookType.VystiaWitch:
                    maxSpellsCount = SpellsVystiaHex.MaxSpellCount;
                    bookGraphic = 0x08AC;
                    minimizedGraphic = 0x08BA;
                    iconStartGraphic = 0x08C0;

                    break;

                case SpellBookType.VystiaSorcerer:
                    maxSpellsCount = SpellsVystiaElemental.MaxSpellCount;
                    bookGraphic = 0x08AC;
                    minimizedGraphic = 0x08BA;
                    iconStartGraphic = 0x08C0;

                    break;

                case SpellBookType.VystiaWarlock:
                    maxSpellsCount = SpellsVystiaDark.MaxSpellCount;
                    bookGraphic = 0x08AC;
                    minimizedGraphic = 0x08BA;
                    iconStartGraphic = 0x08C0;

                    break;

                case SpellBookType.VystiaOracle:
                    maxSpellsCount = SpellsVystiaDivination.MaxSpellCount;
                    bookGraphic = 0x08AC;
                    minimizedGraphic = 0x08BA;
                    iconStartGraphic = 0x08C0;

                    break;

                case SpellBookType.VystiaNecromancer:
                    maxSpellsCount = SpellsVystiaNecromancy.MaxSpellCount;
                    bookGraphic = 0x08AC;
                    minimizedGraphic = 0x08BA;
                    iconStartGraphic = 0x08C0;

                    break;

                case SpellBookType.VystiaSummoner:
                    maxSpellsCount = SpellsVystiaSummoning.MaxSpellCount;
                    bookGraphic = 0x08AC;
                    minimizedGraphic = 0x08BA;
                    iconStartGraphic = 0x08C0;

                    break;

                case SpellBookType.VystiaShaman:
                    maxSpellsCount = SpellsVystiaShamanic.MaxSpellCount;
                    bookGraphic = 0x08AC;
                    minimizedGraphic = 0x08BA;
                    iconStartGraphic = 0x08C0;

                    break;

                case SpellBookType.VystiaBard:
                    maxSpellsCount = SpellsVystiaBardic.MaxSpellCount;
                    bookGraphic = 0x08AC;
                    minimizedGraphic = 0x08BA;
                    iconStartGraphic = 0x08C0;

                    break;

                case SpellBookType.VystiaSongweaving:
                    maxSpellsCount = SpellsVystiaSongweaving.MaxSpellCount;
                    bookGraphic = 0x08AC;
                    minimizedGraphic = 0x08BA;
                    iconStartGraphic = 0x08C0;

                    break;

                case SpellBookType.VystiaEnchanter:
                    maxSpellsCount = SpellsVystiaEnchanting.MaxSpellCount;
                    bookGraphic = 0x08AC;
                    minimizedGraphic = 0x08BA;
                    iconStartGraphic = 0x08C0;

                    break;

                case SpellBookType.VystiaIllusionist:
                    maxSpellsCount = SpellsVystiaIllusion.MaxSpellCount;
                    bookGraphic = 0x08AC;
                    minimizedGraphic = 0x08BA;
                    iconStartGraphic = 0x08C0;

                    break;
            }

            // Vystia spellbooks have 32 spells (4 per circle × 8 circles)
            // They need special handling to display like Magery (8 circles, 4 pages)
            if (IsVystiaSpellbookType(type))
            {
                spellsOnPage = 4; // 4 spells per circle
                dictionaryPagesCount = 8; // 8 circles total (shown 2 per page = 4 index pages)
            }
            else
            {
                spellsOnPage = Math.Min(maxSpellsCount >> 1, 8);
                dictionaryPagesCount = (int)Math.Ceiling(maxSpellsCount / 8.0f);

                if (dictionaryPagesCount % 2 != 0)
                {
                    dictionaryPagesCount++;
                }
            }
        }

        private static bool IsVystiaSpellbookType(SpellBookType type)
        {
            return type >= SpellBookType.VystiaIceMagic && type <= SpellBookType.VystiaIllusionist;
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

                case SpellBookType.VystiaIceMagic:
                case SpellBookType.VystiaDruid:
                case SpellBookType.VystiaWitch:
                case SpellBookType.VystiaSorcerer:
                case SpellBookType.VystiaWarlock:
                case SpellBookType.VystiaOracle:
                case SpellBookType.VystiaNecromancer:
                case SpellBookType.VystiaSummoner:
                case SpellBookType.VystiaShaman:
                case SpellBookType.VystiaBard:
                case SpellBookType.VystiaSongweaving:
                case SpellBookType.VystiaEnchanter:
                case SpellBookType.VystiaIllusionist:
                    offset = 0; // No cliloc tooltips for Vystia spells yet

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

                case SpellBookType.VystiaIceMagic:
                    def = SpellsVystiaIceMagic.GetSpell(offset + 1);
                    name = def.Name;
                    abbreviature = def.PowerWords;
                    reagents = def.CreateReagentListString("\n");

                    break;

                case SpellBookType.VystiaDruid:
                    def = SpellsVystiaNature.GetSpell(offset + 1);
                    name = def.Name;
                    abbreviature = def.PowerWords;
                    reagents = def.CreateReagentListString("\n");

                    break;

                case SpellBookType.VystiaWitch:
                    def = SpellsVystiaHex.GetSpell(offset + 1);
                    name = def.Name;
                    abbreviature = def.PowerWords;
                    reagents = def.CreateReagentListString("\n");

                    break;

                case SpellBookType.VystiaSorcerer:
                    def = SpellsVystiaElemental.GetSpell(offset + 1);
                    name = def.Name;
                    abbreviature = def.PowerWords;
                    reagents = def.CreateReagentListString("\n");

                    break;

                case SpellBookType.VystiaWarlock:
                    def = SpellsVystiaDark.GetSpell(offset + 1);
                    name = def.Name;
                    abbreviature = def.PowerWords;
                    reagents = def.CreateReagentListString("\n");

                    break;

                case SpellBookType.VystiaOracle:
                    def = SpellsVystiaDivination.GetSpell(offset + 1);
                    name = def.Name;
                    abbreviature = def.PowerWords;
                    reagents = def.CreateReagentListString("\n");

                    break;

                case SpellBookType.VystiaNecromancer:
                    def = SpellsVystiaNecromancy.GetSpell(offset + 1);
                    name = def.Name;
                    abbreviature = def.PowerWords;
                    reagents = def.CreateReagentListString("\n");

                    break;

                case SpellBookType.VystiaSummoner:
                    def = SpellsVystiaSummoning.GetSpell(offset + 1);
                    name = def.Name;
                    abbreviature = def.PowerWords;
                    reagents = def.CreateReagentListString("\n");

                    break;

                case SpellBookType.VystiaShaman:
                    def = SpellsVystiaShamanic.GetSpell(offset + 1);
                    name = def.Name;
                    abbreviature = def.PowerWords;
                    reagents = def.CreateReagentListString("\n");

                    break;

                case SpellBookType.VystiaBard:
                    def = SpellsVystiaBardic.GetSpell(offset + 1);
                    name = def.Name;
                    abbreviature = def.PowerWords;
                    reagents = def.CreateReagentListString("\n");

                    break;

                case SpellBookType.VystiaSongweaving:
                    def = SpellsVystiaSongweaving.GetSpell(offset + 1);
                    name = def.Name;
                    abbreviature = def.PowerWords;
                    reagents = def.CreateReagentListString("\n");

                    break;

                case SpellBookType.VystiaEnchanter:
                    def = SpellsVystiaEnchanting.GetSpell(offset + 1);
                    name = def.Name;
                    abbreviature = def.PowerWords;
                    reagents = def.CreateReagentListString("\n");

                    break;

                case SpellBookType.VystiaIllusionist:
                    def = SpellsVystiaIllusion.GetSpell(offset + 1);
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

        private static int GetSpellIdOffset(SpellBookType type)
        {
            switch (type)
            {
                case SpellBookType.Magery:
                    return 0; // Spells 1-64
                case SpellBookType.Necromancy:
                    return 100; // Spells 101-117
                case SpellBookType.Chivalry:
                    return 200; // Spells 201-210
                case SpellBookType.Bushido:
                    return 400; // Spells 401-406
                case SpellBookType.Ninjitsu:
                    return 500; // Spells 501-508
                case SpellBookType.Spellweaving:
                    return 600; // Spells 601-616
                case SpellBookType.Mysticism:
                    return 677; // Spells 678-693
                case SpellBookType.Mastery:
                    return 700; // Spells 701-743
                case SpellBookType.VystiaIceMagic:
                    return 1000; // Spells 1000-1031
                case SpellBookType.VystiaDruid:
                    return 1032; // Spells 1032-1063
                case SpellBookType.VystiaWitch:
                    return 1064; // Spells 1064-1095
                case SpellBookType.VystiaSorcerer:
                    return 1096; // Spells 1096-1127
                case SpellBookType.VystiaWarlock:
                    return 1128; // Spells 1128-1159
                case SpellBookType.VystiaOracle:
                    return 1160; // Spells 1160-1191
                case SpellBookType.VystiaNecromancer:
                    return 1192; // Spells 1192-1223
                case SpellBookType.VystiaSummoner:
                    return 1224; // Spells 1224-1255
                case SpellBookType.VystiaShaman:
                    return 1256; // Spells 1256-1287
                case SpellBookType.VystiaBard:
                    return 1288; // Spells 1288-1319
                case SpellBookType.VystiaSongweaving:
                    return 1384; // Spells 1384-1415
                case SpellBookType.VystiaEnchanter:
                    return 1320; // Spells 1320-1351
                case SpellBookType.VystiaIllusionist:
                    return 1352; // Spells 1352-1383
                default:
                    return 0;
            }
        }

        private void AssignGraphic(Item item)
        {
            switch (item.Graphic)
            {
                default:
                case 0x0EFA:
                    // Check hue to distinguish Vystia spellbooks from Magery
                    if (item.Hue == 0x7D6) // Forest Green
                        _spellBookType = SpellBookType.VystiaDruid;
                    else if (item.Hue == 0x54E) // Fiery Orange
                        _spellBookType = SpellBookType.VystiaSorcerer;
                    else if (item.Hue == 0x482) // Crystal Blue
                        _spellBookType = SpellBookType.VystiaOracle;
                    else if (item.Hue == 0x555) // Deep Blue
                        _spellBookType = SpellBookType.VystiaSummoner;
                    else if (item.Hue == 0x501) // Storm Blue
                        _spellBookType = SpellBookType.VystiaShaman;
                    else if (item.Hue == 0x8A5) // Golden
                        _spellBookType = SpellBookType.VystiaSongweaving;
                    else if (item.Hue == 0x8FD) // Arcane Purple
                        _spellBookType = SpellBookType.VystiaEnchanter;
                    else if (item.Hue == 0x47E) // Silvery
                        _spellBookType = SpellBookType.VystiaIllusionist;
                    else
                        _spellBookType = SpellBookType.Magery;

                    break;

                case 0x2253:
                    // Check hue to distinguish Vystia Necromancer (0x455) from standard Necromancy
                    if (item.Hue == 0x455) // Void Black
                        _spellBookType = SpellBookType.VystiaNecromancer;
                    else
                        _spellBookType = SpellBookType.Necromancy;

                    break;

                case 0x2252:
                    // Check hue to distinguish Vystia Ice Magic (0x481) from Chivalry
                    if (item.Hue == 0x481)
                        _spellBookType = SpellBookType.VystiaIceMagic;
                    else
                        _spellBookType = SpellBookType.Chivalry;

                    break;

                case 0xFF0:
                    // Check hue to distinguish Vystia Witch (0x81D) and Warlock (0x455) from other books
                    if (item.Hue == 0x81D) // Murky Green/Purple
                        _spellBookType = SpellBookType.VystiaWitch;
                    else if (item.Hue == 0x455) // Void Black
                        _spellBookType = SpellBookType.VystiaWarlock;
                    else
                        _spellBookType = SpellBookType.Magery; // Default fallback

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

        /// <summary>
        /// Checks if the spellbook type is a Vystia magic school
        /// </summary>
        private bool IsVystiaSpellbook()
        {
            return _spellBookType >= SpellBookType.VystiaIceMagic && _spellBookType <= SpellBookType.VystiaIllusionist;
        }

        /// <summary>
        /// Gets the circle names for Vystia spellbooks (all use same circle names)
        /// </summary>
        private string[] GetVystiaCircleNames()
        {
            // All Vystia spellbooks have 8 circles with 4 spells each
            return SpellsMagery.CircleNames; // Reuse same circle names
        }

        private class HueGumpPic : GumpPic
        {
            private readonly SpellbookGump _gump;
            private readonly MacroManager _mm;
            private readonly ushort _spellID;
            private readonly string _spellName;

            /// <summary>
            /// ShowEdit button when user pressing ctrl + alt
            /// </summary>
            private bool ShowEdit =>
                Keyboard.Ctrl && Keyboard.Alt && ProfileManager.CurrentProfile.FastSpellsAssign;

            public HueGumpPic(
                SpellbookGump gump,
                int x,
                int y,
                ushort graphic,
                ushort hue,
                ushort spellID,
                string spellName
            ) : base(x, y, graphic, hue)
            {
                _gump = gump;
                _spellID = spellID;
                _spellName = spellName;

                _mm = gump.World.Macros;
            }

            public override void Update()
            {
                base.Update();

                if (_gump.World.ActiveSpellIcons.IsActive(_spellID))
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

                    ref readonly var gumpInfo = ref Client.Game.UO.Gumps.GetGump(0x09CF);

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
                    GameActions.OpenMacroGump(_gump.World, _spellName);
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
