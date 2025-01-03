// SPDX-License-Identifier: BSD-2-Clause

using System.Linq;
using ClassicUO.Configuration;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Assets;
using ClassicUO.Renderer;
using ClassicUO.Resources;
using Microsoft.Xna.Framework;
using ClassicUO.Game.Scenes;

namespace ClassicUO.Game.UI.Gumps
{
    internal class GridLootGump : Gump
    {
        private const int MAX_WIDTH = 300;
        private const int MAX_HEIGHT = 420;

        private static int _lastX = ProfileManager.CurrentProfile.GridLootType == 2 ? 200 : 100;
        private static int _lastY = 100;
        private readonly AlphaBlendControl _background;
        private readonly NiceButton _buttonPrev,
            _buttonNext,
            _setlootbag;
        private readonly Item _corpse;

        private int _currentPage = 1;
        private readonly Label _currentPageLabel;
        private readonly Label _corpseNameLabel;
        private readonly bool _hideIfEmpty;
        private int _pagesCount;

        public GridLootGump(World world, uint local) : base(world, local, 0)
        {
            _corpse = World.Items.Get(local);

            if (_corpse == null)
            {
                Dispose();

                return;
            }

            if (World.Player.ManualOpenedCorpses.Contains(LocalSerial))
            {
                World.Player.ManualOpenedCorpses.Remove(LocalSerial);
            }
            else if (
                World.Player.AutoOpenedCorpses.Contains(LocalSerial)
                && ProfileManager.CurrentProfile != null
                && ProfileManager.CurrentProfile.SkipEmptyCorpse
            )
            {
                IsVisible = false;
                _hideIfEmpty = true;
            }

            X = _lastX;
            Y = _lastY;

            CanMove = true;
            AcceptMouseInput = true;
            WantUpdateSize = true;
            CanCloseWithRightClick = true;
            _background = new AlphaBlendControl();
            //_background.Width = MAX_WIDTH;
            //_background.Height = MAX_HEIGHT;
            Add(_background);

            Width = _background.Width;
            Height = _background.Height;

            _setlootbag = new NiceButton(
                3,
                Height - 23,
                100,
                20,
                ButtonAction.Activate,
                ResGumps.SetLootBag
            )
            {
                ButtonParameter = 2,
                IsSelectable = false
            };

            Add(_setlootbag);

            _buttonPrev = new NiceButton(
                Width - 80,
                Height - 20,
                40,
                20,
                ButtonAction.Activate,
                ResGumps.Prev
            )
            {
                ButtonParameter = 0,
                IsSelectable = false
            };

            _buttonNext = new NiceButton(
                Width - 40,
                Height - 20,
                40,
                20,
                ButtonAction.Activate,
                ResGumps.Next
            )
            {
                ButtonParameter = 1,
                IsSelectable = false
            };

            _buttonNext.IsVisible = _buttonPrev.IsVisible = false;

            Add(_buttonPrev);
            Add(_buttonNext);

            Add(
                _currentPageLabel = new Label("1", true, 999, align: TEXT_ALIGN_TYPE.TS_CENTER)
                {
                    X = Width / 2 - 5,
                    Y = Height - 20
                }
            );

            Add(
                _corpseNameLabel = new Label(
                    GetCorpseName(),
                    true,
                    0x0481,
                    align: TEXT_ALIGN_TYPE.TS_CENTER,
                    maxwidth: 300
                )
                {
                    Width = 300,
                    X = 0,
                    Y = 0
                }
            );
        }

        public override void OnButtonClick(int buttonID)
        {
            if (buttonID == 0)
            {
                _currentPage--;

                if (_currentPage <= 1)
                {
                    _currentPage = 1;
                    _buttonPrev.IsVisible = false;
                }

                _buttonNext.IsVisible = true;
                ChangePage(_currentPage);

                _currentPageLabel.Text = ActivePage.ToString();
                _currentPageLabel.X = Width / 2 - _currentPageLabel.Width / 2;
            }
            else if (buttonID == 1)
            {
                _currentPage++;

                if (_currentPage >= _pagesCount)
                {
                    _currentPage = _pagesCount;
                    _buttonNext.IsVisible = false;
                }

                _buttonPrev.IsVisible = true;

                ChangePage(_currentPage);

                _currentPageLabel.Text = ActivePage.ToString();
                _currentPageLabel.X = Width / 2 - _currentPageLabel.Width / 2;
            }
            else if (buttonID == 2)
            {
                GameActions.Print(World, ResGumps.TargetContainerToGrabItemsInto);
                World.TargetManager.SetTargeting(CursorTarget.SetGrabBag, 0, TargetType.Neutral);
            }
            else
            {
                base.OnButtonClick(buttonID);
            }
        }

