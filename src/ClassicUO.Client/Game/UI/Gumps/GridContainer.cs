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

using System;
using System.Linq;
using ClassicUO.Configuration;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using ClassicUO.Resources;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace ClassicUO.Game.UI.Gumps
{
    internal class GridContainer : Gump
    {

        private static int _lastX = 100;
        private static int _lastY = 100;
        private readonly AlphaBlendControl _background;
        private readonly NiceButton _buttonPrev, _buttonNext;
        private readonly Item _container;
        private const int X_SPACING = 1;
        private const int Y_SPACING = 1;
        private const int GRID_ITEM_SIZE = 50;
        private const int DEFAULT_WIDTH = 3 + 14 + (GRID_ITEM_SIZE + X_SPACING) * 4;
        private const int DEFAULT_HEIGHT = 3 + (GRID_ITEM_SIZE + Y_SPACING) * 4;
        private int _currentPage = 1;
        private readonly Label _currentPageLabel;
        private readonly Label _containerNameLabel;
        private int _pagesCount;
        private readonly ScrollArea _scrollArea;

        public GridContainer(uint local) : base(local, 0)
        {
            _container = World.Items.Get(local);

            if (_container == null)
            {
                Dispose();

                return;
            }

            X = _lastX;
            Y = _lastY;

            CanMove = true;
            AcceptMouseInput = true;
            WantUpdateSize = true;
            CanCloseWithRightClick = true;
            _background = new AlphaBlendControl();
            _background.Width = DEFAULT_WIDTH;
            _background.Height = DEFAULT_HEIGHT;
            Add(_background);

            Width = _background.Width;
            Height = _background.Height;

            _buttonPrev = new NiceButton
            (
                Width - 80,
                Height - 20,
                40,
                20,
                ButtonAction.Activate,
                ResGumps.Prev
            )
            { ButtonParameter = 0, IsSelectable = false };

            _buttonNext = new NiceButton
            (
                Width - 40,
                Height - 20,
                40,
                20,
                ButtonAction.Activate,
                ResGumps.Next
            )
            { ButtonParameter = 1, IsSelectable = false };

            _buttonNext.IsVisible = _buttonPrev.IsVisible = false;


            Add(_buttonPrev);
            Add(_buttonNext);

            Add
            (
                _currentPageLabel = new Label("1", true, 999)
                {
                    X = _background.Width / 2 - Width,
                    Y = Height - 20
                }
            );

            Add
            (
                _containerNameLabel = new Label(GetContainerName(), true, 0x0481)
                {
                    X = 0,
                    Y = 0
                }
            );


            _scrollArea = new ScrollArea(0, _containerNameLabel.Height + 1, _background.Width, _background.Height - (_containerNameLabel.Height + 1), true);
            _scrollArea.AcceptMouseInput = true;
            _scrollArea.CanMove = true;
            Add(_scrollArea);

            _scrollArea.MouseUp += (sender, e) =>
            {
                if (e.Button == MouseButtonType.Left)
                {
                    Console.WriteLine("Mouse up");
                    if (Client.Game.GameCursor.ItemHold.Enabled)
                    {
                        Console.WriteLine("Drag end");
                        GameActions.DropItem(Client.Game.GameCursor.ItemHold.Serial, 0xFFFF, 0xFFFF, 0, _container);
                    }
                }
            };
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
                GameActions.Print(ResGumps.TargetContainerToGrabItemsInto);
                TargetManager.SetTargeting(CursorTarget.SetGrabBag, 0, TargetType.Neutral);
            }
            else
            {
                base.OnButtonClick(buttonID);
            }
        }


        protected override void UpdateContents()
        {
            int x = X_SPACING;
            int y = Y_SPACING + _containerNameLabel.Height;

            foreach (Control child in _scrollArea.Children) {
                if (child is GridLootItem)
                    child.Dispose();
            }

            //foreach (GridLootItem gridLootItem in Children.OfType<GridLootItem>())
            //{
            //    gridLootItem.Dispose();
            //}

            int count = 0;
            _pagesCount = 1;

            int line = 1;

            for (LinkedObject i = _container.Items; i != null; i = i.Next)
            {
                Item it = (Item)i;

                GridLootItem gridItem = new GridLootItem(it, GRID_ITEM_SIZE, _container);

                if (x + GRID_ITEM_SIZE >= _scrollArea.Width - 14 - X_SPACING) //14 is the scroll bar width
                {
                    x = X_SPACING;
                    ++line;

                    y += gridItem.Height + Y_SPACING;

                    //if (y >= _background.Height - _currentPageLabel.Height)
                    //{
                    //    _pagesCount++;
                    //    y = Y_SPACING;
                    //}
                }

                gridItem.X = x + X_SPACING;
                gridItem.Y = y;
                _scrollArea.Add(gridItem);//, _pagesCount);

                x += gridItem.Width + X_SPACING;
                ++count;
            }

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

        }

        public override void Dispose()
        {
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

            batcher.DrawRectangle
            (
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
            if (_container == null || _container.IsDestroyed || _container.OnGround && _container.Distance > 3)
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
            _currentPageLabel.X = Width / 2 - 5;
            _currentPageLabel.Y = Height - 20;

            _containerNameLabel.Text = GetContainerName();

            WantUpdateSize = true;

            if (_container != null && !_container.IsDestroyed && UIManager.MouseOverControl != null && (UIManager.MouseOverControl == this || UIManager.MouseOverControl.RootParent == this))
            {
                SelectedObject.Object = _container;
            }
        }

        private string GetContainerName()
        {
            return _container.Name?.Length > 0 ? _container.Name : "a container";
        }

        private class GridLootItem : Control
        {
            private readonly HitBox _hit;

            public GridLootItem(uint serial, int size, Item container)
            {
                Point startDrag = new Point(0, 0);
                bool potentialDrag = false;


                LocalSerial = serial;

                Item item = World.Items.Get(serial);

                if (item == null)
                {
                    Dispose();

                    return;
                }

                CanMove = false;

                AlphaBlendControl background = new AlphaBlendControl();
                background.Width = size;
                background.Height = size;
                Add(background);

                Label _count = new Label((item.ItemData.IsStackable ? item.Amount : 1).ToString(), true, 0x0481, align: TEXT_ALIGN_TYPE.TS_LEFT, maxwidth: size);
                _count.X = 0;
                _count.Y = size - _count.Height;

                Add(_count);

                _hit = new HitBox(0, 0, size, size, null, 0.25f);
                Add(_hit);

                if (World.ClientFeatures.TooltipsEnabled)
                {
                    _hit.SetTooltip(item);
                }


                _hit.MouseDown += (sender, e) =>
                {
                    if (e.Button == MouseButtonType.Left)
                    {
                        startDrag = e.Location;
                        potentialDrag = true;
                    }
                };

                _hit.MouseExit += (sender, e) =>
                {
                    if (potentialDrag && startDrag != e.Location)
                    {
                        GameActions.PickUp(item, startDrag.X, startDrag.Y);
                        potentialDrag = false;
                    }
                };

                _hit.MouseUp += (sender, e) =>
                {
                    if (e.Button == MouseButtonType.Left)
                    {
                        if (Client.Game.GameCursor.ItemHold.Enabled)
                        {
                            if (item.ItemData.IsContainer)
                                GameActions.DropItem(Client.Game.GameCursor.ItemHold.Serial, 0xFFFF, 0xFFFF, 0, item);
                            else if (item.ItemData.IsStackable && item.Graphic == Client.Game.GameCursor.ItemHold.Graphic)
                                GameActions.DropItem(Client.Game.GameCursor.ItemHold.Serial, item.X, item.Y, 0, item);
                            else
                                GameActions.DropItem(Client.Game.GameCursor.ItemHold.Serial, item.X, item.Y, 0, container);
                        }
                        else
                            GameActions.SingleClick(item);
                        potentialDrag = false;
                    }
                };

                _hit.MouseDoubleClick += (sender, e) =>
                {
                    GameActions.DoubleClick(item);
                };

                Width = background.Width;
                Height = background.Height;

                WantUpdateSize = false;
            }

            public override bool Draw(UltimaBatcher2D batcher, int x, int y)
            {
                base.Draw(batcher, x, y);

                Item item = World.Items.Get(LocalSerial);

                Vector3 hueVector;

                if (item != null)
                {
                    var texture = ArtLoader.Instance.GetStaticTexture(item.DisplayedGraphic, out var bounds);
                    var rect = ArtLoader.Instance.GetRealArtBounds(item.DisplayedGraphic);

                    hueVector = ShaderHueTranslator.GetHueVector(item.Hue, item.ItemData.IsPartialHue, 1f);

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

                    batcher.Draw
                    (
                        texture,
                        new Rectangle
                        (
                            x + point.X,
                            y + point.Y + _hit.Y,
                            originalSize.X,
                            originalSize.Y
                        ),
                        new Rectangle
                        (
                            bounds.X + rect.X,
                            bounds.Y + rect.Y,
                            rect.Width,
                            rect.Height
                        ),
                        hueVector
                    );
                }

                hueVector = ShaderHueTranslator.GetHueVector(0);

                batcher.DrawRectangle
                (
                    SolidColorTextureCache.GetTexture(Color.Gray),
                    x,
                    y,
                    Width,
                    Height,
                    hueVector
                );

                if (_hit.MouseIsOver)
                {
                    hueVector.Z = 0.7f;

                    batcher.Draw
                    (
                        SolidColorTextureCache.GetTexture(Color.Yellow),
                        new Rectangle
                        (
                            x + 1,
                            y,
                            Width - 1,
                            Height
                        ),
                        hueVector
                    );

                    hueVector.Z = 1;
                }

                return true;
            }
        }
    }
}