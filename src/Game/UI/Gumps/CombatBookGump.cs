using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ClassicUO.Game.Data;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.IO;

namespace ClassicUO.Game.UI.Gumps
{
    class CombatBookGump : Gump
    {
        private int _abilityCount = Constants.MAX_ABILITIES_COUNT;
        private int _dictionaryPagesCount = 3;
        private GumpPic _pageCornerLeft, _pageCornerRight, _primAbility, _secAbility;

        public CombatBookGump(int x, int y) : base(0 ,0)
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
            AddChildren(new GumpPic(0, 0, 0x2B02, 0));


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
                    AddChildren(text, page);

                    for (int i = 0; i < spellsOnPage; i++)
                    {
                        if (offs >= _abilityCount)
                            break;

                        text = new HoveredLabel(AbilityData.Abilities[offs].Name, false, 0x0288, 0x33, font: 9)
                        {
                            X = dataX, Y = 42 + y, AcceptMouseInput = true,
                        };

                        AddChildren(text, page);

                        y += 15;
                        offs++;
                    }

                    if (spellsOnPage == 4)
                    {
                        _primAbility = new GumpPic(215, 105, (ushort) (0x5200 + ( (byte)World.Player.PrimaryAbility & 0x7F ) - 1), 0);
                        text = new Label("Primary Ability Icon", false, 0x0288, 80, 6){ X=  265, Y = 105};
                        AddChildren(_primAbility);
                        AddChildren(text, page);

                        _primAbility.DragBegin += (sender, e) =>
                        {
                            AbilityDefinition def = AbilityData.Abilities[((byte)World.Player.PrimaryAbility & 0x7F) - 1];

                            UseAbilityButtonGump gump = new UseAbilityButtonGump(def, true)
                            {
                                X = Mouse.Position.X - 22, Y = Mouse.Position.Y - 22
                            };
                            Engine.UI.Add(gump);
                            Engine.UI.AttemptDragControl(gump, Mouse.Position, true);
                        };

                        _secAbility = new GumpPic(215, 150, (ushort)(0x5200 + ((byte)World.Player.SecondaryAbility & 0x7F) - 1), 0);
                        text = new Label("Secondary Ability Icon", false, 0x0288, 80, 6) { X = 265, Y = 150 };
                        AddChildren(_secAbility);
                        AddChildren(text, page);

                        _secAbility.DragBegin += (sender, e) =>
                        {
                            AbilityDefinition def = AbilityData.Abilities[((byte)World.Player.SecondaryAbility & 0x7F) - 1];

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
                int index = ((byte)  (i == 0 ? World.Player.PrimaryAbility : World.Player.SecondaryAbility) & 0x7F) - 1;

                AbilityDefinition def = AbilityData.Abilities[index];

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
        }
    }
}