        protected override void UpdateContents()
        {
            const int GRID_ITEM_SIZE = 50;

            int x = 20;
            int y = 20;

            foreach (GridLootItem gridLootItem in Children.OfType<GridLootItem>())
            {
                gridLootItem.Dispose();
            }

            int count = 0;
            _pagesCount = 1;

            _background.Width = x;
            _background.Height = y;

            int line = 1;
            int row = 0;

            for (int displayGroup = 0; displayGroup < 2; displayGroup++)
            {
                for (LinkedObject i = _corpse.Items; i != null; i = i.Next)
                {
                    Item it = (Item)i;

                    if (!ItemBelongsToGroup(it, displayGroup) || !it.IsLootable)
                    {
                        continue;
                    }

                    GridLootItem gridItem = new GridLootItem(this, it, GRID_ITEM_SIZE);

                    if (x >= MAX_WIDTH - 20)
                    {
                        x = 20;
                        ++line;

                        y += gridItem.Height + 20;

                        if (y >= MAX_HEIGHT - 60)
                        {
                            _pagesCount++;
                            y = 20;
                            //line = 1;
                        }
                    }

                    gridItem.X = x;
                    gridItem.Y = y + 20;
                    Add(gridItem, _pagesCount);

                    x += gridItem.Width + 20;
                    ++row;
                    ++count;
                }
            }

            _background.Width = (GRID_ITEM_SIZE + 20) * row + 20;
            _background.Height = 20 + 40 + (GRID_ITEM_SIZE + 20) * line + 40;

            if (_background.Height >= MAX_HEIGHT - 40)
            {
                _background.Height = MAX_HEIGHT;
            }

            _background.Width = MAX_WIDTH;

            if (ActivePage <= 1)
            {
                ActivePage = 1;
                _buttonNext.IsVisible = _pagesCount > 1;
                _buttonPrev.IsVisible = false;
            }
            else if (ActivePage >= _pagesCount)
            {
                ActivePage = _pagesCount;
                _buttonNext.IsVisible = false;
                _buttonPrev.IsVisible = _pagesCount > 1;
            }
            else if (ActivePage > 1 && ActivePage < _pagesCount)
            {
                _buttonNext.IsVisible = true;
                _buttonPrev.IsVisible = true;
            }

            if (count == 0)
            {
                GameActions.Print(World, ResGumps.CorpseIsEmpty);
                Dispose();
            }
            else if (_hideIfEmpty && !IsVisible)
            {
                IsVisible = true;
            }
        }

        private bool ItemBelongsToGroup(Item it, int group)
        {
            // Note: items must be assigned to groups in a mutually-exclusive manner, so that each item occurs only once in the grid
            if (it.ItemData.IsStackable)
                return group > 0;
            else
                return group == 0;
        }

        public override void Dispose()
        {
            if (_corpse != null)
            {
                if (_corpse == SelectedObject.CorpseObject)
                {
                    SelectedObject.CorpseObject = null;
                }
            }

            _lastX = X;
            _lastY = Y;

            base.Dispose();
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (!IsVisible || IsDisposed)
            {
                return false;
            }

            base.Draw(batcher, x, y);

            Vector3 hueVector = ShaderHueTranslator.GetHueVector(0);

            batcher.DrawRectangle(
                SolidColorTextureCache.GetTexture(Color.Gray),
                x,
                y,
                Width,
                Height,
                hueVector
            );

            return true;
        }

        public override void Update()
        {
            if (_corpse == null || _corpse.IsDestroyed || _corpse.OnGround && _corpse.Distance > 3)
            {
                Dispose();

                return;
            }

            base.Update();

            if (IsDisposed)
            {
                return;
            }

            if (_background.Width < 100)
            {
                _background.Width = 100;
            }

            if (_background.Height < 120)
            {
                _background.Height = 120;
            }

            Width = _background.Width;
            Height = _background.Height;

            _buttonPrev.X = Width - 80;
            _buttonPrev.Y = Height - 23;
            _buttonNext.X = Width - 40;
            _buttonNext.Y = Height - 20;
            _setlootbag.X = 3;
            _setlootbag.Y = Height - 23;
            _currentPageLabel.X = Width / 2 - 5;
            _currentPageLabel.Y = Height - 20;

            _corpseNameLabel.Text = GetCorpseName();

            WantUpdateSize = true;

            if (
                _corpse != null
                && !_corpse.IsDestroyed
                && UIManager.MouseOverControl != null
                && (
                    UIManager.MouseOverControl == this
                    || UIManager.MouseOverControl.RootParent == this
                )
            )
            {
                SelectedObject.Object = _corpse;
                SelectedObject.CorpseObject = _corpse;
            }
        }

