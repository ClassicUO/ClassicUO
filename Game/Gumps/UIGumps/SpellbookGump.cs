using System;
using System.Collections.Generic;
using System.Text;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Gumps.Controls;
using ClassicUO.Input;
using ClassicUO.IO;

namespace ClassicUO.Game.Gumps.UIGumps
{
    class SpellbookGump : Gump
    {

        private SpellBookType _spellBookType;

        private GumpPic _pageCornerLeft, _pageCornerRight;

        private int _maxPage;

        private Label[] _indexes;
        private readonly List<KeyValuePair<int, int>> _spellList = new List<KeyValuePair<int, int>>();

        private readonly Item _spellBook;

        public SpellbookGump(Item item) : base(item.Serial, 0)
        {
            _spellBook = item;
            _spellBook.SetCallbacks(OnEntityUpdate, OnEntityDispose);

            CanMove = true;
            AcceptMouseInput = false;

            switch (item.Graphic)
            {
                default:
                case 0x0EFA:
                    _spellBookType = SpellBookType.Magery;
                    CreateMagery();
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
            }
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

            AddChildren(_pageCornerRight = new GumpPic(321,8, 0x08BC, 0));
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
                    X = 106 + (i % 2) * 163,
                    Y = 10,
                }, 1 + i / 2);
            }

            for (int i = 0; i < 8; i++)
            {
                AddChildren(new Label(SpellsMagery.CircleNames[i], false, 0x0288, font: 6)
                {
                    X = 62 + (i % 2) * 161,
                    Y = 30,
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
                        X = (i % 2 ) == 0 ? 64 : 225,
                        Y = 52 + 15 * j,
                        AcceptMouseInput = true,
                        LocalSerial = (uint)(index + 1)
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
                                    SetActivePage((int)_indexes[spellIndex - 1].Tag);
                                };

                                _indexes[spellIndex - 1].MouseDoubleClick += (sender, e) =>
                                {
                                    GumpControl control = (GumpControl)sender;
                                    if (FileManager.ClientVersion < ClientVersions.CV_308Z)
                                        GameActions.CastSpellFromBook((int)control.LocalSerial.Value, _spellBook.Serial);
                                    else
                                        GameActions.CastSpell((int)control.LocalSerial.Value);
                                };

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
                SetActivePage((int)ctrl.LocalSerial.Value / 2 + 1);
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
                SetActivePage(ctrl.LocalSerial == 0 ?  1 : _maxPage);
            }
        }

        private void CreateNecromancy()
        {

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
            AddChildren(new Label(SpellsMagery.CircleNames[circle], false, 0x0288, font: 6) { X = isright ? 64 + 162 : 85, Y = 10 } , page);

            AddChildren(new GumpPic(isright ? 225 : 62, 40, (Graphic)(spell.GumpIconID - 0x1298), 0), page);

            AddChildren(new Label(spell.Name, false, 0x0288, 80, 6) { X = isright ? 275 : 112, Y = 34}, page);


            AddChildren(new GumpPicTiled(isright ?  225 : 62, 88, 120, 4, 0x0835), page);
            AddChildren(new Label("Reagents:", false, 0x0288, font: 6){ X = isright ? 225 : 62, Y = 92 }, page);
            AddChildren(new Label(spell.CreateReagentListString(",\n"), false, 0x0288, font: 9) { X = isright ? 225 : 62, Y = 114 }, page);

        }

        private void OnEntityUpdate(Entity entity)
        {
            switch (entity.Graphic)
            {
                default:
                case 0x0EFA:
                    _spellBookType = SpellBookType.Magery;
                    CreateMagery();
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
            }
        }

        private void OnEntityDispose(Entity entity)
        {
            Dispose();
        }
    }
}