        protected override void OnMouseExit(int x, int y)
        {
            if (_corpse != null && !_corpse.IsDestroyed)
            {
                SelectedObject.CorpseObject = null;
            }
        }

        private string GetCorpseName()
        {
            return _corpse.Name?.Length > 0 ? _corpse.Name : "a corpse";
        }

        private class GridLootItem : Control
        {
            private readonly GridLootGump _gump;
            private readonly HitBox _hit;

            public GridLootItem(GridLootGump gump, uint serial, int size)
            {
                _gump = gump;
                LocalSerial = serial;

                Item item = _gump.World.Items.Get(serial);

                if (item == null)
                {
                    Dispose();

                    return;
                }

                CanMove = false;

                HSliderBar amount = new HSliderBar(
                    0,
                    0,
                    size,
                    1,
                    // OSI has an odd behaviour. It uses the Amount field to store unknown data for non stackable items.
                    item.ItemData.IsStackable
                        ? item.Amount
                        : 1,
                    item.ItemData.IsStackable ? item.Amount : 1,
                    HSliderBarStyle.MetalWidgetRecessedBar,
                    true,
                    color: 0xFFFF,
                    drawUp: true
                );

                Add(amount);

                amount.IsVisible = amount.IsEnabled = amount.MaxValue > 1;

                AlphaBlendControl background = new AlphaBlendControl();
                background.Y = 15;
                background.Width = size;
                background.Height = size;
                Add(background);

                _hit = new HitBox(0, 15, size, size, null, 0f);
                Add(_hit);

                if (_gump.World.ClientFeatures.TooltipsEnabled)
                {
                    _hit.SetTooltip(item);
                }

                _hit.MouseUp += (sender, e) =>
                {
                    if (e.Button == MouseButtonType.Left)
                    {
                        GameActions.GrabItem(_gump.World, item, (ushort)amount.Value);
                    }
                };

                Width = background.Width;
                Height = background.Height + 15;

                WantUpdateSize = false;
            }

            public override bool Draw(UltimaBatcher2D batcher, int x, int y)
            {
                base.Draw(batcher, x, y);

                Item item = _gump.World.Items.Get(LocalSerial);

                Vector3 hueVector;

                if (item != null)
                {
                    ref readonly var artInfo = ref Client.Game.UO.Arts.GetArt(item.DisplayedGraphic);

                    var rect = Client.Game.UO.Arts.GetRealArtBounds(item.DisplayedGraphic);

                    hueVector = ShaderHueTranslator.GetHueVector(
                        item.Hue,
                        item.ItemData.IsPartialHue,
                        1f
                    );

                    Point originalSize = new Point(_hit.Width, _hit.Height);
                    Point point = new Point();

                    if (rect.Width < _hit.Width)
                    {
                        originalSize.X = rect.Width;
                        point.X = (_hit.Width >> 1) - (originalSize.X >> 1);
                    }

                    if (rect.Height < _hit.Height)
                    {
                        originalSize.Y = rect.Height;
                        point.Y = (_hit.Height >> 1) - (originalSize.Y >> 1);
                    }

                    batcher.Draw(
                        artInfo.Texture,
                        new Rectangle(
                            x + point.X,
                            y + point.Y + _hit.Y,
                            originalSize.X,
                            originalSize.Y
                        ),
                        new Rectangle(
                            artInfo.UV.X + rect.X,
                            artInfo.UV.Y + rect.Y,
                            rect.Width,
                            rect.Height
                        ),
                        hueVector
                    );
                }

                hueVector = ShaderHueTranslator.GetHueVector(0);

                batcher.DrawRectangle(
                    SolidColorTextureCache.GetTexture(Color.Gray),
                    x,
                    y + 15,
                    Width,
                    Height - 15,
                    hueVector
                );

                if (_hit.MouseIsOver)
                {
                    hueVector.Z = 0.7f;

                    batcher.Draw(
                        SolidColorTextureCache.GetTexture(Color.Yellow),
                        new Rectangle(x + 1, y + 15, Width - 1, Height - 15),
                        hueVector
                    );

                    hueVector.Z = 1;
                }

                return true;
            }
        }
    }
}
